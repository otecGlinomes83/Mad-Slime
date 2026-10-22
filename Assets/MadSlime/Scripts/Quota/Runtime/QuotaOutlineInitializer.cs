using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Items;
using Quota;
using Unity.Profiling;
using UnityEngine;

namespace Game
{
    public class QuotaOutlineInitializer
    {
        private const float FrameBudgetSeconds = 0.006f;
        private ProfilerMarker _prepareItemMarker =
            new ProfilerMarker("QuotaOutlineInitializer.PrepareItem");

        private QuotaBoard _board;

        public QuotaOutlineInitializer(QuotaBoard board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            _board = board;
        }

        public async UniTask PrepareAsync(IReadOnlyList<Item> items, Color color, float width,
            Action<float> progress, CancellationToken cancellationToken)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            cancellationToken.ThrowIfCancellationRequested();
            HashSet<ItemDefinition> quotaTypes = new HashSet<ItemDefinition>();

            foreach (QuotaEntry entry in _board.Entries)
            {
                quotaTypes.Add(entry.Definition);
            }

            if (quotaTypes.Count == 0)
            {
                progress?.Invoke(1f);
                return;
            }

            float frameStart = Time.realtimeSinceStartup;

            for (int i = 0; i < items.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Item item = items[i];

                if (item != null && quotaTypes.Contains(item.Definition))
                {
                    PrepareItem(item, color, width);
                }

                if (Time.realtimeSinceStartup - frameStart >= FrameBudgetSeconds)
                {
                    progress?.Invoke((i + 1) / (float)items.Count);
                    await UniTask.NextFrame(cancellationToken: cancellationToken);
                    frameStart = Time.realtimeSinceStartup;
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            progress?.Invoke(1f);
        }

        private void PrepareItem(Item item, Color color, float width)
        {
            using ProfilerMarker.AutoScope prepareItemScope = _prepareItemMarker.Auto();

            if (item.TryGetComponent(out GhostAnimator animator) == false)
            {
                throw new InvalidOperationException($"{item.name}: quota item requires a GhostAnimator.");
            }

            if (item.TryGetComponent(out Outline outline) == false)
            {
                outline = item.gameObject.AddComponent<Outline>();
            }

            outline.Prepare(color, width);
            animator.BindOutline(outline);
        }
    }
}
