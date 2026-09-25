using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Scriptables;
using UnityEngine;
using VContainer;

namespace ShapeFill
{
    [RequireComponent(typeof(CubeSpawner))]
    public sealed class ShapeFiller : MonoBehaviour
    {
        private enum BoostStage
        {
            Normal,
            Boosted,
            Instant
        }

        private const float NominalFrameDuration = 1f / 60f;

        [SerializeField] private SpriteRenderer _ghostBackground;

        [SerializeField] private Vector3 _spawnPosition;

        private int _fillIndex;
        private int _arrivedCount;
        private int _currentTarget;
        private int _quotaTarget;
        private bool _isFilling;
        private bool _hasResult;
        private BoostStage _boostStage;
        private Sprite _ghostSprite;
        private FillConfig _config;
        private CancellationTokenSource _fillCts;
        private CancellationTokenSource _borderCts;
        private GridBuilder _gridShape;
        private CubeSpawner _spawner;

        public int RequiredFillCount => _gridShape.FillCells.Count;

        public bool CanRescue => _hasResult == true && _isFilling == false && _currentTarget < RequiredFillCount;

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

        [Inject]
        public void Construct(GridBuilder gridShape, CubeSpawner spawner, FillConfig config)
        {
            _gridShape = gridShape;
            _spawner = spawner;
            _config = config;
        }

        private void Awake()
        {
            if (_gridShape == null)
            {
                throw new InvalidOperationException(
                    $"{name}: GridBuilder was not injected. Check that FillLifetimeScope registers GridBuilder and ShapeFiller.");
            }

            if (_spawner == null)
            {
                throw new InvalidOperationException(
                    $"{name}: CubeSpawner was not injected. Check that FillLifetimeScope registers CubeSpawner and ShapeFiller.");
            }

            if (_config == null)
            {
                throw new InvalidOperationException(
                    $"{name}: FillConfig was not injected. Check that FillLifetimeScope has the FillConfig assigned.");
            }
        }

        private void OnDisable()
        {
            CancelFillLoop();
            CancelBorderCascade();
        }

        private void OnDestroy()
        {
            if (_ghostSprite != null)
            {
                Destroy(_ghostSprite);
                _ghostSprite = null;
            }
        }

        public void Initialize()
        {
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

        public void Fill(int quotaCubesCount, int bonusCubesCount)
        {
            int quota = Mathf.Clamp(quotaCubesCount, 0, RequiredFillCount);
            int target = Mathf.Clamp(quota + Mathf.Max(0, bonusCubesCount), quota, RequiredFillCount);

            if (target <= 0)
            {
                _hasResult = true;
                FillCompleted?.Invoke(0f);
                return;
            }

            StopFill();
            _fillIndex = 0;
            _arrivedCount = 0;
            _quotaTarget = quota;
            _currentTarget = target;
            _boostStage = BoostStage.Normal;
            _isFilling = true;
            _hasResult = false;

            _fillCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());

            FillAsync(_fillCts.Token, target).Forget();
        }

        public void Rescue()
        {
            if (CanRescue == false)
            {
                throw new InvalidOperationException(
                    $"{name}: Rescue requires a finished fill below 100%. Check CanRescue before calling.");
            }

            _currentTarget = RequiredFillCount;
            _boostStage = BoostStage.Normal;
            _isFilling = true;

            CancelFillLoop();
            _fillCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());

            FillAsync(_fillCts.Token, _currentTarget).Forget();
        }

        public void StopFill()
        {
            _isFilling = false;
            CancelFillLoop();
        }

        public void Accelerate()
        {
            if (_isFilling == false)
            {
                return;
            }

            if (_boostStage == BoostStage.Normal)
            {
                _boostStage = BoostStage.Boosted;
                return;
            }

            if (_boostStage == BoostStage.Boosted)
            {
                StartInstantCompletion();
            }
        }

        private void StartInstantCompletion()
        {
            _boostStage = BoostStage.Instant;
            CancelFillLoop();

            CompleteInstantlyAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private void CancelFillLoop()
        {
            if (_fillCts == null)
            {
                return;
            }

            _fillCts.Cancel();
            _fillCts.Dispose();
            _fillCts = null;
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
            _ghostBackground.color = new Color(1f, 1f, 1f, _config.GhostOpacity);
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
            float perCubeDelay = _config.BorderCascadeDuration / borderCells.Count;
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
                _config.BorderColor
            );

            borderCube.GrowIn();
        }

        private async UniTaskVoid FillAsync(CancellationToken cancellationToken, int target)
        {
            IReadOnlyList<Vector2Int> fillCells = _gridShape.FillCells;

            try
            {
                if (_config.FillDelay > 0f)
                {
                    await UniTask.Delay((int)(_config.FillDelay * 1000f), cancellationToken: cancellationToken);
                }

                while (_fillIndex < target)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (IsBonusWaveStart(target) == true)
                    {
                        await UniTask.Delay((int)(_config.BonusWaveDelay * 1000f), cancellationToken: cancellationToken);
                    }

                    SpawnFillCube(fillCells[_fillIndex], GetFlightDuration());
                    _fillIndex++;

                    await UniTask.Delay(GetSpawnDelayMilliseconds(), cancellationToken: cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }

            _isFilling = false;
        }

        private float GetSpawnInterval()
        {
            if (_boostStage == BoostStage.Boosted)
            {
                return _config.BoostedSpawnInterval;
            }

            return _config.SpawnInterval;
        }

        private int GetSpawnDelayMilliseconds()
        {
            return Mathf.CeilToInt(GetSpawnInterval() * 1000f);
        }

        private float GetFlightDuration()
        {
            if (_boostStage == BoostStage.Boosted)
            {
                return _config.BoostedFlightDuration;
            }

            return _config.FlightDuration;
        }

        private bool IsBonusWaveStart(int target)
        {
            return _fillIndex == _quotaTarget && _quotaTarget > 0 && _quotaTarget < target;
        }

        private Color GetCubeColor(Vector2Int cell)
        {
            Color color = _gridShape.GetPixelColor(cell.x, cell.y);

            if (_fillIndex >= _quotaTarget)
            {
                color = Color.Lerp(color, _config.BonusTintColor, _config.BonusTintStrength);
            }

            return color;
        }

        private async UniTaskVoid CompleteInstantlyAsync(CancellationToken cancellationToken)
        {
            IReadOnlyList<Vector2Int> fillCells = _gridShape.FillCells;

            if (_fillIndex >= _currentTarget)
            {
                _isFilling = false;
                return;
            }

            try
            {
                while (_fillIndex < _currentTarget)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    int cubesPerFrame = GetInstantCubesPerFrame(_currentTarget - _fillIndex);

                    for (int i = 0; i < cubesPerFrame && _fillIndex < _currentTarget; i++)
                    {
                        SpawnFillCube(fillCells[_fillIndex], _config.InstantFlightDuration);
                        _fillIndex++;
                    }

                    if (_fillIndex < _currentTarget)
                    {
                        await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }

            _isFilling = false;
        }

        private int GetInstantCubesPerFrame(int remaining)
        {
            float frameDuration = Time.deltaTime;

            if (frameDuration <= 0f)
            {
                frameDuration = NominalFrameDuration;
            }

            int framesInWindow = Mathf.CeilToInt(_config.InstantFillDuration / frameDuration);

            if (framesInWindow <= 1)
            {
                return remaining;
            }

            return Mathf.CeilToInt((float)remaining / framesInWindow);
        }

        private void SpawnFillCube(Vector2Int cell, float flightDuration)
        {
            FlyingCube fillCube = _spawner.Spawn(
                _spawnPosition,
                UnityEngine.Random.rotation,
                _gridShape.CellSize,
                GetCubeColor(cell)
            );

            fillCube.Arrived += OnCubeArrived;

            fillCube.Launch(_gridShape.GridToWorld(cell.x, cell.y), flightDuration);
        }

        private void OnCubeArrived(FlyingCube cube)
        {
            cube.Arrived -= OnCubeArrived;
            _arrivedCount++;

            CubeArrived?.Invoke(cube);

            if (_arrivedCount >= _currentTarget)
            {
                _isFilling = false;
                _hasResult = true;

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
