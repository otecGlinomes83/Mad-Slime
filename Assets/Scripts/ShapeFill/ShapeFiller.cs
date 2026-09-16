using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ShapeFill
{
    [RequireComponent(typeof(CubeSpawner))]
    public sealed class ShapeFiller : MonoBehaviour
    {
        [SerializeField] private GridBuilder _gridShape;
        [SerializeField] private SpriteRenderer _ghostBackground;
        [SerializeField] private CubeSpawner _spawner;

        [SerializeField, Range(0f, 1f)] private float _ghostOpacity = 0.4f;
        [SerializeField] private Vector3 _spawnPosition;
        [SerializeField] private Color _borderColor = Color.black;

        [SerializeField] private float _spawnInterval = 0.04f;
        [SerializeField] private float _flightDuration = 0.5f;
        [SerializeField, Min(0.05f)] private float _borderCascadeDuration = 0.5f;
        [SerializeField, Min(0f)] private float _fillDelay = 0.55f;

        private int _fillIndex;
        private int _arrivedCount;
        private int _currentTarget;
        private bool _isFilling;
        private Sprite _ghostSprite;
        private CancellationTokenSource _borderCts;

        public int RequiredFillCount => _gridShape.FillCells.Count;

        public float FillFraction
        {
            get
            {
                if (_currentTarget <= 0)
                {
                    return 0f;
                }

                return Mathf.Clamp01(_arrivedCount / (float)_currentTarget);
            }
        }

        public event Action<float> FillCompleted;

        public event Action<FlyingCube> CubeArrived;

        private void Awake()
        {
            _spawner = GetComponent<CubeSpawner>();
        }

        private void OnDisable()
        {
            CancelBorderCascade();
        }

        public void Initialize()
        {
            if (_gridShape == null)
            {
                throw new InvalidOperationException(
                    $"{name}: GridShape is not assigned. Drag a GridShape component into the _gridShape field.");
            }

            if (_spawner == null)
            {
                throw new InvalidOperationException(
                    $"{name}: CubeSpawner is not assigned.");
            }

            _spawner.Initialize();
        }

        public void BuildShape()
        {
            StopFill();

            _gridShape.Build();
            PlaceGhost();
            SpawnBorder();
            _fillIndex = 0;
        }

        public void Fill(int cubesCount)
        {
            int target = Mathf.Clamp(cubesCount, 0, RequiredFillCount);

            if (target <= 0)
            {
                FillCompleted?.Invoke(0f);
                return;
            }

            StopFill();
            _fillIndex = 0;
            _arrivedCount = 0;
            _currentTarget = target;
            _isFilling = true;
            FillAsync(this.GetCancellationTokenOnDestroy(), target).Forget();
        }

        public void StopFill()
        {
            _isFilling = false;
        }

        private void PlaceGhost()
        {
            if (_ghostBackground == null)
            {
                return;
            }

            Texture2D texture = _gridShape.ShapeTexture;

            if (_ghostSprite != null)
            {
                Destroy(_ghostSprite);
            }

            _ghostSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                Vector2.one * 0.5f,
                texture.width / (_gridShape.Width * _gridShape.CellSize)
            );

            _ghostBackground.sprite = _ghostSprite;
            _ghostBackground.color = new Color(1f, 1f, 1f, _ghostOpacity);
            _ghostBackground.transform.SetPositionAndRotation(
                _gridShape.transform.position,
                _gridShape.transform.rotation
            );
            _ghostBackground.transform.localScale = Vector3.one;
        }

        private void SpawnBorder()
        {
            CancelBorderCascade();

            IReadOnlyList<Vector2Int> borderCells = _gridShape.BorderCells;

            if (borderCells.Count == 0)
            {
                return;
            }

            _borderCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());

            SpawnBorderCascadeAsync(borderCells, _borderCts.Token).Forget();
        }

        private async UniTaskVoid SpawnBorderCascadeAsync(
            IReadOnlyList<Vector2Int> borderCells,
            CancellationToken cancellationToken)
        {
            float perCubeDelay = _borderCascadeDuration / borderCells.Count;
            int delayMilliseconds = Mathf.CeilToInt(perCubeDelay * 1000f);

            try
            {
                for (int i = 0; i < borderCells.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    SpawnBorderCube(borderCells[i]);

                    if (delayMilliseconds > 0)
                    {
                        await UniTask.Delay(delayMilliseconds, cancellationToken: cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        private void SpawnBorderCube(Vector2Int cell)
        {
            FlyingCube borderCube = _spawner.Spawn(
                _gridShape.GridToWorld(cell.x, cell.y),
                Quaternion.identity,
                _gridShape.CellSize,
                _borderColor
            );

            borderCube.GrowIn();
        }

        private async UniTaskVoid FillAsync(CancellationToken cancellationToken, int target)
        {
            IReadOnlyList<Vector2Int> fillCells = _gridShape.FillCells;

            try
            {
                if (_fillDelay > 0f)
                {
                    await UniTask.Delay((int)(_fillDelay * 1000f), cancellationToken: cancellationToken);
                }

                while (_fillIndex < target)
                {
                    if (_isFilling == false)
                    {
                        break;
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    SpawnFillCube(fillCells[_fillIndex], target);
                    _fillIndex++;

                    await UniTask.Delay((int)(_spawnInterval * 1000f), cancellationToken: cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }

            _isFilling = false;
        }

        private void SpawnFillCube(Vector2Int cell, int target)
        {
            FlyingCube fillCube = _spawner.Spawn(
                _spawnPosition,
                UnityEngine.Random.rotation,
                _gridShape.CellSize,
                _gridShape.GetPixelColor(cell.x, cell.y)
            );

            fillCube.Arrived += OnCubeArrived;

            fillCube.Launch(_gridShape.GridToWorld(cell.x, cell.y), _flightDuration);
        }

        private void OnCubeArrived(FlyingCube cube)
        {
            cube.Arrived -= OnCubeArrived;
            _arrivedCount++;

            CubeArrived?.Invoke(cube);

            if (_arrivedCount >= _currentTarget)
            {
                FillCompleted?.Invoke((float)_currentTarget / RequiredFillCount);
            }
        }

        private void CancelBorderCascade()
        {
            if (_borderCts == null)
            {
                return;
            }

            _borderCts.Cancel();
            _borderCts.Dispose();
            _borderCts = null;
        }
    }
}
