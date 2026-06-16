using System.Collections.Generic;
using UnityEngine;

namespace ShapeFill
{
    public sealed class GridShape : MonoBehaviour
    {
        private const int BorderThickness = 2;

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
            ReadPixels();
            ClassifyBorder();
            SortFillTopToBottom();
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

        private void ReadPixels()
        {
            Color[] pixels = _shapeTexture.GetPixels();
            int textureWidth = _shapeTexture.width;
            int textureHeight = _shapeTexture.height;

            int stepX = Mathf.Max(Mathf.CeilToInt((float)textureWidth / _gridResolution), 1);
            int stepY = Mathf.Max(Mathf.CeilToInt((float)textureHeight / _gridResolution), 1);

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
        }

        private void ClassifyBorder()
        {
            _borderCells.Clear();
            _fillCells.Clear();

            for (int gridX = 0; gridX < _gridWidth; gridX++)
            {
                for (int gridY = 0; gridY < _gridHeight; gridY++)
                {
                    if (_filledCells[gridX, gridY] == false)
                    {
                        continue;
                    }

                    if (IsBorderCell(gridX, gridY))
                    {
                        _borderCells.Add(new Vector2Int(gridX, gridY));
                    }
                    else
                    {
                        _fillCells.Add(new Vector2Int(gridX, gridY));
                    }
                }
            }
        }

        private bool IsBorderCell(int gridX, int gridY)
        {
            if (IsOnEdge(gridX, gridY) == true)
            {
                return true;
            }

            if (HasEmptyDirectNeighbor(gridX, gridY) == true)
            {
                return true;
            }

            return HasEmptyNeighborAtDistanceTwo(gridX, gridY);
        }

        private bool IsOnEdge(int gridX, int gridY)
        {
            return gridX < BorderThickness
                || gridX >= _gridWidth - BorderThickness
                || gridY < BorderThickness
                || gridY >= _gridHeight - BorderThickness;
        }

        private bool HasEmptyDirectNeighbor(int gridX, int gridY)
        {
            return _filledCells[gridX - 1, gridY] == false
                || _filledCells[gridX + 1, gridY] == false
                || _filledCells[gridX, gridY - 1] == false
                || _filledCells[gridX, gridY + 1] == false;
        }

        private bool HasEmptyNeighborAtDistanceTwo(int gridX, int gridY)
        {
            if (_filledCells[gridX - 1, gridY] == true && _filledCells[gridX - 2, gridY] == false) { return true; }
            if (_filledCells[gridX + 1, gridY] == true && _filledCells[gridX + 2, gridY] == false) { return true; }
            if (_filledCells[gridX, gridY - 1] == true && _filledCells[gridX, gridY - 2] == false) { return true; }
            if (_filledCells[gridX, gridY + 1] == true && _filledCells[gridX, gridY + 2] == false) { return true; }

            return false;
        }

        private void SortFillTopToBottom()
        {
            _fillCells.Sort(TopToBottomLeftToRightComparer.Instance);
        }

        private sealed class TopToBottomLeftToRightComparer : IComparer<Vector2Int>
        {
            public static readonly TopToBottomLeftToRightComparer Instance = new TopToBottomLeftToRightComparer();

            public int Compare(Vector2Int left, Vector2Int right)
            {
                if (left.y != right.y)
                {
                    return right.y.CompareTo(left.y);
                }

                return left.x.CompareTo(right.x);
            }
        }
    }
}
