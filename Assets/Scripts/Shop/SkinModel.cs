using System;
using UnityEngine;

namespace Skins
{
    public sealed class SkinModel : MonoBehaviour
    {
        [SerializeField] private Renderer[] _renderers;
        [SerializeField] private MeshFilter[] _meshFilters;

        public Renderer[] Renderers => _renderers;
        public MeshFilter[] MeshFilters => _meshFilters;

        private void Awake()
        {
            if (_renderers == null || _renderers.Length == 0)
            {
                throw new InvalidOperationException(
                    $"{name}: Renderers list is empty. Drag every Renderer of the model into the Renderers list on the SkinModel component.");
            }
        }
    }
}
