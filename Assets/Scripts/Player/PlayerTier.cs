using Assets.Scripts.HealthSystem;
using Scriptables;
using Skills;
using System;
using UnityEngine;

public class PlayerTier : MonoBehaviour
{
    [SerializeField] private int _defaultMass;
    [SerializeField] private Health _playerHealth;
    [SerializeField] private int _massPickupDivisor = 4;
    [SerializeField] private TierScalerConfig _tierConfig;

   private int _mass;
   
    public int Mass => _mass;
    public ItemTier MaxUnlockedTier { get; private set; } = ItemTier.Small;

    public event Action<int, int> Changed;

    private void Awake()
    {
        _mass = _defaultMass;
        MaxUnlockedTier = _tierConfig.GetUnlockedTier(_mass);
    }

    public void Setup(int mass)
    {
        if (mass < 0)
            throw new ArgumentOutOfRangeException(nameof(mass),
                "PlayerMass.Setup requires mass to be non-negative. The provided value was negative.");

        _mass = mass;
        MaxUnlockedTier = _tierConfig.GetUnlockedTier(_mass);
        Changed?.Invoke(_defaultMass, _mass);
    }

    public void Add(int amount)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount),
                "PlayerMass.Decrease requires amount to be non-negative. The provided value was negative.");

        int previous = _mass;

        int scaledMass = Mathf.RoundToInt(amount / (float)_massPickupDivisor);
        scaledMass = Mathf.Max(1, scaledMass);

        _mass += scaledMass;
        Changed?.Invoke(previous, _mass);
        MaxUnlockedTier = _tierConfig.GetUnlockedTier(_mass);
    }
}