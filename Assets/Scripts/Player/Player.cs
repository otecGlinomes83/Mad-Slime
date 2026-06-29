using Assets.Scripts.HealthSystem;
using Interfaces;
using PlayerInput;
using System;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Mover))]
[RequireComponent(typeof(Rotator))]
[RequireComponent(typeof(PlayerMass))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Healer))]
public sealed class Player : MonoBehaviour, ITarget
{
    [SerializeField] private PlayerInputReader _inputReader;
    [SerializeField] private QuotaTracker _quotaTracker;
    [SerializeField] private Collector _collector;
    [SerializeField] private Inventory _inventory;

    private Mover _mover;
    private Rotator _rotator;
    private PlayerMass _playerMass;
    private Health _health;
    private Healer _healer;

    public Transform Transform => transform;
    public Health Health => _health;

    private void Awake()
    {
        _mover = GetComponent<Mover>();
        _rotator = GetComponent<Rotator>();
        _playerMass = GetComponent<PlayerMass>();
        _health = GetComponent<Health>();
        _healer = GetComponent<Healer>();
    }

    private void OnEnable()
    {
        _collector.ItemCollected += OnItemCollected;
        _inputReader.SprintPerformed += OnSprintPerformed;
    }

    private void OnDisable()
    {
        _collector.ItemCollected -= OnItemCollected;
        _inputReader.SprintPerformed -= OnSprintPerformed;
    }

    private void Update()
    {
        Vector3 moveDirection = ConvertToWorldDirection(_inputReader.MoveInput);

        _mover.Move(moveDirection);
        _rotator.Rotate(moveDirection);
    }

    private void OnSprintPerformed()
    {
        _mover.TryStartSprint();
    }

    private void OnItemCollected(Item.Item item)
    {
        if (_quotaTracker.IsQuotaItem(item.Definition))
        {
            _inventory.IncreaseQuotaCount();
            _quotaTracker.RegisterCollected(item.Definition);
        }
        else
        {
            _inventory.IncreaseDefaultCount();
        }

        _playerMass.Add(item.Mass);
    }

    private Vector3 ConvertToWorldDirection(Vector2 input)
    {
        Vector3 forward = Vector3.forward;
        Vector3 right = Vector3.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        return forward * input.y + right * input.x;
    }
}