using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class Absorber : MonoBehaviour
{
    [SerializeField] private float _absorptionDuration = 0.3f;

    public async UniTask AbsorbAsync(Transform itemTransform, CancellationToken cancellationToken)
    {
        Vector3 startPosition = itemTransform.position;
        Vector3 startScale = itemTransform.localScale;
        float elapsedTime = 0f;

        while (elapsedTime < _absorptionDuration)
        {
            cancellationToken.ThrowIfCancellationRequested();

            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / _absorptionDuration);
            float smoothedProgress = Mathf.SmoothStep(0f, 1f, progress);

            itemTransform.position = Vector3.Lerp(startPosition, transform.position, smoothedProgress);
            itemTransform.localScale = Vector3.Lerp(startScale, Vector3.zero, smoothedProgress);

            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }
    }
}