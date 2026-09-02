using Skills;
using System;
using UnityEngine;

namespace Player
{
    public sealed class PlayerTier : MonoBehaviour
    {
        [SerializeField] private int _defaultMass;
        [SerializeField] private int _massPickupDivisor = 4;
        [SerializeField] private TierResolver _tierResolver;

        private int _mass;

        public event Action<ItemTier, ItemTier> TierChanged;
        public event Action<int, int> MassChanged;

        public int Mass => _mass;
        public ItemTier CurrentTier { get; private set; } = ItemTier.Small;

        private void Awake()
        {
            if (_tierResolver == null)
            {
                throw new InvalidOperationException(
                    $"{name}: TierResolver is not assigned. Drag a TierResolver component into the _tierResolver field.");
            }

            _mass = _defaultMass;
            CurrentTier = _tierResolver.GetUnlockedTier(_mass);
            TierChanged?.Invoke(CurrentTier, CurrentTier);
            MassChanged?.Invoke(_defaultMass, _mass);
        }

        public void Add(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount),
                    "PlayerTier.Add requires amount to be non-negative. The provided value was negative.");
            }

            int previous = _mass;

            int scaledMass = Mathf.RoundToInt(amount / (float)_massPickupDivisor);
            scaledMass = Mathf.Max(1, scaledMass);

            _mass += scaledMass;
            MassChanged?.Invoke(previous, _mass);

            ItemTier previousTier = CurrentTier;
            CurrentTier = _tierResolver.GetUnlockedTier(_mass);

            TierChanged?.Invoke(previousTier, CurrentTier);
        }
    }
}
