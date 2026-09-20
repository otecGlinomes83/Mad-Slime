using System;
using Game;
using UnityEngine;

namespace ShapeFill
{
    public sealed class FillDebug : MonoBehaviour
    {
        [SerializeField] private ShapeFiller _filler;

        [SerializeField] private FillSessionHandler _sessionHandler;

        [SerializeField] private Transform _cubesContainer;

        [SerializeField] private GameObject _fillProgressLabel;

        [Tooltip("Сколько квотных предметов «набрано» в тесте")]
        [SerializeField, Min(0)] private int _testQuotaCollected = 12;

        [Tooltip("Целевой объём квоты уровня (знаменатель формулы FillPercent)")]
        [SerializeField, Min(0)] private int _testQuotaTarget = 20;

        [Tooltip("Сколько обычных (не квотных) предметов «набрано». Каждый даёт +1 к заливке, как квотный")]
        [SerializeField, Min(0)] private int _testDefaultCollected;

        private Camera _camera;
        private float _startFov;

        private void Awake()
        {
            if (_filler == null)
            {
                throw new InvalidOperationException(
                    $"{name}: ShapeFiller is not assigned. Drag a ShapeFiller component into the _filler field.");
            }

            if (_sessionHandler == null)
            {
                throw new InvalidOperationException(
                    $"{name}: FillSessionHandler is not assigned. Drag a FillSessionHandler component into the _sessionHandler field.");
            }

            if (_cubesContainer == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Cubes container is not assigned. Drag the cubes container Transform into the _cubesContainer field.");
            }

            if (_fillProgressLabel == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Fill progress label is not assigned. Drag the FillProgress GameObject into the _fillProgressLabel field.");
            }

            _camera = Camera.main;

            if (_camera == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Camera.main is not found. Tag the fill scene camera with the MainCamera tag.");
            }

            _startFov = _camera.fieldOfView;

            _sessionHandler.enabled = false;
        }

        [ContextMenu("Run Test Fill")]
        private void RunTestFill()
        {
            if (_camera == null)
            {
                throw new InvalidOperationException(
                    $"{name}: component is not initialized. Enter Play Mode before running the test fill.");
            }

            _camera.fieldOfView = _startFov;

            ClearCubes();
            _fillProgressLabel.SetActive(true);

            int requiredCount = _filler.RequiredFillCount;
            int quotaCount = Mathf.Clamp(Mathf.RoundToInt(GetTestQuotaPercent() * requiredCount), 0, requiredCount);
            int totalCount = Mathf.Clamp(Mathf.RoundToInt(GetTestPercent() * requiredCount), quotaCount, requiredCount);

            _filler.Initialize();
            _filler.BuildShape();
            _filler.Fill(quotaCount, totalCount - quotaCount);
        }

        private float GetTestQuotaPercent()
        {
            if (_testQuotaTarget <= 0)
            {
                return 0f;
            }

            return Mathf.Clamp01(_testQuotaCollected / (float)_testQuotaTarget);
        }

        private float GetTestPercent()
        {
            if (_testQuotaTarget <= 0)
            {
                return 0f;
            }

            float percent = (_testQuotaCollected + _testDefaultCollected) / (float)_testQuotaTarget;

            return Mathf.Clamp01(percent);
        }

        private void ClearCubes()
        {
            for (int i = _cubesContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_cubesContainer.GetChild(i).gameObject);
            }
        }
    }
}
