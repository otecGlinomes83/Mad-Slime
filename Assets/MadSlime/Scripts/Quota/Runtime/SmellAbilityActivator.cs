using System;
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
        private bool _isAvailable;
        private bool _isPaused = true;

        [Inject]
        public void Construct(PlayerUpgrades upgrades, QuotaItemDetector detector)
        {
            _upgrades = upgrades;
            _detector = detector;
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
                    $"{name}: PlayerUpgrades was not injected. Check that ProjectLifetimeScope registers PlayerUpgrades and SmellAbilityActivator.");
            }
        }

        public void Apply()
        {
            _isAvailable = _upgrades.HasSmell();
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
