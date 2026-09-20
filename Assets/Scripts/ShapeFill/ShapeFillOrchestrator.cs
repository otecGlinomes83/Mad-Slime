using System;
using UnityEngine;

namespace ShapeFill
{
    [RequireComponent(typeof(GridBuilder))]
    [RequireComponent(typeof(ShapeFiller))]
    [RequireComponent(typeof(FillCounter))]
    public sealed class ShapeFillOrchestrator : MonoBehaviour
    {
        public event Action<float> FillCompleted;

        private GridBuilder _gridBuilder;
        private ShapeFiller _shapeFiller;
        private FillCounter _fillCounter;

        private void Awake()
        {
            _gridBuilder = GetComponent<GridBuilder>();
            _shapeFiller = GetComponent<ShapeFiller>();
            _fillCounter = GetComponent<FillCounter>();
        }

        private void OnEnable()
        {
            _shapeFiller.FillCompleted += OnFillCompleted;
        }

        private void OnDisable()
        {
            _shapeFiller.FillCompleted -= OnFillCompleted;
        }

        public void StartFill()
        {
            _shapeFiller.Initialize();
            _shapeFiller.BuildShape();

            int maxCubes = _gridBuilder.FillCells.Count;

            _shapeFiller.Fill(
                _fillCounter.CalculateQuotaFill(maxCubes),
                _fillCounter.CalculateBonusFill(maxCubes));
        }

        private void OnFillCompleted(float fillPercent)
        {
            FillCompleted?.Invoke(fillPercent);
        }
    }
}
