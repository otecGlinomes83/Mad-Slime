using System.Collections.Generic;
using Items;
using Quota;
using Scriptables;
using Skills;
using UnityEngine;

namespace Game
{
    public sealed class QuotaGenerator
    {
        private readonly List<ItemDefinition> _candidates = new List<ItemDefinition>();
        private readonly Dictionary<ItemTier, int> _tierEntryCounts = new Dictionary<ItemTier, int>();

        public List<QuotaEntry> Generate(Dictionary<ItemDefinition, int> spawnedCounts, LevelConfig config)
        {
            CollectCandidates(spawnedCounts, config);

            int typesTarget = Random.Range(config.QuotaTypesMin, config.QuotaTypesMax + 1);
            typesTarget = Mathf.Clamp(typesTarget, 0, _candidates.Count);

            ShuffleCandidates();

            List<QuotaEntry> entries = new List<QuotaEntry>(typesTarget);
            _tierEntryCounts.Clear();

            for (int i = 0; i < _candidates.Count && entries.Count < typesTarget; i++)
            {
                ItemDefinition definition = _candidates[i];
                ItemTier tier = definition.Tier;

                _tierEntryCounts.TryGetValue(tier, out int tierCount);

                if (tierCount >= config.QuotaMaxSameTier)
                {
                    continue;
                }

                int target = Random.Range(config.QuotaTargetMin, config.QuotaTargetMax + 1);
                int spawned = spawnedCounts[definition];

                if (target > spawned)
                {
                    Debug.LogWarning($"[Quota] {definition.name}: quota reduced from {target} to {spawned} — not enough items spawned.");
                    target = spawned;
                }

                entries.Add(new QuotaEntry(definition, target));
                _tierEntryCounts[tier] = tierCount + 1;
            }

            _candidates.Clear();

            return entries;
        }

        private void CollectCandidates(Dictionary<ItemDefinition, int> spawnedCounts, LevelConfig config)
        {
            foreach (KeyValuePair<ItemDefinition, int> pair in spawnedCounts)
            {
                if (pair.Value >= config.QuotaTargetMin)
                {
                    _candidates.Add(pair.Key);
                }
            }
        }

        private void ShuffleCandidates()
        {
            for (int i = _candidates.Count - 1; i > 0; i--)
            {
                int swapIndex = Random.Range(0, i + 1);
                (_candidates[i], _candidates[swapIndex]) = (_candidates[swapIndex], _candidates[i]);
            }
        }
    }
}
