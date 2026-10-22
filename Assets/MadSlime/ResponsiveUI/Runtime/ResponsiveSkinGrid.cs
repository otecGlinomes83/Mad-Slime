using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    [DisallowMultipleComponent, RequireComponent(typeof(GridLayoutGroup))]
    public class ResponsiveSkinGrid : MonoBehaviour
    {
        private const float WidthChangeThreshold = 0.5f;

        [SerializeField, Min(100f)] private float _preferredCellSize = 208f;
        [SerializeField, Min(1)] private int _maximumColumns = 5;

        private GridLayoutGroup _grid;
        private RectTransform _rect;
        private float _width = -1f;

        private void Awake()
        {
            if (TryGetComponent(out _grid) == false)
            {
                throw new System.InvalidOperationException($"{name}: GridLayoutGroup is required.");
            }

            _rect = (RectTransform)transform;
        }

        private void LateUpdate()
        {
            float width = _rect.rect.width;

            if (Mathf.Abs(width - _width) < WidthChangeThreshold || width <= 0f)
            {
                return;
            }

            _width = width;

            float available = width - _grid.padding.horizontal;
            int columns = Mathf.Clamp(Mathf.FloorToInt((available + _grid.spacing.x) /
                (_preferredCellSize + _grid.spacing.x)), 1, _maximumColumns);
            float size = (available - (columns - 1) * _grid.spacing.x) / columns;

            _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _grid.constraintCount = columns;
            _grid.cellSize = new Vector2(size, size);
        }
    }
}
