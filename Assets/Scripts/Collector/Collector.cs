using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class Collector : MonoBehaviour
{
    [SerializeField] private CollectableDetector _detector;
    [SerializeField] private float _absorptionDuration = 0.3f;

    public event Action<int> OnCollect;

    private void OnEnable()
    {
        _detector.Detected += OnCollectableDetected;
    }

    private void OnDisable()
    {
        _detector.Detected -= OnCollectableDetected;
    }

    private void OnCollectableDetected(ICollectable collectable, Transform collectableTransform)
    {
        collectable.Collect();
        AbsorbAsync(collectable, collectableTransform).Forget();
    }

    private async UniTaskVoid AbsorbAsync(ICollectable collectable, Transform collectableTransform)
    {
        CancellationToken cancellationToken = this.GetCancellationTokenOnDestroy();

        try
        {
            await AnimateAbsorptionAsync(collectableTransform, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        int mass = collectable.Mass;
        collectable.Release();

        OnCollect?.Invoke(mass);
    }

    private async UniTask AnimateAbsorptionAsync(Transform collectableTransform, CancellationToken cancellationToken)
    {
        Vector3 startPosition = collectableTransform.position;
        Vector3 startScale = collectableTransform.localScale;
        float elapsedTime = 0f;

        while (elapsedTime < _absorptionDuration)
        {
            cancellationToken.ThrowIfCancellationRequested();

            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / _absorptionDuration);
            float smoothedProgress = Mathf.SmoothStep(0f, 1f, progress);

            collectableTransform.position = Vector3.Lerp(startPosition, transform.position, smoothedProgress);
            collectableTransform.localScale = Vector3.Lerp(startScale, Vector3.zero, smoothedProgress);

            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }
    }
}