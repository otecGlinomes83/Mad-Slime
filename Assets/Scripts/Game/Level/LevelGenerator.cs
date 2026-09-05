using System;
using System.Collections.Generic;
using Items;
using Quota;
using Scriptables;
using Skills;
using UnityEngine;
using VContainer;
using Random = UnityEngine.Random;

namespace Game
{
    public sealed class LevelGenerator : MonoBehaviour
    {
        [SerializeField] private Transform _itemsRoot;
        [SerializeField] private MeshRenderer _floorRenderer;
        [SerializeField] private Vector2 _mapSize = new Vector2(30f, 30f);
        [SerializeField] private Collectables.Collector _collector;

        private readonly List<Item> _zonePool = new List<Item>(16);
        private readonly List<Vector3> _positions = new List<Vector3>(64);
        private readonly Dictionary<ItemDefinition, int> _spawnedCounts = new Dictionary<ItemDefinition, int>();

        private LevelConfigResolver _configResolver;
        private PlayerProgress _progress;
        private LevelProgress _levelProgress;
        private ItemPool _itemPool;
        private QuotaGenerator _quotaGenerator;

        public Vector2 MapSize => _mapSize;

        [Inject]
        public void Construct(LevelConfigResolver configResolver, PlayerProgress progress, LevelProgress levelProgress,
            ItemPool itemPool, QuotaGenerator quotaGenerator)
        {
            _configResolver = configResolver;
            _progress = progress;
            _levelProgress = levelProgress;
            _itemPool = itemPool;
            _quotaGenerator = quotaGenerator;
        }

        private void Awake()
        {
            if (_itemsRoot == null)
            {
                throw new InvalidOperationException(
                    $"{name}: ItemsRoot is not assigned. Drag a Transform into the _itemsRoot field.");
            }

            if (_configResolver == null)
            {
                throw new InvalidOperationException(
                    $"{name}: dependencies were not injected. GameLifetimeScope must be the first object in the scene hierarchy.");
            }

            Generate();
        }

        private void OnEnable()
        {
            if (_collector != null)
            {
                _collector.ItemCollected += OnItemCollected;
            }
        }

        private void OnDisable()
        {
            if (_collector != null)
            {
                _collector.ItemCollected -= OnItemCollected;
            }
        }

        private void OnItemCollected(Items.Item item)
        {
            _itemPool.Release(item);
        }

        private void Generate()
        {
            LevelConfig config = _configResolver.GetConfigFor(_progress.CurrentLevel);

            ApplyTheme(config);
            _spawnedCounts.Clear();
            SpawnItems(config);

            List<QuotaEntry> quota = _quotaGenerator.Generate(_spawnedCounts, config);
            _levelProgress.Reset(quota, config.DefaultCountDivisor);

            Debug.Log(
                $"{name}: level {_progress.CurrentLevel} from '{config.name}': spawned {GetTotalSpawnedCount()} items, quota types {quota.Count}.");
        }

        private int GetTotalSpawnedCount()
        {
            int total = 0;

            foreach (KeyValuePair<ItemDefinition, int> pair in _spawnedCounts)
            {
                total += pair.Value;
            }

            return total;
        }

        private void ApplyTheme(LevelConfig config)
        {
            if (_floorRenderer == null || config.Theme.FloorMaterial == null)
            {
                return;
            }

            _floorRenderer.sharedMaterial = config.Theme.FloorMaterial;
        }

        private void SpawnItems(LevelConfig config)
        {
            LayoutSet layout = config.Layout;
            bool mirrorX = layout.AllowMirroring == true && Random.value > 0.5f;
            bool mirrorZ = layout.AllowMirroring == true && Random.value > 0.5f;

            IReadOnlyList<SpawnZone> zones = layout.Zones;

            for (int i = 0; i < zones.Count; i++)
            {
                SpawnZone zone = zones[i];

                FillZonePool(config.Theme.ItemPool, zone);

                if (_zonePool.Count == 0)
                {
                    Debug.LogWarning(
                        $"{name}: zone {i} ({zone.Shape}) skipped: theme '{config.Theme.name}' has no Item prefab with assigned Definition for tiers {zone.MinTier}-{zone.MaxTier}.");
                    continue;
                }

                float spacing = ResolveSpacing(zone, layout);
                CollectPositions(zone, spacing, mirrorX, mirrorZ, layout);

                for (int j = 0; j < _positions.Count; j++)
                {
                    Item itemPrefab = _zonePool[Random.Range(0, _zonePool.Count)];

                    Item item = _itemPool.Get(itemPrefab);
                    item.Initialize(ClampToMap(_positions[j], spacing * 0.5f));
                    item.transform.SetParent(_itemsRoot, true);

                    ItemDefinition definition = itemPrefab.Definition;

                    if (_spawnedCounts.TryGetValue(definition, out int count) == true)
                    {
                        _spawnedCounts[definition] = count + 1;
                    }
                    else
                    {
                        _spawnedCounts[definition] = 1;
                    }
                }
            }
        }

        private void FillZonePool(IReadOnlyList<Item> themePool, SpawnZone zone)
        {
            _zonePool.Clear();

            for (int i = 0; i < themePool.Count; i++)
            {
                Item prefab = themePool[i];

                if (prefab == null || prefab.Definition == null)
                {
                    continue;
                }

                ItemTier prefabTier = prefab.Definition.Tier;

                if (prefabTier < zone.MinTier || prefabTier > zone.MaxTier)
                {
                    continue;
                }

                _zonePool.Add(prefab);
            }
        }

        private float ResolveSpacing(SpawnZone zone, LayoutSet layout)
        {
            if (zone.AutoSpacing == false && zone.Spacing > 0f)
            {
                return zone.Spacing;
            }

            float maxRadius = 0f;

            for (int i = 0; i < _zonePool.Count; i++)
            {
                float radius = ItemSize.GetRadiusXZ(_zonePool[i]);
                maxRadius = Mathf.Max(maxRadius, radius);
            }

            return Mathf.Max(0.5f, maxRadius * layout.AutoSpacingFactor);
        }

        private void CollectPositions(SpawnZone zone, float spacing, bool mirrorX, bool mirrorZ, LayoutSet layout)
        {
            _positions.Clear();

            Vector2 center = zone.Center;

            if (mirrorX == true)
            {
                center.x = -center.x;
            }

            if (mirrorZ == true)
            {
                center.y = -center.y;
            }

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
                CollectScatterPositions(center, zone.Radius, zone.Count, spacing, layout);
            }
        }

        private void CollectGridPositions(Vector2 center, int count, float spacing)
        {
            if (count <= 0)
            {
                return;
            }

            int rows = Mathf.Max(1, Mathf.RoundToInt(Mathf.Sqrt(count)));
            int columns = Mathf.CeilToInt(count / (float)rows);
            float halfWidth = (columns - 1) * spacing * 0.5f;
            float halfDepth = (rows - 1) * spacing * 0.5f;

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

                    _positions.Add(new Vector3(center.x, 0f, center.y) + offset);
                    placedCount++;
                }
            }
        }

        private void CollectCircleGridPositions(Vector2 center, int count, float spacing)
        {
            if (count <= 0)
            {
                return;
            }

            _positions.Add(new Vector3(center.x, 0f, center.y));

            int placedCount = 1;
            int ringIndex = 1;

            while (placedCount < count)
            {
                float ringRadius = ringIndex * spacing;
                int ringCapacity = Mathf.Max(1, Mathf.FloorToInt(2f * Mathf.PI * ringRadius / spacing));
                int pointsOnRing = Mathf.Min(ringCapacity, count - placedCount);
                float angleStep = 2f * Mathf.PI / pointsOnRing;

                for (int i = 0; i < pointsOnRing; i++)
                {
                    float angle = angleStep * i;
                    float x = center.x + Mathf.Cos(angle) * ringRadius;
                    float z = center.y + Mathf.Sin(angle) * ringRadius;

                    _positions.Add(new Vector3(x, 0f, z));
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

                _positions.Add(new Vector3(x, 0f, z));
            }
        }

        private void CollectScatterPositions(Vector2 center, float radius, int count, float spacing,
            LayoutSet layout)
        {
            float minDistance = spacing * layout.ScatterDistanceFactor;
            float minDistanceSqr = minDistance * minDistance;
            int attemptsLimit = count * 10;
            int attempts = 0;

            while (_positions.Count < count && attempts < attemptsLimit)
            {
                attempts++;

                Vector2 randomPoint = Random.insideUnitCircle * radius;
                Vector3 candidate = new Vector3(center.x + randomPoint.x, 0f, center.y + randomPoint.y);

                if (IsFarEnough(candidate, minDistanceSqr) == true)
                {
                    _positions.Add(candidate);
                }
            }
        }

        private bool IsFarEnough(Vector3 candidate, float minDistanceSqr)
        {
            for (int i = 0; i < _positions.Count; i++)
            {
                Vector3 delta = candidate - _positions[i];
                delta.y = 0f;

                if (delta.sqrMagnitude < minDistanceSqr)
                {
                    return false;
                }
            }

            return true;
        }

        private Vector3 ClampToMap(Vector3 localPosition, float margin)
        {
            float halfX = _mapSize.x * 0.5f - margin;
            float halfZ = _mapSize.y * 0.5f - margin;

            Vector3 clamped = localPosition;
            clamped.x = Mathf.Clamp(clamped.x, -halfX, halfX);
            clamped.z = Mathf.Clamp(clamped.z, -halfZ, halfZ);

            return transform.TransformPoint(clamped);
        }
    }
}
