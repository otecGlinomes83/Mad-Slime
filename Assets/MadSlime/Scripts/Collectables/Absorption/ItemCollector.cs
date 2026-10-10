using Cysharp.Threading.Tasks;
using Items;
using Player;
using System;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace Collectables
{
    public class ItemCollector : MonoBehaviour
    {
        private HashSet<Item> _itemsInAbsorption = new HashSet<Item>();

        private CollectAvailability _collectAvailability;
        private ItemDetector _detector;
        private Absorber _absorber;
        private bool _isRunning;
        private CancellationTokenSource _absorptionSource;

        public event Action<Item> ItemCollected;

        [Inject]
        public void Construct(CollectAvailability collectAvailability, ItemDetector detector, Absorber absorber)
        {
            _collectAvailability = collectAvailability;
            _detector = detector;
            _absorber = absorber;
        }

        private void Awake()
        {
            if (_collectAvailability == null)
            {
                throw new InvalidOperationException(
                    $"{name}: CollectAvailability was not injected. Check that GameLifetimeScope registers CollectAvailability and ItemCollector.");
            }

            if (_detector == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Detector was not injected. Check that GameLifetimeScope registers ItemDetector and ItemCollector.");
            }

            if (_absorber == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Absorber was not injected. Check that GameLifetimeScope registers Absorber and ItemCollector.");
            }
        }

        private void OnEnable()
        {
            _detector.Detected += OnItemDetected;
        }

        private void OnDisable()
        {
            _detector.Detected -= OnItemDetected;

            StopCollecting();
        }

        public void StartCollecting()
        {
            if (_absorptionSource == null)
            {
                _absorptionSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            }

            _isRunning = true;
        }

        public void PauseCollecting()
        {
            _isRunning = false;
        }

        public void StopCollecting()
        {
            _isRunning = false;

            if (_absorptionSource != null)
            {
                _absorptionSource.Cancel();
                _absorptionSource.Dispose();
                _absorptionSource = null;
            }

            foreach (Item item in _itemsInAbsorption)
            {
                if (item != null)
                {
                    item.gameObject.SetActive(false);
                }
            }

            _itemsInAbsorption.Clear();
        }

        private void OnItemDetected(Item item)
        {
            if (_isRunning == false)
            {
                return;
            }

            if (item.Definition == null)
            {
                throw new InvalidOperationException(
                    $"{name}: detected item '{item.name}' has no Definition assigned. " +
                    "Assign a definition to its prefab or delete the item from the scene.");
            }

            if (_collectAvailability.CanCollect(item.Definition.Tier) == false)
            {
                return;
            }

            if (_itemsInAbsorption.Contains(item) == true)
            {
                return;
            }

            _itemsInAbsorption.Add(item);

            if (item.TryGetComponent(out ItemBody body) == false)
            {
                throw new InvalidOperationException($"{item.name}: ItemBody is missing.");
            }

            body.Capture();
            CancellationToken cancellationToken = _absorptionSource.Token;

            ItemCollected?.Invoke(item);

                        if (cancellationToken.IsCancellationRequested == false)
            {
                AbsorbAsync(item, cancellationToken).Forget();
            }
        }

        private async UniTaskVoid AbsorbAsync(Item item, CancellationToken cancellationToken)
        {
            try
            {
                await _absorber.AbsorbAsync(item.transform, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _itemsInAbsorption.Remove(item);
                return;
            }

            _itemsInAbsorption.Remove(item);

            item.gameObject.SetActive(false);
        }
    }
}
