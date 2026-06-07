using TMPro;
using UnityEngine;

namespace UI
{
    public sealed class MassUI : MonoBehaviour
    {
        [SerializeField] private PlayerMass _playerMass;
        [SerializeField] private TMP_Text _text;

        private void OnEnable()
        {
            _playerMass.Changed += OnMassChanged;
        }

        private void OnDisable()
        {
            _playerMass.Changed -= OnMassChanged;
        }

        private void Start()
        {
            UpdateText(_playerMass.Mass);
        }

        private void OnMassChanged(int previous, int current)
        {
            UpdateText(current);
        }

        private void UpdateText(int mass)
        {
            _text.text = $"{mass}";
        }
    }
}
