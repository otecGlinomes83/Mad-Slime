using Collectables;
using Game;
using Movement;
using PlayerInput;
using Scriptables;
using UnityEngine;
using VContainer;

namespace Player
{
    [RequireComponent(typeof(Mover))]
    [RequireComponent(typeof(Rotator))]
    [RequireComponent(typeof(PlayerTier))]
    public sealed class Player : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader _inputReader;
        [SerializeField] private Collector _collector;

        private Mover _mover;
        private Rotator _rotator;
        private PlayerTier _playerTier;
        private LevelProgress _levelProgress;
        private PlayerConfig _playerConfig;

        [Inject]
        public void Construct(LevelProgress levelProgress, PlayerConfig playerConfig)
        {
            _levelProgress = levelProgress;
            _playerConfig = playerConfig;
        }

        private void Awake()
        {
            _mover = GetComponent<Mover>();
            _rotator = GetComponent<Rotator>();
            _playerTier = GetComponent<PlayerTier>();
        }

        private void Start()
        {
            if (_playerConfig == null)
            {
                return;
            }

            _mover.SetDefaultSpeed(_playerConfig.BaseMoveSpeed);
            _mover.SetSmoothTime(_playerConfig.MoveSmoothTime);
            _rotator.SetSpeed(_playerConfig.RotationSpeed);
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

        private void OnItemCollected(Items.Item item)
        {
            _levelProgress.RegisterCollected(item.Definition);
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
