using System;
using UnityEngine;

namespace Shop
{
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    public sealed class SkinModel : MonoBehaviour
    {
        public const string WalkTrigger = "Walk";

        private MeshRenderer _renderer;
        private MeshFilter _meshFilter;

        public MeshRenderer Renderer
        {
            get
            {
                if (_renderer == null && TryGetComponent(out MeshRenderer renderer))
                {
                    _renderer = renderer;
                }

                return _renderer;
            }
        }

        public MeshFilter MeshFilter
        {
            get
            {
                if (_meshFilter == null && TryGetComponent(out MeshFilter meshFilter))
                {
                    _meshFilter = meshFilter;
                }

                return _meshFilter;
            }
        }

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
