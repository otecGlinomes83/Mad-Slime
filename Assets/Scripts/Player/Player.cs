using UnityEngine;

[RequireComponent(typeof(Mover))]
public sealed class Player : MonoBehaviour
{
    [SerializeField] private PlayerInputReader _inputReader;
    [SerializeField] private Transform _cameraTransform;

    private Mover _mover;

    private void Awake()
    {
        _mover = GetComponent<Mover>();
    }

    private void Update()
    {
        Vector3 direction = ConvertToWorldDirection(_inputReader.MoveInput);
        _mover.Move(direction);
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
