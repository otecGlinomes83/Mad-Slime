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
                    $"{name}: SfxPlayer was not injected. The object must be spawned through the DI container (IObjectResolver.Instantiate) or its scene scope must be the first object in the scene hierarchy.");
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
            _sfxPlayer.Play(_sfxClip);
        }
    }
}