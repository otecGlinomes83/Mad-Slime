using System;
using UnityEngine;

namespace Skins
{
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    public sealed class SkinModel : MonoBehaviour
    {
        private MeshRenderer _renderer;
        private MeshFilter _meshFilter;

        public MeshRenderer Renderer => _renderer;
        public MeshFilter MeshFilter => _meshFilter;

        private void Awake()
        {
            if (TryGetComponent(out _renderer) == false)
            {
                throw new InvalidOperationException(
                    $"{name}: MeshRenderer is missing. SkinModel requires a MeshRenderer on the same object.");
            }

            if (TryGetComponent(out _meshFilter) == false)
            {
                throw new InvalidOperationException(
                    $"{name}: MeshFilter is missing. SkinModel requires a MeshFilter on the same object.");
            }

            if (_meshFilter.sharedMesh == null)
            {
                throw new InvalidOperationException(
                    $"{name}: MeshFilter has no mesh assigned.");
            }
        }
    }
}
