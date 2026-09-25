using System;
using UnityEngine;

namespace ShapeFill
{
    public sealed class CubeSpawner : MonoBehaviour
    {
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

        public FlyingCube Spawn(Vector3 position, Quaternion rotation, float scale, Color color)
        {
            FlyingCube cube = Instantiate(_cubePrefab, position, rotation, _cubesParent);
            cube.transform.localScale = Vector3.one * scale;

            SetColor(cube.gameObject, color);

            return cube;
        }

        private void SetColor(GameObject cube, Color color)
        {
            if (cube.TryGetComponent(out Renderer cubeRenderer) == false)
            {
                return;
            }

            _propertyBlock.SetColor(Shader.PropertyToID("_Color"), color);
            cubeRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}