using System;
using Cysharp.Threading.Tasks;
using Items;
using Player;
using UnityEngine;
using VContainer;

namespace Collectables
{
    public sealed class Collector : MonoBehaviour
    {
        private PlayerTier _tierHolder;
        private ItemDetector _detector;
        private Absorber _absorber;

        public event Action<Items.Item> ItemCollected;

        [Inject]
        public void Construct(PlayerTier tierHolder, ItemDetector detector, Absorber absorber)
        {
            _tierHolder = tierHolder;
            _detector = detector;
            _absorber = absorber;
        }

        private void Awake()
        {
            if (_tierHolder == null)
            {
                throw new InvalidOperationException(
                    $"{name}: TierHolder was not injected. Check that GameLifetimeScope registers PlayerTier and Collector.");
            }

            if (_detector == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Detector was not injected. Check that GameLifetimeScope registers ItemDetector and Collector.");
            }

            if (_absorber == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Absorber was not injected. Check that GameLifetimeScope registers Absorber and Collector.");
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

        private void OnItemDetected(Items.Item item)
        {
            if (item.Definition == null)
            {
                Debug.LogError(
                    $"{name}: detected item '{item.name}' has no Definition assigned. " +
                    "Assign a definition to its prefab or delete the item from the scene.",
                    item.gameObject);
                return;
            }

            if (item.Definition.Tier > _tierHolder.CurrentTier)
            {
                return;
            }

            CollectAsync(item).Forget();
        }

        private async UniTaskVoid CollectAsync(Items.Item item)
        {
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