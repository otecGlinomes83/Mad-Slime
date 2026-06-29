using Audio;
using Game;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public sealed class PauseMenu : BaseWindow
    {
        [SerializeField] private Button _closeButton;
        [SerializeField] private AudioSettingsPanel _settingsPanel;

        public void Initialize(Pauser pauser, AudioMixerController audioMixerController)
        {
            Initialize(pauser);
            _closeButton.onClick.AddListener(Close);

            _settingsPanel.Initialize(audioMixerController);
        }

        protected override void OnDisable()
        {
            _closeButton?.onClick.RemoveListener(Close);
            base.OnDisable();
        }

        private void Close()
        {
            Destroy(gameObject);
        }
    }
}