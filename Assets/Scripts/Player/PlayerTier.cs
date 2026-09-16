using Scriptables;
using Skills;
using System;
using UnityEngine;
using VContainer;

namespace Player
{
    public sealed class PlayerTier : MonoBehaviour
    {
        [SerializeField] private TierResolver _tierResolver;

        private int _mass;
        private PlayerConfig _config;

        public event Action<ItemTier, ItemTier> TierChanged;
        public event Action<int, int> MassChanged;

        public int Mass => _mass;
        public ItemTier CurrentTier { get; private set; } = ItemTier.Small;

        [Inject]
        public void Construct(PlayerConfig config)
        {
            _config = config;
        }

        private void Awake()
        {
            if (_tierResolver == null)
            {
                throw new InvalidOperationException(
                    $"{name}: TierResolver is not assigned. Drag a TierResolver component into the _tierResolver field.");
            }

            if (_config == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerConfig was not injected. Check that GameLifetimeScope is configured and PlayerTier is registered.");
            }

            _mass = _config.StartMass;
            CurrentTier = _tierResolver.GetUnlockedTier(_mass);
        }

        public void Add(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount),
                    "PlayerTier.Add requires amount to be non-negative. The provided value was negative.");
            }

            int previous = _mass;

            _mass += amount;
            MassChanged?.Invoke(previous, _mass);

            ItemTier previousTier = CurrentTier;
            CurrentTier = _tierResolver.GetUnlockedTier(_mass);

            TierChanged?.Invoke(previousTier, CurrentTier);
        }
    }
}
