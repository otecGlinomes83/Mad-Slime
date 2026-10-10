using System;
using UnityEngine;
using VContainer;

namespace ShapeFill
{
    [RequireComponent(typeof(GridBuilder))]
    [RequireComponent(typeof(ShapeFiller))]
    public class ShapeFillOrchestrator : MonoBehaviour
    {
        public event Action<float> FillCompleted;

        private GridBuilder _gridBuilder;
        private ShapeFiller _shapeFiller;
        private FillResultCalculator _resultCalculator;

        public bool CanRescue => _shapeFiller.CanRescue;

        [Inject]
        public void Construct(GridBuilder gridBuilder, ShapeFiller shapeFiller, FillResultCalculator resultCalculator)
        {
            _gridBuilder = gridBuilder;
            _shapeFiller = shapeFiller;
            _resultCalculator = resultCalculator;
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

            if (_resultCalculator == null)
            {
                throw new InvalidOperationException(
                    $"{name}: FillResultCalculator was not injected. Check that FillLifetimeScope registers FillResultCalculator and ShapeFillOrchestrator.");
            }
        }

        private void OnEnable()
        {
            _shapeFiller.FillCompleted += OnFillCompleted;
        }

        private void OnDisable()
        {
            if (_shapeFiller != null)
            {
                _shapeFiller.FillCompleted -= OnFillCompleted;
            }
        }

        public void Prepare()
        {
            _shapeFiller.Initialize();
            _shapeFiller.BuildShape();
        }

        public FillResult CalculateResult()
        {
            return _resultCalculator.Calculate(_gridBuilder.FillCells.Count);
        }

        public void Show(FillResult result)
        {
            _shapeFiller.ShowResult(result);
        }

        public void Stop()
        {
            _shapeFiller.StopFill();
        }

        public void RescueShow()
        {
            _shapeFiller.Rescue();
        }

        public void Accelerate()
        {
            _shapeFiller.Accelerate();
        }

        private void OnFillCompleted(float fillPercent)
        {
            FillCompleted?.Invoke(fillPercent);
        }
    }
}
