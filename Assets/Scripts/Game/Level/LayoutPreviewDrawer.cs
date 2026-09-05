using System;
using System.Collections.Generic;
using Scriptables;
using Skills;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game
{
    public sealed class LayoutPreviewDrawer : MonoBehaviour
    {
        [SerializeField] private LevelsCatalog _catalog;
        [SerializeField] private LevelGenerator _levelGenerator;
        [SerializeField] private int _previewLevel = 1;
        [SerializeField] private bool _mirrorX;
        [SerializeField] private bool _mirrorZ;
        [SerializeField] private Color _boundsColor = Color.gray;

        private readonly List<Vector3> _previewPositions = new List<Vector3>(64);

        private LevelConfigResolver _resolver;

        public LevelsCatalog Catalog => _catalog;
        public LevelGenerator LevelGenerator => _levelGenerator;
        public int PreviewLevel => _previewLevel;
        public bool MirrorX => _mirrorX;
        public bool MirrorZ => _mirrorZ;

        public LevelConfigResolver Resolver
        {
            get
            {
                if (_resolver == null && _catalog != null)
                {
                    _resolver = new LevelConfigResolver(_catalog);
                }

                return _resolver;
            }
        }

#if UNITY_EDITOR
        private static GUIStyle _zoneLabelStyle;
        private float _lastGridHalfX;
        private float _lastGridHalfZ;
        private float _lastOuterRadius;

        private static GUIStyle GetZoneLabelStyle()
        {
            if (_zoneLabelStyle == null)
            {
                _zoneLabelStyle = new GUIStyle();
                _zoneLabelStyle.normal.textColor = Color.yellow;
            }

            return _zoneLabelStyle;
        }

        private void OnDrawGizmos()
        {
            if (_catalog == null || _levelGenerator == null)
            {
                return;
            }

            if (_catalog.Ranges.Count == 0)
            {
                return;
            }

            LevelConfig config = Resolver.GetConfigFor(_previewLevel);

            DrawMapBounds();
            DrawOrigin();
            DrawZones(config);
        }

        private void DrawMapBounds()
        {
            Vector2 mapSize = _levelGenerator.MapSize;
            float halfX = mapSize.x * 0.5f;
            float halfZ = mapSize.y * 0.5f;

            Vector3 cornerA = transform.TransformPoint(new Vector3(-halfX, 0f, -halfZ));
            Vector3 cornerB = transform.TransformPoint(new Vector3(halfX, 0f, -halfZ));
            Vector3 cornerC = transform.TransformPoint(new Vector3(halfX, 0f, halfZ));
            Vector3 cornerD = transform.TransformPoint(new Vector3(-halfX, 0f, halfZ));

            Gizmos.color = _boundsColor;
            Gizmos.DrawLine(cornerA, cornerB);
            Gizmos.DrawLine(cornerB, cornerC);
            Gizmos.DrawLine(cornerC, cornerD);
            Gizmos.DrawLine(cornerD, cornerA);
        }

        private void DrawOrigin()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.4f);
        }

        private void DrawZones(LevelConfig config)
        {
            LayoutSet layout = config.Layout;
            IReadOnlyList<SpawnZone> zones = layout.Zones;

            for (int i = 0; i < zones.Count; i++)
            {
                SpawnZone zone = zones[i];
                Color tierColor = GetTierColor(zone.MinTier);

                Vector2 center = zone.Center;

                if (_mirrorX == true)
                {
                    center.x = -center.x;
                }

                if (_mirrorZ == true)
                {
                    center.y = -center.y;
                }

                float spacing = ResolveSpacing(zone, layout, config.Theme.ItemPool);
                CollectPositions(zone, center, spacing, i, layout);

                if (zone.Shape == SpawnShape.Grid)
                {
                    DrawRectOutline(center, _lastGridHalfX, _lastGridHalfZ, tierColor);
                }
                else if (zone.Shape == SpawnShape.CircleGrid)
                {
                    DrawZoneOutline(center, _lastOuterRadius, tierColor);
                }
                else
                {
                    DrawZoneOutline(center, zone.Radius, tierColor);
                }

                DrawZoneDots(tierColor, spacing);
                DrawZoneLabel(center, zone, i);
            }
        }

        private void DrawRectOutline(Vector2 center, float halfX, float halfZ, Color color)
        {
            Gizmos.color = color;

            Vector3 cornerA = transform.TransformPoint(new Vector3(center.x - halfX, 0f, center.y - halfZ));
            Vector3 cornerB = transform.TransformPoint(new Vector3(center.x + halfX, 0f, center.y - halfZ));
            Vector3 cornerC = transform.TransformPoint(new Vector3(center.x + halfX, 0f, center.y + halfZ));
            Vector3 cornerD = transform.TransformPoint(new Vector3(center.x - halfX, 0f, center.y + halfZ));

            Gizmos.DrawLine(cornerA, cornerB);
            Gizmos.DrawLine(cornerB, cornerC);
            Gizmos.DrawLine(cornerC, cornerD);
            Gizmos.DrawLine(cornerD, cornerA);
        }

        private void DrawZoneOutline(Vector2 center, float radius, Color color)
        {
            const int segments = 36;

            Gizmos.color = color;

            Vector3 previousPoint = transform.TransformPoint(new Vector3(center.x + radius, 0f, center.y));

            for (int i = 1; i <= segments; i++)
            {
                float angle = 2f * Mathf.PI * i / segments;
                float x = center.x + Mathf.Cos(angle) * radius;
                float z = center.y + Mathf.Sin(angle) * radius;

                Vector3 nextPoint = transform.TransformPoint(new Vector3(x, 0f, z));
                Gizmos.DrawLine(previousPoint, nextPoint);
                previousPoint = nextPoint;
            }
        }

        private void DrawZoneDots(Color tierColor, float spacing)
        {
            float dotRadius = Mathf.Clamp(spacing * 0.2f, 0.1f, 0.5f);

            Gizmos.color = tierColor;

            for (int i = 0; i < _previewPositions.Count; i++)
            {
                Gizmos.DrawSphere(transform.TransformPoint(_previewPositions[i]), dotRadius);
            }
        }

        private void DrawZoneLabel(Vector2 center, SpawnZone zone, int zoneIndex)
        {
            string tierRange = $"{zone.MinTier}-{zone.MaxTier}";
            string labelText = $"Zone {zoneIndex}: {zone.Shape} x{_previewPositions.Count} {tierRange}";

            Vector3 labelPosition = transform.TransformPoint(new Vector3(center.x, 0f, center.y)) + Vector3.up;

            Handles.Label(labelPosition, labelText, GetZoneLabelStyle());
        }

        private float ResolveSpacing(SpawnZone zone, LayoutSet layout, IReadOnlyList<Items.Item> pool)
        {
            if (zone.AutoSpacing == false && zone.Spacing > 0f)
            {
                return zone.Spacing;
            }

            float maxRadius = 0f;

            for (int i = 0; i < pool.Count; i++)
            {
                Items.Item prefab = pool[i];

                if (prefab == null || prefab.Definition == null)
                {
                    continue;
                }

                ItemTier prefabTier = prefab.Definition.Tier;

                if (prefabTier < zone.MinTier || prefabTier > zone.MaxTier)
                {
                    continue;
                }

                maxRadius = Mathf.Max(maxRadius, ItemSize.GetRadiusXZ(prefab));
            }

            return Mathf.Max(0.5f, maxRadius * layout.AutoSpacingFactor);
        }

        private void CollectPositions(SpawnZone zone, Vector2 center, float spacing, int zoneIndex,
            LayoutSet layout)
        {
            _previewPositions.Clear();

            if (zone.Shape == SpawnShape.Grid)
            {
                CollectGridPositions(center, zone.Count, spacing);
            }
            else if (zone.Shape == SpawnShape.CircleGrid)
            {
                CollectCircleGridPositions(center, zone.Count, spacing);
            }
            else if (zone.Shape == SpawnShape.Circle)
            {
                CollectCirclePositions(center, zone.Radius, zone.Count);
            }
            else
            {
                CollectScatterPositions(center, zone.Radius, zone.Count, spacing, zoneIndex, layout);
            }
        }

        private void CollectGridPositions(Vector2 center, int count, float spacing)
        {
            _lastGridHalfX = spacing * 0.5f;
            _lastGridHalfZ = spacing * 0.5f;

            if (count <= 0)
            {
                return;
            }

            int rows = Mathf.Max(1, Mathf.RoundToInt(Mathf.Sqrt(count)));
            int columns = Mathf.CeilToInt(count / (float)rows);
            float halfWidth = (columns - 1) * spacing * 0.5f;
            float halfDepth = (rows - 1) * spacing * 0.5f;

            _lastGridHalfX = halfWidth + spacing * 0.5f;
            _lastGridHalfZ = halfDepth + spacing * 0.5f;

            int placedCount = 0;

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    if (placedCount >= count)
                    {
                        break;
                    }

                    Vector3 offset = new Vector3(column * spacing - halfWidth, 0f, row * spacing - halfDepth);

                    _previewPositions.Add(new Vector3(center.x, 0f, center.y) + offset);
                    placedCount++;
                }
            }
        }

        private void CollectCircleGridPositions(Vector2 center, int count, float spacing)
        {
            _lastOuterRadius = spacing * 0.5f;

            if (count <= 0)
            {
                return;
            }

            _previewPositions.Add(new Vector3(center.x, 0f, center.y));

            int placedCount = 1;
            int ringIndex = 1;

            while (placedCount < count)
            {
                float ringRadius = ringIndex * spacing;
                _lastOuterRadius = ringRadius + spacing * 0.5f;

                int ringCapacity = Mathf.Max(1, Mathf.FloorToInt(2f * Mathf.PI * ringRadius / spacing));
                int pointsOnRing = Mathf.Min(ringCapacity, count - placedCount);
                float angleStep = 2f * Mathf.PI / pointsOnRing;

                for (int i = 0; i < pointsOnRing; i++)
                {
                    float angle = angleStep * i;
                    float x = center.x + Mathf.Cos(angle) * ringRadius;
                    float z = center.y + Mathf.Sin(angle) * ringRadius;

                    _previewPositions.Add(new Vector3(x, 0f, z));
                    placedCount++;
                }

                ringIndex++;
            }
        }

        private void CollectCirclePositions(Vector2 center, float radius, int count)
        {
            if (count <= 0)
            {
                return;
            }

            float angleStep = 2f * Mathf.PI / count;

            for (int i = 0; i < count; i++)
            {
                float angle = angleStep * i;
                float x = center.x + Mathf.Cos(angle) * radius;
                float z = center.y + Mathf.Sin(angle) * radius;

                _previewPositions.Add(new Vector3(x, 0f, z));
            }
        }

        private void CollectScatterPositions(Vector2 center, float radius, int count, float spacing, int zoneIndex,
            LayoutSet layout)
        {
            System.Random zoneRandom = new System.Random(zoneIndex * 7919 + 17);
            float minDistance = spacing * layout.ScatterDistanceFactor;
            float minDistanceSqr = minDistance * minDistance;
            int attemptsLimit = count * 10;
            int attempts = 0;

            while (_previewPositions.Count < count && attempts < attemptsLimit)
            {
                attempts++;

                double angle = zoneRandom.NextDouble() * 2.0 * Math.PI;
                double distance = radius * Math.Sqrt(zoneRandom.NextDouble());

                float x = center.x + Mathf.Cos((float)angle) * (float)distance;
                float z = center.y + Mathf.Sin((float)angle) * (float)distance;

                Vector3 candidate = new Vector3(x, 0f, z);

                if (IsFarEnough(candidate, minDistanceSqr) == true)
                {
                    _previewPositions.Add(candidate);
                }
            }
        }

        private bool IsFarEnough(Vector3 candidate, float minDistanceSqr)
        {
            for (int i = 0; i < _previewPositions.Count; i++)
            {
                Vector3 delta = candidate - _previewPositions[i];
                delta.y = 0f;

                if (delta.sqrMagnitude < minDistanceSqr)
                {
                    return false;
                }
            }

            return true;
        }

        private static Color GetTierColor(ItemTier tier)
        {
            if (tier == ItemTier.Small)
            {
                return Color.green;
            }

            if (tier == ItemTier.Medium)
            {
                return Color.yellow;
            }

            if (tier == ItemTier.Large)
            {
                return new Color(1f, 0.5f, 0f);
            }

            return Color.magenta;
        }
#endif
    }
}
