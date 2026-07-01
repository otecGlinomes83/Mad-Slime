using TMPro;
using UnityEngine;

namespace UI
{
    public sealed class MassUI : MonoBehaviour
    {
        [SerializeField] private PlayerTier _playerTier;
        [SerializeField] private TMP_Text _text;

        private void OnEnable()
        {
            _playerTier.Changed += OnTierChanged;
        }

        private void OnDisable()
        {
            _playerTier.Changed -= OnTierChanged;
        }

        private void Start()
        {
            UpdateText(_playerTier.Mass);
        }

        private void OnTierChanged(int previous, int current)
        {
            UpdateText(current);
        }

        private void UpdateText(int mass)
        {
            _text.text = $"{mass}";
        }
    }
}
