using System;
using Cysharp.Threading.Tasks;
using Items;
using Player;
using UnityEngine;

namespace Collectables
{
    public sealed class Collector : MonoBehaviour
    {
        [SerializeField] private PlayerTier _tierHolder;
        [SerializeField] private ItemDetector _detector;
        [SerializeField] private Absorber _absorber;
        [SerializeField] private WeightPopup _weightPopup;

        public event Action<Items.Item> ItemCollected;

        private void Awake()
        {
            if (_tierHolder == null)
            {
                throw new InvalidOperationException(
                    $"{name}: TierHolder is not assigned. Drag a PlayerTier component into the _tierHolder field.");
            }

            if (_detector == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Detector is not assigned. Drag an ItemDetector component into the _detector field.");
            }

            if (_absorber == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Absorber is not assigned. Drag an Absorber component into the _absorber field.");
            }

            if (_weightPopup == null)
            {
                throw new InvalidOperationException(
                    $"{name}: WeightPopup is not assigned. Drag a WeightPopup component into the _weightPopup field.");
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
            Vector3 itemPosition = item.transform.position;
            int itemMass = item.Mass;

            _weightPopup.Show(itemPosition, itemMass);

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
