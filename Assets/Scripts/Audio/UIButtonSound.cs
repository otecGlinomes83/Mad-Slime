using Scriptables;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Audio
{
    public sealed class UIButtonSound : MonoBehaviour
    {
        [SerializeField] private SfxClip _sfxClip;
        [SerializeField] private List<Button> _buttons;

        private SfxPlayer _sfxPlayer;

        [Inject]
        public void Construct(SfxPlayer sfxPlayer)
        {
            _sfxPlayer = sfxPlayer;
        }

        private void Awake()
        {
            if (_sfxPlayer == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SfxPlayer was not injected. The object must be spawned through the DI container (IObjectResolver.Instantiate) or its scene scope must be the first object in the scene hierarchy.");
            }

            if (_sfxClip == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SfxClip is not assigned. Drag a SfxClip asset into the _sfxClip field.");
            }

            if (_buttons.Count <= 0)
            {
                throw new InvalidOperationException(
                    $"{name}: buttons is empty");
            }
        }

        private void OnEnable()
        {
            foreach (Button button in _buttons)
            {
                button.onClick.AddListener(PlayClick);
            }
        }

        private void OnDisable()
        {
            foreach (Button button in _buttons)
            {
                button.onClick.RemoveListener(PlayClick);
            }
        }

        public void AddButton(Button button)
        {
            _buttons.Add(button);
            button.onClick.AddListener(PlayClick);
        }

        private void PlayClick()
        {
            _sfxPlayer.Play(_sfxClip);
        }
    }
}