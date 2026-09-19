using System;
using System.Collections.Generic;
using Items;
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
        [SerializeField] private LevelGenerator _levelGenerator;
        [SerializeField] private LayoutsLibrary _library;
        [SerializeField] private TierTable _tierTable;
        [SerializeField] private PropSet _propSet;
        [SerializeField] private bool _mirrorX;
        [SerializeField] private bool _mirrorZ;
        [SerializeField] private Color _boundsColor = Color.gray;
        [Space]
        [SerializeField] private LayoutSet _customLayout;

        private readonly List<float> _zoneRadii = new List<float>(16);

        public LevelGenerator LevelGenerator => _levelGenerator;
        public LayoutsLibrary Library => _library;
        public PropSet PropSet => _propSet;
        public bool MirrorX => _mirrorX;
        public bool MirrorZ => _mirrorZ;
        public LayoutSet CustomLayout => _customLayout;

#if UNITY_EDITOR
        private static GUIStyle s_zoneLabelStyle;

        private static GUIStyle GetZoneLabelStyle()
        {
            if (s_zoneLabelStyle == null)
            {
                s_zoneLabelStyle = new GUIStyle();
                s_zoneLabelStyle.normal.textColor = Color.yellow;
            }

            return s_zoneLabelStyle;
        }

        private void OnDrawGizmos()
        {
            if (_levelGenerator == null || _tierTable == null || _library == null)
            {
                return;
            }

            DrawMapBounds();
            DrawOrigin();

            if (_customLayout != null)
            {
                DrawLayoutZones(_customLayout, 0);
                return;
            }

            for (int layoutIndex = 0; layoutIndex < _library.Layouts.Count; layoutIndex++)
            {
                LayoutSet layout = _library.Layouts[layoutIndex];

                if (layout != null)
                {
                    DrawLayoutZones(layout, layoutIndex);
                }
            }
        }

        private float MaxTierScale(SpawnZone zone)
        {
            float maxScale = 1f;

            for (int i = 0; i < _tierTable.Entries.Count; i++)
            {
                TierEntry entry = _tierTable.Entries[i];

                if (entry.Tier >= zone.MinTier && entry.Tier <= zone.MaxTier)
                {
                    maxScale = Mathf.Max(maxScale, entry.Scale);
                }
            }

            return maxScale;
        }

        private void DrawLayoutZones(LayoutSet layout, int layoutIndex)
        {
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

                float maxScale = MaxTierScale(zone);
                _zoneRadii.Clear();

                for (int propIndex = 0; propIndex < _propSet.Props.Count; propIndex++)
                {
                    if (_propSet.Props[propIndex] != null)
                    {
                        _zoneRadii.Add(ItemSize.GetRadiusXZ(_propSet.Props[propIndex]) * maxScale);
                    }
                }

                if (_zoneRadii.Count == 0)
                {
                    _zoneRadii.Add(maxScale);
                }

                float spacing = ZoneLayoutPlanner.ResolveSpacing(zone, layout, _zoneRadii);
                ZoneLayoutPlanner planner = new ZoneLayoutPlanner(new System.Random(layoutIndex * 7919 + i * 17 + 3));
                planner.Collect(zone, center, spacing, layout);

                if (zone.Shape == SpawnShape.Grid)
                {
                    Vector2 halfExtents = GetPositionHalfExtents(planner.Positions);
                    DrawRectOutline(center, halfExtents.x, halfExtents.y, tierColor);
                }
                else if (zone.Shape == SpawnShape.CircleGrid)
                {
                    float outerRadius = GetMaxRadialDistance(planner.Positions, center);
                    DrawZoneOutline(center, outerRadius, tierColor);
                }
                else
                {
                    DrawZoneOutline(center, zone.Radius, tierColor);
                }

                DrawZoneDots(planner.Positions, tierColor, spacing);
                DrawZoneLabel(center, zone, layoutIndex, i, planner.Positions.Count);
            }
        }

        private void DrawMapBounds()
        {
            Bounds floorBounds = _levelGenerator.FloorBounds;

            if (floorBounds.size.x == 0f || floorBounds.size.z == 0f)
            {
                return;
            }

            Vector3 cornerA = new Vector3(floorBounds.min.x, floorBounds.min.y, floorBounds.min.z);
            Vector3 cornerB = new Vector3(floorBounds.max.x, floorBounds.min.y, floorBounds.min.z);
            Vector3 cornerC = new Vector3(floorBounds.max.x, floorBounds.min.y, floorBounds.max.z);
            Vector3 cornerD = new Vector3(floorBounds.min.x, floorBounds.min.y, floorBounds.max.z);

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

        private static Vector2 GetPositionHalfExtents(IReadOnlyList<Vector3> positions)
        {
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minZ = float.MaxValue;
            float maxZ = float.MinValue;

            for (int i = 0; i < positions.Count; i++)
            {
                minX = Mathf.Min(minX, positions[i].x);
                maxX = Mathf.Max(maxX, positions[i].x);
                minZ = Mathf.Min(minZ, positions[i].z);
                maxZ = Mathf.Max(maxZ, positions[i].z);
            }

            return new Vector2((maxX - minX) * 0.5f + 0.3f, (maxZ - minZ) * 0.5f + 0.3f);
        }

        private static float GetMaxRadialDistance(IReadOnlyList<Vector3> positions, Vector2 center)
        {
            float maxDistance = 0f;

            for (int i = 0; i < positions.Count; i++)
            {
                float deltaX = positions[i].x - center.x;
                float deltaZ = positions[i].z - center.y;
                maxDistance = Mathf.Max(maxDistance, Mathf.Sqrt(deltaX * deltaX + deltaZ * deltaZ));
            }

            return maxDistance;
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
                float offsetX = center.x + Mathf.Cos(angle) * radius;
                float offsetZ = center.y + Mathf.Sin(angle) * radius;

                Vector3 nextPoint = transform.TransformPoint(new Vector3(offsetX, 0f, offsetZ));
                Gizmos.DrawLine(previousPoint, nextPoint);
                previousPoint = nextPoint;
            }
        }

        private void DrawZoneDots(IReadOnlyList<Vector3> positions, Color tierColor, float spacing)
        {
            float dotRadius = Mathf.Clamp(spacing * 0.2f, 0.1f, 0.5f);

            Gizmos.color = tierColor;

            for (int i = 0; i < positions.Count; i++)
            {
                Gizmos.DrawSphere(transform.TransformPoint(positions[i]), dotRadius);
            }
        }

        private void DrawZoneLabel(Vector2 center, SpawnZone zone, int layoutIndex, int zoneIndex, int positionsCount)
        {
            string tierRange = $"{zone.MinTier}-{zone.MaxTier}";
            string labelText = $"L{layoutIndex} / Zone {zoneIndex}: {zone.Shape} x{positionsCount} {tierRange}";

            Vector3 labelPosition = transform.TransformPoint(new Vector3(center.x, 0f, center.y)) + Vector3.up;

            Handles.Label(labelPosition, labelText, GetZoneLabelStyle());
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
