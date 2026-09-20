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
        [SerializeField] private Button _menuButton;
        [SerializeField] private AudioSettingsPanel _settingsPanel;
        
        private Action _menuAction;

        public void Initialize(Pauser pauser, AudioMixerController audioMixerController, bool isMenuButtonNeeded,
            Action menuAction = null)
        {
            Initialize(pauser);
            _closeButton.onClick.AddListener(Close);
            _menuButton.gameObject.SetActive(false);

            if (isMenuButtonNeeded && menuAction != null)
            {
                _menuButton.gameObject.SetActive(true);
                _menuButton.onClick.AddListener(OnMenuClicked);
                _menuAction = menuAction;
            }

            _settingsPanel.Initialize(audioMixerController);
        }

        protected override void OnDisable()
        {
            _closeButton?.onClick.RemoveListener(Close);
            _menuButton?.onClick.RemoveListener(OnMenuClicked);
            base.OnDisable();
        }

        private void OnMenuClicked()
        {
            _menuAction?.Invoke();
            Close();
        }

        private void Close()
        {
            Destroy(gameObject);
        }
    }
}