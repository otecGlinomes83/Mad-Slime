using System;
using System.Threading;
using Collectables;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class Collector : MonoBehaviour
{
    [SerializeField] private Player _player;
    [SerializeField] private CollectableDetector _detector;
    [SerializeField] private float _absorptionDuration = 0.3f;

    public event Action<Item> ItemCollected;

    private void OnEnable()
    {
        _detector.Detected += OnItemDetected;
    }

    private void OnDisable()
    {
        _detector.Detected -= OnItemDetected;
    }

    private void OnItemDetected(Item item)
    {
        if (_player.Mass < item.Mass)
        {
            return;
        }

        item.Collect();
        AbsorbAsync(item).Forget();
    }

    private async UniTaskVoid AbsorbAsync(Item item)
    {
        CancellationToken cancellationToken = this.GetCancellationTokenOnDestroy();

        try
        {
            await AnimateAbsorptionAsync(item.transform, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        item.Release();
        ItemCollected?.Invoke(item);
    }

    private async UniTask AnimateAbsorptionAsync(Transform itemTransform, CancellationToken cancellationToken)
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