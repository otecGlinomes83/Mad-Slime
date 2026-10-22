using System;
using DG.Tweening;
using Scriptables;
using UnityEngine;

namespace Player
{
    public class ScalePunch : MonoBehaviour
    {
        [SerializeField] private PlayerConfig _config;
        [SerializeField] private ModelDeformer _deformer;

        private const float FullProgress = 1f;

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
                    $"{name}: PlayerConfig has an empty SquashCurve. Add at least one keyframe to "
                        + $"the Collect Squash curve in PlayerConfig.");
            }

            if (_deformer == null)
            {
                throw new InvalidOperationException(
                    $"{name}: ModelDeformer is not assigned.");
            }

        }

        private void OnDisable()
        {
            KillTween();

            _progress = 0f;

            if (_deformer != null)
            {
                _deformer.SetCollectCompression(0f);
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
            _deformer.SetCollectCompression(0f);
        }

        private float ReadProgress()
        {
            return _progress;
        }

        private void ApplyProgress(float progress)
        {
            _progress = progress;

            float compression = 1f - Mathf.Clamp01(_config.SquashCurve.Evaluate(progress));
            _deformer.SetCollectCompression(compression);
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
