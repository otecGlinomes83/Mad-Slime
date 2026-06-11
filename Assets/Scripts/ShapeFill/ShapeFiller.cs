using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class ShapeFiller : MonoBehaviour
{
    [SerializeField] private GridShape _gridShape;
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
    private readonly List<GameObject> _cubes = new List<GameObject>();
    private int _fillIndex;
    private bool _isFilling;

    public event Action<float> ProgressUpdated;

    private void Awake()
    {
        _propertyBlock = new MaterialPropertyBlock();
    }

    private void Start()
    {
        BuildShape();
        StartFill();
    }

    public void BuildShape()
    {
        Clear();
        _gridShape.Build();
        SetupGhost();
        BuildBorder();
        _fillIndex = 0;
    }

    public void StartFill()
    {
        if (_isFilling)
        {
            return;
        }

        CancellationToken cancellationToken = this.GetCancellationTokenOnDestroy();
        FillAsync(cancellationToken).Forget();
    }

    private void Clear()
    {
        _isFilling = false;

        for (int i = 0; i < _cubes.Count; i++)
        {
            Destroy(_cubes[i]);
        }

        _cubes.Clear();
    }

    private void SetupGhost()
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

    private void BuildBorder()
    {
        IReadOnlyList<Vector2Int> borderCells = _gridShape.BorderCells;

        for (int i = 0; i < borderCells.Count; i++)
        {
            Vector2Int cell = borderCells[i];
            Vector3 worldPosition = _gridShape.GridToWorld(cell.x, cell.y);

            FlyingCube borderCube = Instantiate(_cubePrefab, worldPosition, Quaternion.identity, _cubesParent);
            borderCube.transform.localScale = Vector3.one * _gridShape.CellSize;

            SetCubeColor(borderCube.gameObject, _borderColor);
            _cubes.Add(borderCube.gameObject);
        }
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

    private async UniTaskVoid FillAsync(CancellationToken cancellationToken)
    {
        _isFilling = true;

        IReadOnlyList<Vector2Int> fillCells = _gridShape.FillCells;
        int totalCells = fillCells.Count;

        while (_fillIndex < totalCells)
        {
            if (_isFilling == false)
            {
                break;
            }

            cancellationToken.ThrowIfCancellationRequested();

            Vector2Int cell = fillCells[_fillIndex];

            FlyingCube fillCube = Instantiate(_cubePrefab, _spawnPosition, UnityEngine.Random.rotation, _cubesParent);
            fillCube.transform.localScale = Vector3.one * _gridShape.CellSize;

            Color pixelColor = _gridShape.GetPixelColor(cell.x, cell.y);

            if (pixelColor.a > 0.1f)
            {
                SetCubeColor(fillCube.gameObject, pixelColor);
            }
            else
            {
                SetCubeColor(fillCube.gameObject, Color.white);
            }

            Vector3 targetPosition = _gridShape.GridToWorld(cell.x, cell.y);

            fillCube.Arrived += OnCubeArrived;
            fillCube.Launch(targetPosition, _flightDuration, _spinSpeed);

            _cubes.Add(fillCube.gameObject);
            _fillIndex++;

            ProgressUpdated?.Invoke((float)_fillIndex / totalCells);

            await UniTask.Delay((int)(_spawnInterval * 1000f), cancellationToken: cancellationToken);
        }

        _isFilling = false;
    }

    private void OnCubeArrived(FlyingCube cube)
    {
        cube.Arrived -= OnCubeArrived;
    }
}
