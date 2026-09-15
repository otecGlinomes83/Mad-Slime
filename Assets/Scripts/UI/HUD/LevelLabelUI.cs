using Game;
using TMPro;
using UnityEngine;
using VContainer;

namespace UI
{
    public sealed class LevelLabelUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _labelText;

        private PlayerProgress _progress;

        [Inject]
        public void Construct(PlayerProgress progress)
        {
            _progress = progress;
        }

        private void OnEnable()
        {
            Localization.LanguageChanged += UpdateLabel;
            UpdateLabel();
        }

        private void OnDisable()
        {
            Localization.LanguageChanged -= UpdateLabel;
        }

        private void Start()
        {
            UpdateLabel();
        }

        private void UpdateLabel()
        {
            if (_progress == null || _labelText == null)
            {
                return;
            }

            _labelText.text = string.Format(Localization.Get("level_label"), _progress.CurrentLevel);
        }
    }
}
