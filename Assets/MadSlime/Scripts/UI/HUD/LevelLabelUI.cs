using System;
using Game;
using TMPro;
using UnityEngine;

namespace UI
{
    public class LevelLabelUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _labelText;

        private int _level;

        private void OnEnable()
        {
            Localization.LanguageChanged += UpdateLabel;
            UpdateLabel();
        }

        private void OnDisable()
        {
            Localization.LanguageChanged -= UpdateLabel;
        }

        public void SetLevel(int level)
        {
            if (level <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }

            _level = level;
            UpdateLabel();
        }

        private void UpdateLabel()
        {
            if (_level <= 0)
            {
                return;
            }

            _labelText.text = string.Format(Localization.Get("level_label"), _level);
        }
    }
}
