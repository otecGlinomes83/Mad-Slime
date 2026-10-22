using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Items;
using Collectables;
using UnityEngine;
using Upgrades;
using VContainer;

namespace Game
{
    public class SmellAbilityActivator : MonoBehaviour
    {
        [SerializeField] private QuotaItemHighlighter _highlighter;

        private PlayerUpgrades _upgrades;
        private QuotaItemDetector _detector;
        private QuotaOutlineInitializer _outlineInitializer;
        private bool _isAvailable;
        private bool _isPaused = true;

        [Inject]
        public void Construct(PlayerUpgrades upgrades, QuotaItemDetector detector,
            QuotaOutlineInitializer outlineInitializer)
        {
            _upgrades = upgrades;
            _detector = detector;
            _outlineInitializer = outlineInitializer;
        }

        private void Awake()
        {
            if (_highlighter == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Highlighter is not assigned. Drag the QuotaItemHighlighter component into the _highlighter field.");
            }

            if (_upgrades == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerUpgrades was not injected. Check that ProjectLifetimeScope "
                        + $"registers PlayerUpgrades and SmellAbilityActivator.");
            }
        }

        public async UniTask PrepareAsync(IReadOnlyList<Item> items, Action<float> progress,
            CancellationToken cancellationToken)
        {
            _isAvailable = false;
            _isPaused = true;
            ApplyState();
            cancellationToken.ThrowIfCancellationRequested();

            if (_upgrades.HasSmell() == false)
            {
                progress?.Invoke(1f);
                return;
            }

            await _outlineInitializer.PrepareAsync(items, _highlighter.HighlightColor,
                _highlighter.OutlineWidth, progress, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            _isAvailable = true;
            ApplyState();
        }

        public void SetPaused(bool isPaused)
        {
            _isPaused = isPaused;
            ApplyState();
        }

        private void ApplyState()
        {
            bool isEnabled = _isAvailable && _isPaused == false;
            _detector.enabled = isEnabled;
            _highlighter.enabled = isEnabled;
        }
    }
}
