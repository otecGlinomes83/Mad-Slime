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

        private void OnDisable()
        {
            _shapeFiller.FillCompleted -= OnFillCompleted;
        }

        public void StartFill()
        {
            _shapeFiller.FillCompleted += OnFillCompleted;

            _shapeFiller.Initialize();
            _gridBuilder.Build();
            _shapeFiller.BuildShape();
            _shapeFiller.Fill(_fillCounter.CalculateFill(_gridBuilder.FillCells.Count));
        }

        private void OnFillCompleted(float fillPercent)
        {
            FillCompleted?.Invoke(fillPercent);
        }
    }
}
