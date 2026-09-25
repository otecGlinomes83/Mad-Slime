using System;
using DG.Tweening;
using Scriptables;
using UnityEngine;

namespace Player
{
    public sealed class ScalePunch : MonoBehaviour
    {
        [SerializeField] private PlayerConfig _config;
        [SerializeField] private Transform _target;

        private const float FullProgress = 1f;

        private Vector3 _baseScale;
        private float _progress;
        private Tween _squashTween;

        private void Awake()
        {
            if (_config == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerConfig is not assigned. Drag the PlayerConfig asset into the _config field.");
            }

            if (_config.SquashCurve == null || _config.SquashCurve.length == 0)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerConfig has an empty SquashCurve. Add at least one keyframe to the Collect Squash curve in PlayerConfig.");
            }

            if (_target == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Target is not assigned. Drag a Transform into the _target field.");
            }

            _baseScale = _target.localScale;
        }

        private void OnDisable()
        {
            KillTween();

            _progress = 0f;

            if (_target != null)
            {
                _target.localScale = _baseScale;
            }
        }

        public void Punch()
        {
            KillTween();

            _squashTween = DOTween.To(ReadProgress, ApplyProgress, FullProgress, _config.SquashDuration)
                .SetEase(Ease.Linear)
                .SetTarget(this)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(OnSquashCompleted);
        }

        private void OnSquashCompleted()
        {
            _progress = 0f;
            _target.localScale = _baseScale;
        }

        private float ReadProgress()
        {
            return _progress;
        }

        private void ApplyProgress(float progress)
        {
            _progress = progress;

            _target.localScale = _baseScale * _config.SquashCurve.Evaluate(progress);
        }

        private void KillTween()
        {
            if (_squashTween == null)
            {
                return;
            }

            _squashTween.Kill();
            _squashTween = null;
        }
    }
}
