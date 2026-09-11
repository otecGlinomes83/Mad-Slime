using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Scriptables;
using UnityEngine;

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
        }

        public async UniTask AbsorbAsync(Transform itemTransform, CancellationToken cancellationToken)
        {
            float duration = _config.AbsorptionDuration;
            Vector3 startPosition = itemTransform.position;
            Vector3 startScale = itemTransform.localScale;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                cancellationToken.ThrowIfCancellationRequested();

                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / duration);
                float smoothedProgress = Mathf.SmoothStep(0f, 1f, progress);

                itemTransform.position = Vector3.Lerp(startPosition, transform.position, smoothedProgress);
                itemTransform.localScale = Vector3.Lerp(startScale, Vector3.zero, smoothedProgress);

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }
    }
}
