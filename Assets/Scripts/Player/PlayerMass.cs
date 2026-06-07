using System;
using Health;
using UnityEngine;

public class PlayerMass : MonoBehaviour, IMassHolder
{
    [SerializeField] private int _mass;
    [SerializeField] private int _defaultMass;
    [SerializeField] private Health.Health _playerHealth;
    [SerializeField] private int _damageAmount = 5;

    public int Mass => _mass;

    public event Action<int, int> Changed;

    private void Awake()
    {
        _mass = _defaultMass;
    }

    private void OnEnable()
    {
        _playerHealth.Damaged += Decrease;
    }

    private void OnDisable()
    {
        _playerHealth.Damaged -= Decrease;
    }

    public void ResetMass()
    {
        _mass = _defaultMass;
        Changed?.Invoke(_mass, _defaultMass);
    }

    public void Setup(int mass)
    {
        if (mass < 0)
            throw new ArgumentOutOfRangeException(nameof(mass),
                "PlayerMass.Setup requires mass to be non-negative. The provided value was negative.");

        _mass = mass;
        Changed?.Invoke(_defaultMass, _mass);
    }

    public void Add(int amount)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount),
                "PlayerMass.Add requires amount to be non-negative. The provided value was negative.");

        int previous = _mass;
        _mass += amount;
        Changed?.Invoke(previous, _mass);
    }

    public void Decrease()
    {
        if (_damageAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(_damageAmount),
                "PlayerMass.Decrease requires _damageAmount to be non-negative. The value is negative.");
        }

        int previous = _mass;
        _mass = Mathf.Max(_mass - _damageAmount, _defaultMass);
        Changed?.Invoke(previous, _mass);
    }
}