using System;
using Game;
using TMPro;
using UnityEngine;

namespace UI
{
    [RequireComponent(typeof(TMP_Text))]
    public sealed class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string _key;

        private TMP_Text _text;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            Localization.LanguageChanged += Apply;
            Apply();
        }

        private void OnDisable()
        {
            Localization.LanguageChanged -= Apply;
        }

        private void Apply()
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
