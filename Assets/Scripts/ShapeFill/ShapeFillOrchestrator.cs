using System;
using UnityEngine;

namespace ShapeFill
{
    [RequireComponent(typeof(GridShape))]
    [RequireComponent(typeof(ShapeFiller))]
    public sealed class ShapeFillOrchestrator : MonoBehaviour
    {
        private GridShape _gridShape;
        private ShapeFiller _shapeFiller;

        public int RequiredFillCount => _gridShape.FillCells.Count;

        public event Action<float> ProgressUpdated;

        private void Awake()
        {
            _gridShape = GetComponent<GridShape>();
            _shapeFiller = GetComponent<ShapeFiller>();

            if (_gridShape == null)
            {
                throw new InvalidOperationException(
                    $"{name}: GridShape is required but not found on the same GameObject.");
            }

            if (_shapeFiller == null)
            {
                throw new InvalidOperationException(
                    $"{name}: ShapeFiller is required but not found on the same GameObject.");
            }

            Build();
        }

        private void OnEnable()
        {
            _shapeFiller.ProgressUpdated += OnFillerProgressUpdated;
        }

        private void OnDisable()
        {
            _shapeFiller.ProgressUpdated -= OnFillerProgressUpdated;
        }

        public void Build()
        {
            _gridShape.Build();
            _shapeFiller.BuildShape();
        }

        public void StartFilling(int cubesCount)
        {
            _shapeFiller.Fill(cubesCount);
        }

        private void OnFillerProgressUpdated(float progress)
        {
            ProgressUpdated?.Invoke(progress);
        }
    }
}
