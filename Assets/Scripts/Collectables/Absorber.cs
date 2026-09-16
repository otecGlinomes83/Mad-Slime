using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Scriptables;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Collectables
{
    public sealed class Absorber : MonoBehaviour
    {
        [SerializeField] private PlayerConfig _config;

        private void Awake()
        {
            if (_config == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerConfig is not assigned. Drag the PlayerConfig asset into the _config field.");
            }

            if (_config.MinAbsorbDuration > _config.MaxAbsorbDuration)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerConfig has MinAbsorbDuration {_config.MinAbsorbDuration} greater than MaxAbsorbDuration {_config.MaxAbsorbDuration}.");
            }

            if (_config.MinArcFraction > _config.MaxArcFraction)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerConfig has MinArcFraction {_config.MinArcFraction} greater than MaxArcFraction {_config.MaxArcFraction}.");
            }

            if (_config.MinSpinSpeed > _config.MaxSpinSpeed)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerConfig has MinSpinSpeed {_config.MinSpinSpeed} greater than MaxSpinSpeed {_config.MaxSpinSpeed}.");
            }
        }

        public async UniTask AbsorbAsync(Transform itemTransform, CancellationToken cancellationToken)
        {
            Vector3 startPosition = itemTransform.position;
            Vector3 startScale = itemTransform.localScale;
            float duration = CalculateDuration(startPosition, transform.position);
            Vector3 controlPosition = CalculateControlPosition(startPosition, transform.position);
            Vector3 spinAxis = Random.onUnitSphere;
            float spinSpeed = Random.Range(_config.MinSpinSpeed, _config.MaxSpinSpeed);
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                cancellationToken.ThrowIfCancellationRequested();

                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / duration);
                float easedProgress = Mathf.Pow(progress, _config.AbsorbEasePower);

                itemTransform.position = GetBezierPoint(startPosition, controlPosition, transform.position, easedProgress);
                itemTransform.Rotate(spinAxis, spinSpeed * Time.deltaTime, Space.World);

                float shrink = Mathf.Clamp01(Mathf.InverseLerp(_config.ShrinkStart, 1f, progress));
                itemTransform.localScale = startScale * (1f - shrink);

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }

        private float CalculateDuration(Vector3 startPosition, Vector3 endPosition)
        {
            float distance = Vector3.Distance(startPosition, endPosition);

            return Mathf.Clamp(distance / _config.AbsorbSpeed, _config.MinAbsorbDuration, _config.MaxAbsorbDuration);
        }

        private Vector3 CalculateControlPosition(Vector3 startPosition, Vector3 endPosition)
        {
            Vector3 offset = endPosition - startPosition;
            float distance = offset.magnitude;

            if (distance < 0.001f)
            {
                return Vector3.Lerp(startPosition, endPosition, 0.5f);
            }

            Vector3 perpendicular = Vector3.Cross(offset / distance, Vector3.up);

            if (perpendicular.sqrMagnitude < 0.001f)
            {
                perpendicular = Vector3.right;
            }
            else
            {
                perpendicular.Normalize();
            }

            float side = 1f;

            if (Random.value < 0.5f)
            {
                side = -1f;
            }

            float arcMagnitude = distance * Random.Range(_config.MinArcFraction, _config.MaxArcFraction) * side;

            return Vector3.Lerp(startPosition, endPosition, 0.5f) + perpendicular * arcMagnitude;
        }

        private static Vector3 GetBezierPoint(Vector3 start, Vector3 control, Vector3 end, float progress)
        {
            float invertedProgress = 1f - progress;

            return invertedProgress * invertedProgress * start
                + 2f * invertedProgress * progress * control
                + progress * progress * end;
        }
    }
}
