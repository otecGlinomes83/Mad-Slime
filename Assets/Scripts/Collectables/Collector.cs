using System;
using Cysharp.Threading.Tasks;
using Interfaces;
using Item;
using Player;
using UnityEngine;

namespace Collectables
{
    public sealed class Collector : MonoBehaviour
    {
    [SerializeField] private MonoBehaviour _massHolderSource;
    [SerializeField] private ItemDetector _detector;
    [SerializeField] private Absorber _absorber;

    private PlayerTier _tierHolder;

    public event Action<Item.Item> ItemCollected;

    private void Awake()
    {
        if (_massHolderSource.TryGetComponent(out PlayerTier massHolder) == false)
        {
            throw new InvalidOperationException(
                $"Collector: {_massHolderSource.name} does not implement PlayerMass.");
        }

        _tierHolder = massHolder;
    }

    private void OnEnable()
    {
        _detector.Detected += OnItemDetected;
    }

    private void OnDisable()
    {
        _detector.Detected -= OnItemDetected;
    }

    private async void OnItemDetected(Item.Item item)
    {
        if (item.Definition.Tier > _tierHolder.MaxUnlockedTier)
        {
            return;
        }

        item.Collect();

        try
        {
            await _absorber.AbsorbAsync(item.transform, this.GetCancellationTokenOnDestroy());
        }
        catch (OperationCanceledException)
        {
            return;
        }

        item.Shutdown();
        ItemCollected?.Invoke(item);
    }
    }
}
