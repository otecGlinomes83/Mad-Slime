using Game;
using Player;
using Skills;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class GrowthBarView : MonoBehaviour
    {
        [SerializeField] private PlayerTier _playerTier;
        [SerializeField] private TierResolver _tierResolver;
        [SerializeField] private Image _progressBar;
        [SerializeField] private TMP_Text _tierText;

        private void Awake()
        {
            if (_playerTier == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerTier is not assigned. Drag a PlayerTier component into the _playerTier field.");
            }

            if (_tierResolver == null)
            {
                throw new InvalidOperationException(
                    $"{name}: TierResolver is not assigned. Drag a TierResolver component into the _tierResolver field.");
            }

            if (_progressBar == null)
            {
                throw new InvalidOperationException(
                    $"{name}: ProgressBar is not assigned. Drag an Image into the _progressBar field.");
            }

            if (_tierText == null)
            {
                throw new InvalidOperationException(
                    $"{name}: TierText is not assigned. Drag a TMP_Text into the _tierText field.");
            }
        }

        private void OnEnable()
        {
            _playerTier.MassChanged += OnMassChanged;
            _playerTier.TierChanged += OnTierChanged;
        }

        private void Start()
        {
            Refresh(_playerTier.Mass);
        }

        private void OnDisable()
        {
            _playerTier.MassChanged -= OnMassChanged;
            _playerTier.TierChanged -= OnTierChanged;
        }

        private void OnMassChanged(int previousMass, int currentMass)
        {
            Refresh(currentMass);
        }

        private void OnTierChanged(ItemTier previousTier, ItemTier currentTier)
        {
            Refresh(_playerTier.Mass);
        }

        private void Refresh(int mass)
        {
            _progressBar.fillAmount = _tierResolver.GetTierProgress(mass);
            _tierText.text = Localization.Get($"tier_{_playerTier.CurrentTier}");
        }
    }
}
