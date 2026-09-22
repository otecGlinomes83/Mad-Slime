using System;
using UnityEngine;
using VContainer;

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

        [Inject]
        public void Construct(GridBuilder gridBuilder, ShapeFiller shapeFiller, FillCounter fillCounter)
        {
            _gridBuilder = gridBuilder;
            _shapeFiller = shapeFiller;
            _fillCounter = fillCounter;
        }

        private void Awake()
        {
            if (_gridBuilder == null)
            {
                throw new InvalidOperationException(
                    $"{name}: GridBuilder was not injected. Check that FillLifetimeScope registers GridBuilder and ShapeFillOrchestrator.");
            }

            if (_shapeFiller == null)
            {
                throw new InvalidOperationException(
                    $"{name}: ShapeFiller was not injected. Check that FillLifetimeScope registers ShapeFiller and ShapeFillOrchestrator.");
            }

            if (_fillCounter == null)
            {
                throw new InvalidOperationException(
                    $"{name}: FillCounter was not injected. Check that FillLifetimeScope registers FillCounter and ShapeFillOrchestrator.");
            }
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