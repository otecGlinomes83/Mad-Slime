using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Mover))]
[RequireComponent(typeof(Rotator))]
public sealed class Player : MonoBehaviour
{
    [SerializeField] private PlayerInputReader _inputReader;
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private Collector _collector;

    private Mover _mover;
    private Rotator _rotator;
    private Vector3 _moveDirection;

    private int _mass;

    private void Awake()
    {
        _mover = GetComponent<Mover>();
        _rotator = GetComponent<Rotator>();
    }

    private void OnEnable()
    {
        _collector.OnCollect += IncreaseMass;
    }

    private void OnDisable()
    {
        _collector.OnCollect -= IncreaseMass;
    }

    private void Update()
    {
        _moveDirection = ConvertToWorldDirection(_inputReader.MoveInput);
    }

    private void FixedUpdate()
    {
        _mover.Move(_moveDirection);
        _rotator.Rotate(_moveDirection);
    }

    private void IncreaseMass(int mass)
    {
        _mass += mass;
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