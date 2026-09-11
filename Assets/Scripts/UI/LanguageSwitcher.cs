using System;
using Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class LanguageSwitcher : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _label;

        private void Awake()
        {
            if (_button == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Button is not assigned. Drag a Button into the _button field.");
            }

            if (_label == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Label is not assigned. Drag a TMP_Text into the _label field.");
            }
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(OnButtonClick);
            Localization.LanguageChanged += Apply;
            Apply();
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnButtonClick);
            Localization.LanguageChanged -= Apply;
        }

        private void OnButtonClick()
        {
            Localization.CycleLanguage();
        }

        private void Apply()
        {
            _label.text = Localization.Get($"lang_self_{Localization.CurrentLanguage}");
        }
    }
}
