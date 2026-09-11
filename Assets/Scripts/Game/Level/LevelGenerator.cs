using System;
using System.Collections.Generic;
using Items;
using Quota;
using Scriptables;
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
        private readonly Dictionary<ItemDefinition, int> _spawnedCounts = new Dictionary<ItemDefinition, int>();
        private readonly ZoneLayoutPlanner _layoutPlanner = new ZoneLayoutPlanner(new System.Random());

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
            LayoutSet layout = PickLayout(config);
            bool mirrorX = layout.AllowMirroring == true && Random.value > 0.5f;
            bool mirrorZ = layout.AllowMirroring == true && Random.value > 0.5f;

            IReadOnlyList<SpawnZone> zones = layout.Zones;

            for (int i = 0; i < zones.Count; i++)
            {
                SpawnZone zone = zones[i];

                ZoneLayoutPlanner.FilterPool(config.Theme.ItemPool, zone, _zonePool);

                if (_zonePool.Count == 0)
                {
                    Debug.LogWarning(
                        $"{name}: zone {i} ({zone.Shape}) skipped: theme '{config.Theme.name}' has no Item prefab with assigned Definition for tiers {zone.MinTier}-{zone.MaxTier}.");
                    continue;
                }

                float spacing = ZoneLayoutPlanner.ResolveSpacing(zone, layout, _zonePool);

                Vector2 center = zone.Center;

                if (mirrorX == true)
                {
                    center.x = -center.x;
                }

                if (mirrorZ == true)
                {
                    center.y = -center.y;
                }

                _layoutPlanner.Collect(zone, center, spacing, layout);

                for (int j = 0; j < _layoutPlanner.Positions.Count; j++)
                {
                    Item itemPrefab = _zonePool[Random.Range(0, _zonePool.Count)];

                    Item item = _itemPool.Get(itemPrefab);
                    item.Initialize(ClampToMap(_layoutPlanner.Positions[j], spacing * 0.5f));
                    item.transform.SetParent(_itemsRoot, true);

                    CountSpawned(itemPrefab.Definition);
                }
            }
        }

        private LayoutSet PickLayout(LevelConfig config)
        {
            if (config.Layouts.Count == 0)
            {
                throw new InvalidOperationException(
                    $"{name}: LevelConfig '{config.name}' has no LayoutSets assigned. Add at least one LayoutSet to the _layouts list.");
            }

            LayoutSet layout = config.Layouts[Random.Range(0, config.Layouts.Count)];

            if (layout == null)
            {
                throw new InvalidOperationException(
                    $"{name}: LevelConfig '{config.name}' contains an empty LayoutSet slot. Remove it or assign a LayoutSet asset.");
            }

            if (layout.Zones.Count == 0)
            {
                throw new InvalidOperationException(
                    $"{name}: LayoutSet '{layout.name}' has no zones. Add at least one SpawnZone.");
            }

            return layout;
        }

        private void CountSpawned(ItemDefinition definition)
        {
            if (_spawnedCounts.TryGetValue(definition, out int count) == true)
            {
                _spawnedCounts[definition] = count + 1;
            }
            else
            {
                _spawnedCounts[definition] = 1;
            }
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
