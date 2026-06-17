using System;
using System.Threading;
using Item;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class Collector : MonoBehaviour
{
    [SerializeField] private MonoBehaviour _massHolderSource;
    [SerializeField] private ItemDetector _detector;
    [SerializeField] private float _absorptionDuration = 0.3f;

    private IMassHolder _massHolder;
    
    public event Action<Item.Item> ItemCollected;

    private void Awake()
    {
        if (_massHolderSource.TryGetComponent(out IMassHolder massHolder))
        {
            _massHolder = massHolder;
        }
        else
        {
            throw new InvalidOperationException(
                $"SessionHandler: {_massHolderSource.name} does not implement IMassHolder.");
        }
    }

    private void OnEnable()
    {
        _detector.Detected += OnItemDetected;
    }

    private void OnDisable()
    {
        _detector.Detected -= OnItemDetected;
    }

    private void OnItemDetected(Item.Item item)
    {
        if (_massHolder.Mass < item.Mass)
        {
            return;
        }

        item.Collect();
        AbsorbAsync(item).Forget();
    }

    private async UniTaskVoid AbsorbAsync(Item.Item item)
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