using System;
using Audio;
using Game;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class PauseMenu : BaseWindow
    {
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private AudioSettingsPanel _settingsPanel;

        private Action _restartAction;

        public void Initialize(Pauser pauser, AudioMixerController audioMixerController, bool showRestart, Action restartAction)
        {
            Initialize(pauser);
            _closeButton.onClick.AddListener(Close);

            _restartAction = restartAction;

            if (_restartButton != null)
            {
                bool canShow = showRestart == true && restartAction != null;
                _restartButton.gameObject.SetActive(canShow);

                if (canShow == true)
                {
                    _restartButton.onClick.AddListener(OnRestartClicked);
                }
            }

            _settingsPanel.Initialize(audioMixerController);
        }

        protected override void OnDisable()
        {
            _closeButton?.onClick.RemoveListener(Close);
            _restartButton?.onClick.RemoveListener(OnRestartClicked);
            base.OnDisable();
        }

        private void OnRestartClicked()
        {
            _restartAction?.Invoke();
            Close();
        }

        private void Close()
        {
            Destroy(gameObject);
        }
    }
}