using System;
using Game;
using TMPro;
using UnityEngine;

namespace UI
{
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string _key;

        private TMP_Text _text;

        private void Awake()
        {
            if (TryGetComponent(out _text) == false)
            {
                throw new InvalidOperationException($"{name}: TMP_Text is required.");
            }
        }

        private void OnEnable()
        {
            Localization.LanguageChanged += OnLanguageChanged;
            OnLanguageChanged();
        }

        private void OnDisable()
        {
            Localization.LanguageChanged -= OnLanguageChanged;
        }

        private void OnLanguageChanged()
        {
            if (_key == null || _key == string.Empty)
            {
                throw new InvalidOperationException(
                    $"{name}: LocalizedText key is empty. Fill the _key field.");
            }

            _text.text = Localization.Get(_key);
        }
    }
}
