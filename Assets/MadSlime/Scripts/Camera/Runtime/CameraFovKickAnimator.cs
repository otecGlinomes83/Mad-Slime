using DG.Tweening;
using Player;
using Scriptables;
using Skills;
using System;
using UnityEngine;
using VContainer;

namespace CameraSystem
{
    public class CameraFovKickAnimator : MonoBehaviour
    {
        [SerializeField] private CameraImpulseConfig _config;

        private PlayerTier _playerTier;
        private float _fovKick;
        private Tween _recoverTween;

        public float FovKick => _fovKick;

        [Inject]
        public void Construct(PlayerTier playerTier)
        {
            _playerTier = playerTier;
        }

        private void Awake()
        {
            if (_config == null)
            {
                throw new InvalidOperationException(
                    $"{name}: CameraImpulseConfig is not assigned. Drag the CameraImpulseConfig asset into the _config field.");
            }
        }

        private void OnEnable()
        {
            _playerTier.TierChanged += OnTierChanged;
        }

        private void OnDisable()
        {
            _playerTier.TierChanged -= OnTierChanged;

            KillRecoverTween();
        }

        private void OnTierChanged(SizeTier previousTier, SizeTier currentTier)
        {
            if (currentTier <= previousTier)
            {
                return;
            }

            KillRecoverTween();

            _fovKick = _config.TierFovKick;

            float recoverDuration = 1f / _config.FovRecoverSpeed;

            _recoverTween = DOTween.To(ReadFovKick, ApplyFovKick, 0f, recoverDuration)
                .SetEase(Ease.OutQuad)
                .SetTarget(this)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        private void KillRecoverTween()
        {
            if (_recoverTween == null)
            {
                return;
            }

            _recoverTween.Kill();
            _recoverTween = null;
        }

        private float ReadFovKick()
        {
            return _fovKick;
        }

        private void ApplyFovKick(float fovKick)
        {
            _fovKick = fovKick;
        }
    }
}
