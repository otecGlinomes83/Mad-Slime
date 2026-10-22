using Scriptables;
using System;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Audio
{
    [RequireComponent(typeof(Button))]
    public class UIButtonSound : MonoBehaviour
    {
        [SerializeField] private SfxClip _sfxClip;

        private Button _button;
        private IUISoundPlayer _soundPlayer;

        [Inject]
        public void Construct(IUISoundPlayer soundPlayer)
        {
            _soundPlayer = soundPlayer;
        }

        private void Awake()
        {
            if (TryGetComponent(out _button) == false)
            {
                throw new InvalidOperationException(
                    $"{name}: Button component is missing. UIButtonSound requires the Button component on the same object.");
            }

            if (_soundPlayer == null)
            {
                throw new InvalidOperationException(
                    $"{name}: IUISoundPlayer was not injected. Scene buttons are injected by the "
                        + $"scene LifetimeScope; spawned buttons must use IObjectResolver.Instantiate.");
            }

            if (_sfxClip == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SfxClip is not assigned. Drag a SfxClip asset into the _sfxClip field.");
            }
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(PlayClick);
        }

        private void OnDisable()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(PlayClick);
            }
        }

        private void PlayClick()
        {
            _soundPlayer.Play(_sfxClip);
        }
    }
}
