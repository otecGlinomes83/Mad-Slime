using Collectables;
using HealthSystem;
using Interfaces;
using Movement;
using PlayerInput;
using Quota;
using Skills;
using UnityEngine;

namespace Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Mover))]
    [RequireComponent(typeof(Rotator))]
    [RequireComponent(typeof(PlayerTier))]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(Healer))]
    public sealed class Player : MonoBehaviour, ITarget
    {
    [SerializeField] private PlayerInputReader _inputReader;
    [SerializeField] private QuotaTracker _quotaTracker;
    [SerializeField] private Collector _collector;
    [SerializeField] private SprintSkill _sprintSkill;

    private Mover _mover;
    private Rotator _rotator;
    private PlayerTier _playerTier;
    private Health _health;
    private Healer _healer;

    public Transform Transform => transform;
    public Health Health => _health;

    public ItemTier Tier => _playerTier.MaxUnlockedTier;

    private void Awake()
    {
        _mover = GetComponent<Mover>();
        _rotator = GetComponent<Rotator>();
        _playerTier = GetComponent<PlayerTier>();
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
        _sprintSkill.Activate();
    }

    private void OnItemCollected(Item.Item item)
    {
        _quotaTracker.RegisterCollected(item.Definition);
        _playerTier.Add(item.Mass);
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
}