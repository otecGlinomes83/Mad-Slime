using Scriptables;
using System;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Audio
{
    [RequireComponent(typeof(Button))]
    public sealed class UIButtonSound : MonoBehaviour
    {
        [SerializeField] private SfxClip _sfxClip;

        private Button _button;
        private SfxPlayer _sfxPlayer;

        [Inject]
        public void Construct(SfxPlayer sfxPlayer)
        {
            _sfxPlayer = sfxPlayer;
        }

        private void Awake()
        {
            _button = GetComponent<Button>();

            if (_sfxPlayer == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SfxPlayer was not injected. The scene scope must inject this UIButtonSound: drag the component into the scope's UI Button Sounds list (runtime-spawned objects are injected automatically via IObjectResolver.Instantiate).");
            }

            if (_sfxClip == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SfxClip is not assigned. Drag a SfxClip asset into the _sfxClip field.");
            }

            if (_button == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Button component is missing. UIButtonSound requires the Button component on the same object.");
            }
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(PlayClick);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(PlayClick);
        }

        private void PlayClick()
        {
            _sfxPlayer.PlayUi(_sfxClip);
        }
    }
}