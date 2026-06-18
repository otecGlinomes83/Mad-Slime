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
        [SerializeField] private Color _ghostColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        [SerializeField] private float _spawnInterval = 0.04f;
        [SerializeField] private float _flightDuration = 0.5f;
        [SerializeField] private Vector3 _spawnPosition;
        [SerializeField] private Vector3 _spinSpeed = new Vector3(360f, 720f, 180f);
        [SerializeField] private Color _borderColor = Color.black;

        private MaterialPropertyBlock _propertyBlock;
        private readonly List<GameObject> _spawnedCubes = new List<GameObject>();
        private int _fillIndex;
        private bool _isFilling;

        public int RequiredFillCount => _gridShape.FillCells.Count;

        public event Action<float> ProgressUpdated;

        private void Awake()
        {
            if (_gridShape == null)
            {
                throw new InvalidOperationException(
                    $"{name}: GridShape is not assigned. Drag a GridShape component into the _gridShape field in the inspector.");
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

            _propertyBlock = new MaterialPropertyBlock();
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
            if (cubesCount <= 0)
            {
                return;
            }

            int target = Mathf.Clamp(cubesCount, 0, RequiredFillCount);

            StopFill();
            _fillIndex = 0;
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
            int totalCells = fillCells.Count;

            while (_fillIndex < target)
            {
                if (_isFilling == false)
                {
                    break;
                }

                cancellationToken.ThrowIfCancellationRequested();

                SpawnFillCube(fillCells[_fillIndex], totalCells);
                _fillIndex++;

                await UniTask.Delay((int)(_spawnInterval * 1000f), cancellationToken: cancellationToken);
            }

            _isFilling = false;
        }

        private void SpawnFillCube(Vector2Int cell, int totalCells)
        {
            FlyingCube fillCube = Instantiate(_cubePrefab, _spawnPosition, UnityEngine.Random.rotation, _cubesParent);
            fillCube.transform.localScale = Vector3.one * _gridShape.CellSize;

            Color pixelColor = _gridShape.GetPixelColor(cell.x, cell.y);
            SetCubeColor(fillCube.gameObject, pixelColor);

            fillCube.Arrived += OnCubeArrived;
            fillCube.Launch(_gridShape.GridToWorld(cell.x, cell.y), _flightDuration, _spinSpeed);

            _spawnedCubes.Add(fillCube.gameObject);

            ProgressUpdated?.Invoke((float)_fillIndex / totalCells);
        }

        private void OnCubeArrived(FlyingCube cube)
        {
            cube.Arrived -= OnCubeArrived;
        }
    }
}
