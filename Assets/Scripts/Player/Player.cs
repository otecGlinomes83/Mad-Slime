using System;
using Collectables;
using Health;
using Interfaces;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Mover))]
[RequireComponent(typeof(Rotator))]
[RequireComponent(typeof(PlayerMass))]
[RequireComponent(typeof(Health.Health))]
[RequireComponent(typeof(Healer))]
public sealed class Player : MonoBehaviour, ITarget
{
    [SerializeField] private PlayerInputReader _inputReader;
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private Collector _collector;
    [SerializeField] private Inventory _inventory;

    private Mover _mover;
    private Rotator _rotator;
    private PlayerMass _playerMass;
    private Health.Health _health;
    private Healer _healer;

    public Transform Transform => transform;
    public Health.Health Health => _health;

    private void Awake()
    {
        _mover = GetComponent<Mover>();
        _rotator = GetComponent<Rotator>();
        _playerMass = GetComponent<PlayerMass>();
        _health = GetComponent<Health.Health>();
        _healer = GetComponent<Healer>();
    }

    private void OnEnable()
    {
        _collector.ItemCollected += OnItemCollected;
    }

    private void OnDisable()
    {
        _collector.ItemCollected -= OnItemCollected;
    }

    private void Update()
    {
        Vector3 moveDirection = ConvertToWorldDirection(_inputReader.MoveInput);

        _mover.Move(moveDirection);
        _rotator.Rotate(moveDirection);
    }

    private void OnItemCollected(Item item)
    {
        _inventory.Add(item.Definition);
        _playerMass.Add(item.Mass);
    }

    private Vector3 ConvertToWorldDirection(Vector2 input)
    {
        if (_cameraTransform == null)
        {
            throw new InvalidOperationException(
                "Player.ConvertToWorldDirection requires _cameraTransform to be assigned. The camera transform is null.");
        }

        Vector3 forward = _cameraTransform.forward;
        Vector3 right = _cameraTransform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        return forward * input.y + right * input.x;
    }
}
