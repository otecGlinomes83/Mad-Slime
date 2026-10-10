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
        private TierLookup _tierLookup;
        private ZoneLayoutPlanner _layoutPlanner = new ZoneLayoutPlanner(new System.Random());
        private List<Item> _zonePool = new List<Item>(16);
        private List<float> _zoneRadii = new List<float>(16);
        private List<LevelPlacement> _placements = new List<LevelPlacement>(256);

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
            bool mirrorX = layout.AllowMirroring == true && Random.value > 0.5f;
            bool mirrorZ = layout.AllowMirroring == true && Random.value > 0.5f;

            for (int i = 0; i < layout.Zones.Count; i++)
            {
                SpawnZone zone = layout.Zones[i];
                BuildZonePool(zone, assignedVariants, tierTable);

                if (_zonePool.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"Zone {i} ({zone.Shape}) of layout '{layout.name}' has no props in tiers " +
                        $"{zone.MinTier}-{zone.MaxTier} for level config '{config.name}'.");
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
                AppendPlacements(assignedVariants, tierTable, origin, floorBounds, spacing * 0.5f);
            }

            return _placements;
        }

        private void BuildZonePool(SpawnZone zone, Dictionary<Item, ItemDefinition> assignedVariants, TierTable tierTable)
        {
            _zonePool.Clear();
            _zoneRadii.Clear();

            foreach (KeyValuePair<Item, ItemDefinition> pair in assignedVariants)
            {
                if (pair.Value.Tier < zone.MinTier || pair.Value.Tier > zone.MaxTier)
                {
                    continue;
                }

                _zonePool.Add(pair.Key);
                _zoneRadii.Add(LevelItemsSpawner.GetRadiusXZ(pair.Key) * _tierLookup.Get(pair.Value.Tier).Scale);
            }
        }

        private void AppendPlacements(Dictionary<Item, ItemDefinition> assignedVariants, TierTable tierTable,
            Transform origin, Bounds floorBounds, float margin)
        {
            IReadOnlyList<ZoneLayoutPlanner.Placement> placements = _layoutPlanner.Placements;

            for (int i = 0; i < placements.Count; i++)
            {
                ZoneLayoutPlanner.Placement placement = placements[i];
                Item prefab = _zonePool[placement.PoolIndex];
                ItemDefinition definition = assignedVariants[prefab];
                float scale = _tierLookup.Get(definition.Tier).Scale;
                Vector3 position = ClampToMap(origin.TransformPoint(placement.Position), floorBounds, margin);
                _placements.Add(new LevelPlacement(prefab, definition, position, scale));
            }
        }

        private Vector3 ClampToMap(Vector3 worldPosition, Bounds floorBounds, float margin)
        {
            worldPosition.x = Mathf.Clamp(worldPosition.x, floorBounds.min.x + margin, floorBounds.max.x - margin);
            worldPosition.z = Mathf.Clamp(worldPosition.z, floorBounds.min.z + margin, floorBounds.max.z - margin);
            return worldPosition;
        }
    }
}
