using Player;
using System;
using TMPro;
using UnityEngine;
using VContainer;

namespace UI
{
    public sealed class MassUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;

        private PlayerTier _playerTier;

        [Inject]
        public void Construct(PlayerTier playerTier)
        {
            _playerTier = playerTier;
        }

        private void Awake()
        {
            if (_playerTier == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerTier was not injected. Check that GameLifetimeScope registers PlayerTier and MassUI.");
            }

            if (_text == null)
            {
                throw new InvalidOperationException(
                    $"{name}: TMP_Text is not assigned. Drag a TMP_Text component into the _text field.");
            }
        }

        private void OnEnable()
        {
            _playerTier.MassChanged += OnTierChanged;
        }

        private void OnDisable()
        {
            _playerTier.MassChanged -= OnTierChanged;
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