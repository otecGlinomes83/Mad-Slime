using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ShapeFill
{
    public sealed class ShapeFiller : MonoBehaviour
    {
        [SerializeField] private GridBuilder _gridShape;
        [SerializeField] private FlyingCube _cubePrefab;
        [SerializeField] private Transform _cubesParent;
        [SerializeField] private SpriteRenderer _ghostBackground;

        [SerializeField] private Color _ghostColor = new Color(0.3f, 0.3f, 0.3f, 0.75f);
        [SerializeField] private Vector3 _spawnPosition;
        [SerializeField] private Color _borderColor = Color.black;

        [SerializeField] private float _spawnInterval = 0.04f;
        [SerializeField] private float _flightDuration = 0.5f;

        private readonly List<GameObject> _spawnedCubes = new List<GameObject>();

        private MaterialPropertyBlock _propertyBlock;

        private int _fillIndex;
        private int _arrivedCount;
        private int _currentTarget;
        private bool _isFilling;

        public int RequiredFillCount => _gridShape.FillCells.Count;

        public event Action<float> FillCompleted;

        public event Action<FlyingCube> CubeArrived;

        public void Initialize()
        {
            if (_gridShape == null)
            {
                throw new InvalidOperationException(
                    $"{name}: GridShape is not assigned. Drag a GridShape component into the _gridBuilder field in the inspector.");
            }

            if (_cubePrefab == null)
            {
                throw new InvalidOperationException(
                    $"{name}: FlyingCube prefab is not assigned. Drag a FlyingCube prefab into the _cubePrefab field in the inspector.");
            }

            if (_cubesParent == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Cubes parent is not assigned. Drag a Transform into the _cubesParent field in the inspector.");
            }

            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }
        }

        public void BuildShape()
        {
            StopFill();
            ClearCubes();

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

        private void ClearCubes()
        {
            for (int i = 0; i < _spawnedCubes.Count; i++)
            {
                Destroy(_spawnedCubes[i]);
            }

            _spawnedCubes.Clear();
        }

        private void PlaceGhost()
        {
            if (_ghostBackground == null)
            {
                return;
            }

            Texture2D texture = _gridShape.ShapeTexture;

            _ghostBackground.sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                Vector2.one * 0.5f,
                texture.width / (_gridShape.Width * _gridShape.CellSize)
            );

            _ghostBackground.color = _ghostColor;
            _ghostBackground.transform.SetPositionAndRotation(
                _gridShape.transform.position,
                _gridShape.transform.rotation
            );
            _ghostBackground.transform.localScale = Vector3.one;
        }

        private void SpawnBorder()
        {
            IReadOnlyList<Vector2Int> borderCells = _gridShape.BorderCells;

            for (int i = 0; i < borderCells.Count; i++)
            {
                Vector2Int cell = borderCells[i];
                FlyingCube borderCube = SpawnStationaryCube(_gridShape.GridToWorld(cell.x, cell.y));

                SetCubeColor(borderCube.gameObject, _borderColor);
                _spawnedCubes.Add(borderCube.gameObject);
            }
        }

        private FlyingCube SpawnStationaryCube(Vector3 worldPosition)
        {
            FlyingCube cube = Instantiate(_cubePrefab, worldPosition, Quaternion.identity, _cubesParent);
            cube.transform.localScale = Vector3.one * _gridShape.CellSize;
            return cube;
        }

        private void SetCubeColor(GameObject cube, Color color)
        {
            if (cube.TryGetComponent(out Renderer cubeRenderer) == false)
            {
                return;
            }

            _propertyBlock.SetColor(Shader.PropertyToID("_Color"), color);
            cubeRenderer.SetPropertyBlock(_propertyBlock);
        }

        private async UniTaskVoid FillAsync(CancellationToken cancellationToken, int target)
        {
            IReadOnlyList<Vector2Int> fillCells = _gridShape.FillCells;

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

            _isFilling = false;
        }

        private void SpawnFillCube(Vector2Int cell, int target)
        {
            FlyingCube fillCube = Instantiate(_cubePrefab, _spawnPosition, UnityEngine.Random.rotation, _cubesParent);
            fillCube.transform.localScale = Vector3.one * _gridShape.CellSize;

            Color pixelColor = _gridShape.GetPixelColor(cell.x, cell.y);
            SetCubeColor(fillCube.gameObject, pixelColor);

            fillCube.Arrived += OnCubeArrived;

            fillCube.Launch(_gridShape.GridToWorld(cell.x, cell.y), _flightDuration);

            _spawnedCubes.Add(fillCube.gameObject);
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
    }
}
