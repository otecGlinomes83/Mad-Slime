using System;
using UnityEngine;

namespace UI
{
    public sealed class LookAtCamera : MonoBehaviour
    {
        [SerializeField] private Camera _camera;

        private void Awake()
        {
            if (_camera == null)
            {
                throw new InvalidOperationException(
                    $"{name}: LookAtCamera requires a Camera. Drag the Main Camera into the _camera field.");
            }
        }

        private void LateUpdate()
        {
            transform.rotation = _camera.transform.rotation;
        }
    }
}
