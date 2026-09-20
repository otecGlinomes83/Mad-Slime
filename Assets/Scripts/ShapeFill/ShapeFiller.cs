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
        private enum BoostStage
        {
            Normal,
            Boosted,
            Instant
        }

        private const float NominalFrameDuration = 1f / 60f;

        [SerializeField] private GridBuilder _gridShape;

        [SerializeField] private SpriteRenderer _ghostBackground;

        [SerializeField] private CubeSpawner _spawner;

        [SerializeField, Range(0f, 1f)] private float _ghostOpacity = 0.4f;

        [SerializeField] private Vector3 _spawnPosition;

        [SerializeField] private Color _borderColor = Color.black;

        [SerializeField] private float _spawnInterval = 0.04f;

        [SerializeField] private float _flightDuration = 0.5f;

        [Tooltip("Интервал спавна кубов после первого тапа (с). Меньше = кубы вылетают чаще")]
        [SerializeField, Min(0.001f)] private float _boostedSpawnInterval = 0.015f;

        [Tooltip("Время полёта куба до ячейки после первого тапа (с). Меньше = резче долетает")]
        [SerializeField, Min(0.01f)] private float _boostedFlightDuration = 0.25f;

        [Tooltip("Общее окно вылета всех оставшихся кубов после второго тапа (с). «Моментально» условно: 0.25 = короткий залп")]
        [SerializeField, Min(0.05f)] private float _instantFillDuration = 0.25f;

        [Tooltip("Время полёта каждого куба при досыпании (с). Финал наступит после долёта последнего куба")]
        [SerializeField, Min(0.01f)] private float _instantFlightDuration = 0.1f;

        [Tooltip("Пауза после долёта квотной волны перед бонусной (с). 0 = без паузы")]
        [SerializeField, Min(0f)] private float _bonusWaveDelay = 0.35f;

        [Tooltip("Цвет, в который подмешиваются бонусные кубы (заливка сверх квоты)")]
        [SerializeField] private Color _bonusTintColor = Color.black;

        [Tooltip("Сила подмешивания цвета бонусных кубов. 0 = не отличаются от квотных, 1 = полностью цвета оттенка")]
        [SerializeField, Range(0f, 1f)] private float _bonusTintStrength = 0.3f;

        [SerializeField, Min(0.05f)] private float _borderCascadeDuration = 0.5f;

        [SerializeField, Min(0f)] private float _fillDelay = 0.55f;

        private int _fillIndex;
        private int _arrivedCount;
        private int _currentTarget;
        private int _quotaTarget;
        private bool _isFilling;
        private BoostStage _boostStage;
        private Sprite _ghostSprite;
        private CancellationTokenSource _fillCts;
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
            CancelFillLoop();
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

        public void Fill(int quotaCubesCount, int bonusCubesCount)
        {
            int quota = Mathf.Clamp(quotaCubesCount, 0, RequiredFillCount);
            int target = Mathf.Clamp(quota + Mathf.Max(0, bonusCubesCount), quota, RequiredFillCount);

            if (target <= 0)
            {
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

            _fillCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());

            FillAsync(_fillCts.Token, target).Forget();
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
                    cancellationToken.ThrowIfCancellationRequested();

                    if (IsBonusWaveStart(target) == true)
                    {
                        await UniTask.Delay((int)(_bonusWaveDelay * 1000f), cancellationToken: cancellationToken);
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
                return _boostedSpawnInterval;
            }

            return _spawnInterval;
        }

        private int GetSpawnDelayMilliseconds()
        {
            return Mathf.CeilToInt(GetSpawnInterval() * 1000f);
        }

        private float GetFlightDuration()
        {
            if (_boostStage == BoostStage.Boosted)
            {
                return _boostedFlightDuration;
            }

            return _flightDuration;
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
                color = Color.Lerp(color, _bonusTintColor, _bonusTintStrength);
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
                        SpawnFillCube(fillCells[_fillIndex], _instantFlightDuration);
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

            int framesInWindow = Mathf.CeilToInt(_instantFillDuration / frameDuration);

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
