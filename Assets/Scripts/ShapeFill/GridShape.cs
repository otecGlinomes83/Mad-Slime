using System.Collections.Generic;
using UnityEngine;

public sealed class GridShape : MonoBehaviour
{
    [SerializeField] private Texture2D _shapeTexture;
    [SerializeField] private float _cellSize = 0.5f;
    [SerializeField] private int _gridResolution = 64;

    private int _gridWidth;
    private int _gridHeight;
    private bool[,] _filledCells;
    private Color[,] _pixelColors;
    private readonly List<Vector2Int> _borderCells = new List<Vector2Int>();
    private readonly List<Vector2Int> _fillCells = new List<Vector2Int>();

    public IReadOnlyList<Vector2Int> BorderCells => _borderCells;
    public IReadOnlyList<Vector2Int> FillCells => _fillCells;
    public int Width => _gridWidth;
    public int Height => _gridHeight;
    public Texture2D ShapeTexture => _shapeTexture;
    public float CellSize => _cellSize;

    public void Build()
    {
        Color[] pixels = _shapeTexture.GetPixels();
        int textureWidth = _shapeTexture.width;
        int textureHeight = _shapeTexture.height;

        int stepX = Mathf.CeilToInt((float)textureWidth / _gridResolution);
        int stepY = Mathf.CeilToInt((float)textureHeight / _gridResolution);
        stepX = Mathf.Max(stepX, 1);
        stepY = Mathf.Max(stepY, 1);

        _gridWidth = Mathf.CeilToInt((float)textureWidth / stepX);
        _gridHeight = Mathf.CeilToInt((float)textureHeight / stepY);

        _filledCells = new bool[_gridWidth, _gridHeight];
        _pixelColors = new Color[_gridWidth, _gridHeight];

        for (int gridX = 0; gridX < _gridWidth; gridX++)
        {
            for (int gridY = 0; gridY < _gridHeight; gridY++)
            {
                int pixelX = gridX * stepX;
                int pixelY = gridY * stepY;
                Color pixelColor = pixels[pixelX + pixelY * textureWidth];
                _filledCells[gridX, gridY] = pixelColor.a > 0.1f;
                _pixelColors[gridX, gridY] = pixelColor;
            }
        }

        for (int gridX = 0; gridX < _gridWidth; gridX++)
        {
            for (int gridY = 0; gridY < _gridHeight; gridY++)
            {
                if (_filledCells[gridX, gridY] == false)
                {
                    continue;
                }

                if (IsBorder(gridX, gridY))
                {
                    _borderCells.Add(new Vector2Int(gridX, gridY));
                }
                else
                {
                    _fillCells.Add(new Vector2Int(gridX, gridY));
                }
            }
        }

        System.Random random = new System.Random();

        for (int i = _fillCells.Count - 1; i > 0; i--)
        {
            int swapIndex = random.Next(i + 1);
            Vector2Int temp = _fillCells[i];
            _fillCells[i] = _fillCells[swapIndex];
            _fillCells[swapIndex] = temp;
        }
    }

    public Vector3 GridToWorld(int gridX, int gridY)
    {
        float localX = (gridX - (_gridWidth - 1) * 0.5f) * _cellSize;
        float localY = (gridY - (_gridHeight - 1) * 0.5f) * _cellSize;

        return transform.TransformPoint(new Vector3(localX, localY, 0f));
    }

    public Color GetPixelColor(int gridX, int gridY)
    {
        if (gridX < 0 || gridX >= _gridWidth || gridY < 0 || gridY >= _gridHeight)
        {
            return Color.white;
        }

        return _pixelColors[gridX, gridY];
    }

    private bool IsBorder(int gridX, int gridY)
    {
        if (gridX == 0 || gridX == _gridWidth - 1 || gridY == 0 || gridY == _gridHeight - 1)
        {
            return true;
        }

        return _filledCells[gridX - 1, gridY] == false ||
               _filledCells[gridX + 1, gridY] == false ||
               _filledCells[gridX, gridY - 1] == false ||
               _filledCells[gridX, gridY + 1] == false;
    }
}
