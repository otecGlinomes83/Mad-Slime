using System;
using UnityEngine;

namespace ShapeFill
{
    public class FillResult
    {
        private int _quotaCubes;
        private int _bonusCubes;
        private int _targetCubes;
        private float _percent;
        private bool _isWin;

        public int QuotaCubes => _quotaCubes;

        public int BonusCubes => _bonusCubes;

        public int TargetCubes => _targetCubes;

        public float Percent => _percent;

        public bool IsWin => _isWin;

        public FillResult(int quotaCubes, int bonusCubes, int requiredCubes)
        {
            if (quotaCubes < 0 || bonusCubes < 0 || requiredCubes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(requiredCubes), "Fill result requires a built nonempty shape and nonnegative counts.");
            }

            _quotaCubes = quotaCubes;
            _bonusCubes = bonusCubes;
            _targetCubes = Mathf.Clamp(quotaCubes + bonusCubes, 0, requiredCubes);
            _percent = Mathf.Clamp01(_targetCubes / (float)requiredCubes);
            _isWin = _percent >= 1f;
        }
    }
}
