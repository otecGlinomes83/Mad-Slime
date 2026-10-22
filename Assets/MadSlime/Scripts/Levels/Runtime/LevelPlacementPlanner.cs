using System;
using System.Collections.Generic;
using Items;
using Scriptables;
using Skills;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game
{
    public class LevelPlacementPlanner
    {
        private const SizeTier LargeItemGapMinimumTier = SizeTier.Large;
        private const int MaximumBossCount = 10;

        private PlacementOccupancy _occupancy = new PlacementOccupancy();
        private TierLookup _tierLookup;
        private ZoneLayoutPlanner _layoutPlanner = new ZoneLayoutPlanner(new System.Random());
        private List<Item> _zonePool = new List<Item>(16);
        private List<float> _zoneRadii = new List<float>(16);
        private List<LevelPlacement> _placements = new List<LevelPlacement>(256);
        private int _bossCount;

        public LayoutSet PickLayout(LayoutsLibrary library)
        {
            if (library == null || library.Layouts.Count == 0)
            {
                throw new InvalidOperationException("LevelPlacementPlanner: layouts library is required and cannot be empty.");
            }

            for (int i = 0; i < library.Layouts.Count; i++)
            {
                LayoutSet layout = library.Layouts[i];

                if (layout == null || layout.Zones.Count == 0)
                {
                    throw new InvalidOperationException($"{library.name}: layout {i} is missing or has no zones.");
                }
            }

            return library.Layouts[Random.Range(0, library.Layouts.Count)];
        }

        public IReadOnlyList<LevelPlacement> Plan(LevelConfig config, LayoutSet layout,
            Dictionary<Item, ItemDefinition> assignedVariants, TierTable tierTable,
            Transform origin, Bounds floorBounds)
        {
            _tierLookup = new TierLookup(tierTable);
            _placements.Clear();
            _occupancy.Clear();
            _bossCount = 0;
            bool mirrorX = layout.AllowMirroring == true && Random.value > 0.5f;
            bool mirrorZ = layout.AllowMirroring == true && Random.value > 0.5f;

            for (int tierIndex = (int)config.MaxTier; tierIndex >= (int)config.MinTier; tierIndex--)
            {
                SizeTier tier = (SizeTier)tierIndex;

                for (int i = 0; i < layout.Zones.Count; i++)
                {
                    SpawnZone zone = layout.Zones[i];

                    if (tier < zone.MinTier || tier > zone.MaxTier)
                    {
                        continue;
                    }

                    BuildZonePool(zone, assignedVariants, tier);

                    if (_zonePool.Count == 0)
                    {
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

                    _layoutPlanner.CollectPlacements(zone, center, spacing, layout, _zoneRadii, zone.SingleType);
                    AppendPlacements(assignedVariants, origin, floorBounds, layout);
                }
            }

            if (_placements.Count == 0)
            {
                throw new InvalidOperationException($"{layout.name}: no zones match the item tiers of level config '{config.name}'.");
            }

            return _placements;
        }

        private void BuildZonePool(SpawnZone zone, Dictionary<Item, ItemDefinition> assignedVariants, SizeTier tier)
        {
            _zonePool.Clear();
            _zoneRadii.Clear();

            foreach (KeyValuePair<Item, ItemDefinition> pair in assignedVariants)
            {
                if (pair.Value.Tier != tier)
                {
                    continue;
                }

                float radius = LevelItemsSpawner.GetRadiusXZ(pair.Key) * _tierLookup.Get(tier).Scale;

                if (zone.Shape == SpawnShape.Scatter && radius > zone.Radius)
                {
                    continue;
                }

                _zonePool.Add(pair.Key);
                _zoneRadii.Add(radius);
            }
        }

        private void AppendPlacements(Dictionary<Item, ItemDefinition> assignedVariants,
            Transform origin, Bounds floorBounds, LayoutSet layout)
        {
            IReadOnlyList<ZonePlacement> placements = _layoutPlanner.Placements;

            for (int i = 0; i < placements.Count; i++)
            {
                ZonePlacement placement = placements[i];
                Item prefab = _zonePool[placement.PoolIndex];
                ItemDefinition definition = assignedVariants[prefab];

                if (definition.Tier == SizeTier.Boss && _bossCount >= MaximumBossCount)
                {
                    continue;
                }

                float scale = _tierLookup.Get(definition.Tier).Scale;
                float radius = _zoneRadii[placement.PoolIndex];

                if (radius > floorBounds.extents.x || radius > floorBounds.extents.z)
                {
                    continue;
                }

                Vector3 position = ClampToMap(origin.TransformPoint(placement.Position), floorBounds, radius);
                float gap = 0f;

                if (definition.Tier >= LargeItemGapMinimumTier)
                {
                    gap = layout.LargeItemGap;
                }

                if (IsStartAreaFree(position, radius, definition.Tier, layout) == false
                    || _occupancy.IsSpaceFree(position, radius, gap) == false)
                {
                    continue;
                }

                _placements.Add(new LevelPlacement(prefab, definition, position, scale));
                _occupancy.Add(position, radius, gap);

                if (definition.Tier == SizeTier.Boss)
                {
                    _bossCount++;
                }
            }
        }

        private bool IsStartAreaFree(Vector3 position, float radius, SizeTier tier, LayoutSet layout)
        {
            float centerDistance = new Vector2(position.x, position.z).magnitude;

            float clearRadius = layout.StartClearRadius;

            if (tier == SizeTier.Small)
            {
                clearRadius = layout.SmallItemStartClearRadius;
            }

            return centerDistance >= clearRadius + radius;
        }

        private Vector3 ClampToMap(Vector3 worldPosition, Bounds floorBounds, float margin)
        {
            worldPosition.x = Mathf.Clamp(worldPosition.x, floorBounds.min.x + margin, floorBounds.max.x - margin);
            worldPosition.z = Mathf.Clamp(worldPosition.z, floorBounds.min.z + margin, floorBounds.max.z - margin);
            return worldPosition;
        }
    }
}
