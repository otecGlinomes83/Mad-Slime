using Collectables;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Mover))]
[RequireComponent(typeof(Rotator))]
public sealed class Player : MonoBehaviour
{
    [SerializeField] private PlayerInputReader _inputReader;
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private Collector _collector;
    [SerializeField] private Inventory _inventory;

    private Mover _mover;
    private Rotator _rotator;

    private int _mass;

    public int Mass => _mass;

    private void Awake()
    {
        _mover = GetComponent<Mover>();
        _rotator = GetComponent<Rotator>();
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
        _mass += item.Mass;
        _inventory.Add(item.Definition);
    }

    private Vector3 ConvertToWorldDirection(Vector2 input)
    {
        if (_cameraTransform == null)
        {
            return new Vector3(input.x, 0f, input.y);
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