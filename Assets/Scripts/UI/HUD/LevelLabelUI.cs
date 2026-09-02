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
            UpdateLabel();
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

            _labelText.text = $"Уровень {_progress.CurrentLevel}";
        }
    }
}
