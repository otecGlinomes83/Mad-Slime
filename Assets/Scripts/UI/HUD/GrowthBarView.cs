using Player;
using Skills;
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
            _tierText.text = _tierResolver.GetTierLabelFor(_playerTier.CurrentTier);
        }
    }
}
