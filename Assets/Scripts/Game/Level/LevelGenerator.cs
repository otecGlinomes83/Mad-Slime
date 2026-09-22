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

        private readonly Dictionary<Item, ItemDefinition> _assignedVariants = new Dictionary<Item, ItemDefinition>();
        private readonly Dictionary<ItemDefinition, int> _spawnedCounts = new Dictionary<ItemDefinition, int>();
        private readonly List<Item> _zonePool = new List<Item>(16);
        private readonly List<float> _zoneRadii = new List<float>(16);
        private readonly ZoneLayoutPlanner _layoutPlanner = new ZoneLayoutPlanner(new System.Random());

        private LevelConfigResolver _configResolver;
        private PlayerProgress _progress;
        private LevelProgress _levelProgress;
        private ItemPool _itemPool;
        private QuotaGenerator _quotaGenerator;
        private TierTable _tierTable;
        private LayoutsLibrary _layoutsLibrary;
        private Movement.Mover _mover;
        private Collectables.Collector _collector;
        private Bounds _floorBounds;

        public Bounds FloorBounds
        {
            get
            {
                if (_floorRenderer == null)
                {
                    return default;
                }

                return _floorRenderer.bounds;
            }
        }

        [Inject]
        public void Construct(LevelConfigResolver configResolver, PlayerProgress progress, LevelProgress levelProgress,
            ItemPool itemPool, QuotaGenerator quotaGenerator, TierTable tierTable, LayoutsLibrary layoutsLibrary,
            Movement.Mover mover, Collectables.Collector collector)
        {
            _configResolver = configResolver;
            _progress = progress;
            _levelProgress = levelProgress;
            _itemPool = itemPool;
            _quotaGenerator = quotaGenerator;
            _tierTable = tierTable;
            _layoutsLibrary = layoutsLibrary;
            _mover = mover;
            _collector = collector;
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

            if (_floorRenderer == null)
            {
                throw new InvalidOperationException(
                    $"{name}: FloorRenderer is not assigned. Drag a MeshRenderer into the _floorRenderer field.");
            }

            if (_mover == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Mover was not injected. Check that GameLifetimeScope registers Mover and LevelGenerator.");
            }

            if (_collector == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Collector was not injected. Check that GameLifetimeScope registers Collector and LevelGenerator.");
            }

            Generate();
        }

        private void OnEnable()
        {
            _collector.ItemCollected += OnItemCollected;
        }

        private void OnDisable()
        {
            _collector.ItemCollected -= OnItemCollected;
        }

        private void OnItemCollected(Items.Item item)
        {
            _itemPool.Release(item);
        }

        private void Generate()
        {
            LevelConfig config = _configResolver.GetConfigFor(_progress.CurrentLevel);
            LayoutSet layout = PickLayout();

            _floorBounds = _floorRenderer.bounds;
            _mover.SetBounds(_floorBounds);

            ApplyTheme(config);
            _spawnedCounts.Clear();
            AssignTiers(config);
            SpawnItems(config, layout);
            Physics.SyncTransforms();

            List<QuotaEntry> quota = _quotaGenerator.Generate(_spawnedCounts, config);
            _levelProgress.Reset(quota);

            Debug.Log(
                $"{name}: level {_progress.CurrentLevel} from '{config.name}', layout '{layout.name}': spawned {GetTotalSpawnedCount()} items, quota types {quota.Count}.");
        }

        private LayoutSet PickLayout()
        {
            if (_layoutsLibrary == null)
            {
                throw new InvalidOperationException(
                    $"{name}: LayoutsLibrary was not injected. Assign it in the ProjectScope.");
            }

            if (_layoutsLibrary.Layouts.Count == 0)
            {
                throw new InvalidOperationException(
                    $"{name}: LayoutsLibrary '{_layoutsLibrary.name}' is empty. Add at least one LayoutSet.");
            }

            List<LayoutSet> playable = new List<LayoutSet>(_layoutsLibrary.Layouts.Count);

            foreach (LayoutSet layout in _layoutsLibrary.Layouts)
            {
                if (layout == null)
                {
                    Debug.LogWarning(
                        $"{name}: LayoutsLibrary '{_layoutsLibrary.name}' has an empty slot, skipped.");
                    continue;
                }

                if (layout.Zones.Count == 0)
                {
                    Debug.LogWarning(
                        $"{name}: LayoutSet '{layout.name}' has no zones, skipped. Add zones to it or remove it from the library.");
                    continue;
                }

                playable.Add(layout);
            }

            if (playable.Count == 0)
            {
                throw new InvalidOperationException(
                    $"{name}: LayoutsLibrary '{_layoutsLibrary.name}' has no playable layouts — all are empty or zone-less. " +
                    "Fill at least one LayoutSet with zones.");
            }

            return playable[Random.Range(0, playable.Count)];
        }

        private void AssignTiers(LevelConfig config)
        {
            _assignedVariants.Clear();

            if (config.PropSet == null)
            {
                throw new InvalidOperationException(
                    $"{name}: LevelConfig '{config.name}' has no PropSet assigned. Drag a PropSet asset into the _propSet field.");
            }

            IReadOnlyList<PropVariant> variants = config.PropSet.Variants;

            if (variants.Count == 0)
            {
                throw new InvalidOperationException(
                    $"{name}: PropSet '{config.PropSet.name}' has no baked variants. Run Mad Slime → Prop Factory.");
            }

            Dictionary<Item, List<ItemDefinition>> variantsByPrefab = new Dictionary<Item, List<ItemDefinition>>();

            foreach (PropVariant variant in variants)
            {
                if (variant == null || variant.Prefab == null || variant.Definition == null)
                {
                    throw new InvalidOperationException(
                        $"{name}: PropSet '{config.PropSet.name}' contains an empty variant. Re-run Mad Slime → Prop Factory.");
                }

                if (InTierRange(variant.Definition.Tier, config) == false)
                {
                    continue;
                }

                if (variantsByPrefab.TryGetValue(variant.Prefab, out List<ItemDefinition> list) == false)
                {
                    list = new List<ItemDefinition>();
                    variantsByPrefab.Add(variant.Prefab, list);
                }

                list.Add(variant.Definition);
            }

            if (variantsByPrefab.Count == 0)
            {
                throw new InvalidOperationException(
                    $"{name}: PropSet '{config.PropSet.name}' has no variants within tier range {config.MinTier}-{config.MaxTier}. Re-run Prop Factory.");
            }

            List<Item> prefabs = new List<Item>(variantsByPrefab.Keys);
            Shuffle(prefabs);

            List<ItemTier> tiers = TiersInRange(config);
            int guaranteedCount = Mathf.Min(tiers.Count, prefabs.Count);

            for (int i = 0; i < prefabs.Count; i++)
            {
                Item prefab = prefabs[i];
                List<ItemDefinition> options = variantsByPrefab[prefab];
                ItemDefinition chosen;

                if (i < guaranteedCount)
                {
                    chosen = FindByTier(options, tiers[i]);
                }
                else
                {
                    chosen = options[Random.Range(0, options.Count)];
                }

                _assignedVariants[prefab] = chosen;
            }
        }

        private void SpawnItems(LevelConfig config, LayoutSet layout)
        {
            bool mirrorX = layout.AllowMirroring == true && Random.value > 0.5f;
            bool mirrorZ = layout.AllowMirroring == true && Random.value > 0.5f;

            IReadOnlyList<SpawnZone> zones = layout.Zones;

            for (int i = 0; i < zones.Count; i++)
            {
                SpawnZone zone = zones[i];

                FillZonePool(zone);

                if (_zonePool.Count == 0)
                {
                    Debug.LogWarning(
                        $"{name}: zone {i} ({zone.Shape}) skipped: no props assigned to tiers {zone.MinTier}-{zone.MaxTier} on this run.");
                    continue;
                }

                float spacing = ZoneLayoutPlanner.ResolveSpacing(zone, layout, _zoneRadii);

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
                    Item prefab = _zonePool[Random.Range(0, _zonePool.Count)];
                    ItemDefinition definition = _assignedVariants[prefab];
                    float scale = _tierTable.Get(definition.Tier).Scale;

                    Item item = _itemPool.Get(prefab);
                    item.SetDefinition(definition);
                    item.Initialize(ClampToMap(_layoutPlanner.Positions[j], spacing * 0.5f), scale);
                    item.transform.SetParent(_itemsRoot, true);

                    CountSpawned(definition);
                }
            }
        }

        private void FillZonePool(SpawnZone zone)
        {
            _zonePool.Clear();
            _zoneRadii.Clear();

            foreach (KeyValuePair<Item, ItemDefinition> pair in _assignedVariants)
            {
                if (pair.Value.Tier < zone.MinTier || pair.Value.Tier > zone.MaxTier)
                {
                    continue;
                }

                _zonePool.Add(pair.Key);
                _zoneRadii.Add(ItemSize.GetRadiusXZ(pair.Key) * _tierTable.Get(pair.Value.Tier).Scale);
            }
        }

        private void Shuffle(List<Item> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int swapIndex = Random.Range(0, i + 1);
                (list[i], list[swapIndex]) = (list[swapIndex], list[i]);
            }
        }

        private static List<ItemTier> TiersInRange(LevelConfig config)
        {
            List<ItemTier> tiers = new List<ItemTier>();

            for (int value = (int)config.MinTier; value <= (int)config.MaxTier; value++)
            {
                tiers.Add((ItemTier)value);
            }

            return tiers;
        }

        private static bool InTierRange(ItemTier tier, LevelConfig config)
        {
            return tier >= config.MinTier && tier <= config.MaxTier;
        }

        private static ItemDefinition FindByTier(List<ItemDefinition> options, ItemTier tier)
        {
            for (int i = 0; i < options.Count; i++)
            {
                if (options[i].Tier == tier)
                {
                    return options[i];
                }
            }

            return options[0];
        }

        private void ApplyTheme(LevelConfig config)
        {
            if (_floorRenderer == null || config.Theme == null || config.Theme.FloorMaterial == null)
            {
                return;
            }

            _floorRenderer.sharedMaterial = config.Theme.FloorMaterial;
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

        private int GetTotalSpawnedCount()
        {
            int total = 0;

            foreach (KeyValuePair<ItemDefinition, int> pair in _spawnedCounts)
            {
                total += pair.Value;
            }

            return total;
        }

        private Vector3 ClampToMap(Vector3 localPosition, float margin)
        {
            Vector3 worldPosition = transform.TransformPoint(localPosition);
            worldPosition.x = Mathf.Clamp(worldPosition.x, _floorBounds.min.x + margin, _floorBounds.max.x - margin);
            worldPosition.z = Mathf.Clamp(worldPosition.z, _floorBounds.min.z + margin, _floorBounds.max.z - margin);

            return worldPosition;
        }
    }
}
