using System;
using UnityEngine;

namespace ShapeFill
{
    public class CubeSpawner : MonoBehaviour
    {
        private static int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private FlyingCube _cubePrefab;
        [SerializeField] private Transform _cubesParent;

        private MaterialPropertyBlock _propertyBlock;

        public void Initialize()
        {
            if (_cubePrefab == null)
            {
                throw new InvalidOperationException(
                    $"{name}: FlyingCube prefab is not assigned. Drag a FlyingCube prefab into the _cubePrefab field.");
            }

            if (_cubesParent == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Cubes parent is not assigned. Drag a Transform into the _cubesParent field.");
            }

            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }
        }

        public CubeFlightAnimator Spawn(Vector3 position, Quaternion rotation, float scale, Color color)
        {
            FlyingCube cube = Instantiate(_cubePrefab, position, rotation, _cubesParent);
            cube.transform.localScale = Vector3.one * scale;

            SetColor(cube.gameObject, color);

            if (cube.TryGetComponent(out CubeFlightAnimator flightAnimator) == false)
            {
                throw new InvalidOperationException(
                    $"{name}: the FlyingCube prefab '{_cubePrefab.name}' has no CubeFlightAnimator. Add one to the prefab.");
            }

            return flightAnimator;
        }

        private void SetColor(GameObject cube, Color color)
        {
            if (cube.TryGetComponent(out Renderer cubeRenderer) == false)
            {
                return;
            }

            _propertyBlock.SetColor(ColorId, color);
            cubeRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
