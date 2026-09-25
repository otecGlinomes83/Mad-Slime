using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class MenuBackground : MonoBehaviour
    {
        private static readonly int UnscaledTimeId = Shader.PropertyToID("_UnscaledTime");

        [SerializeField] private RawImage _background;
        [SerializeField] private float _timeSpeed = 1f;

        private Material _material;
        private float _elapsedTime;

        private void Awake()
        {
            if (_background == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Background is not assigned. Drag the background RawImage into the _background field.");
            }

            _material = new Material(_background.material);
            _background.material = _material;
        }

        private void Update()
        {
            _elapsedTime += Time.unscaledDeltaTime * _timeSpeed;
            _material.SetFloat(UnscaledTimeId, _elapsedTime);
        }

        private void OnDestroy()
        {
            if (_material != null)
            {
                Destroy(_material);
            }
        }
    }
}
