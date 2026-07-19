using HealthSystem;
using Skills;
using System;
using UnityEngine;

namespace Player
{
    public class PlayerTier : MonoBehaviour
    {
    [SerializeField] private int _defaultMass;
    [SerializeField] private Health _playerHealth;
    [SerializeField] private int _massPickupDivisor = 4;
    [SerializeField] private TierResolver _tierResolver;

    private int _mass;

    public int Mass => _mass;
    public ItemTier MaxUnlockedTier { get; private set; } = ItemTier.Small;

    public event Action<ItemTier, ItemTier> TierChanged;
    public event Action<int, int> MassChanged;

    private void Awake()
    {
        if (_tierResolver == null)
        {
            throw new InvalidOperationException(
                $"{name}: TierResolver is not assigned. Drag a TierResolver component into the _tierResolver field.");
        }

        _mass = _defaultMass;
        MaxUnlockedTier = _tierResolver.GetUnlockedTier(_mass);
        TierChanged?.Invoke(MaxUnlockedTier, MaxUnlockedTier);
        MassChanged?.Invoke(_defaultMass, _mass);
    }

    public void Add(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount),
                "PlayerMass.Decrease requires amount to be non-negative. The provided value was negative.");
        }

        int previous = _mass;

        int scaledMass = Mathf.RoundToInt(amount / (float)_massPickupDivisor);
        scaledMass = Mathf.Max(1, scaledMass);

        _mass += scaledMass;
        MassChanged?.Invoke(previous, _mass);

        ItemTier previousTier = MaxUnlockedTier;
        MaxUnlockedTier = _tierResolver.GetUnlockedTier(_mass);

        TierChanged?.Invoke(previousTier, MaxUnlockedTier);
    }
    }
}