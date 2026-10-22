using System;
using Scriptables;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game
{
    public class LevelSurface
    {
        private const float BackgroundSizeMultiplier = 64f;
        private const float BackgroundThickness = 1f;

        private MeshRenderer _background;
        private MeshRenderer _edge;
        private Vector3 _meshSize;

        public LevelSurface(Mesh mesh, Transform parent)
        {
            _meshSize = mesh.bounds.size;
            _background = CreateSurface("LowerEnvironment", mesh, parent);
            _edge = CreateSurface("PlatformEdge", mesh, parent);
        }

        public void Apply(LevelTheme theme, Bounds floorBounds)
        {
            if (theme.BackgroundMaterial == null || theme.EdgeMaterial == null)
            {
                throw new InvalidOperationException($"{theme.name}: background and edge materials are required.");
            }

            float edgeTop = floorBounds.min.y;
            Vector3 edgeCenter = floorBounds.center;
            edgeCenter.y = edgeTop - theme.PlatformDepth * 0.5f;
            SetSurface(_edge, theme.EdgeMaterial, edgeCenter,
                new Vector3(floorBounds.size.x, theme.PlatformDepth, floorBounds.size.z));
            _edge.enabled = theme.EnvironmentPrefab == null;

            Vector3 backgroundCenter = floorBounds.center;
            backgroundCenter.y = edgeTop - theme.PlatformDepth - BackgroundThickness * 0.5f;
            SetSurface(_background, theme.BackgroundMaterial, backgroundCenter,
                new Vector3(floorBounds.size.x * BackgroundSizeMultiplier, BackgroundThickness,
                    floorBounds.size.z * BackgroundSizeMultiplier));
        }

        private MeshRenderer CreateSurface(string name, Mesh mesh, Transform parent)
        {
            GameObject surface = new GameObject(name);
            surface.transform.SetParent(parent, false);
            surface.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = surface.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            return renderer;
        }

        private void SetSurface(MeshRenderer renderer, Material material, Vector3 position, Vector3 size)
        {
            renderer.sharedMaterial = material;
            renderer.transform.position = position;
            renderer.transform.localScale = new Vector3(size.x / _meshSize.x, size.y / _meshSize.y, size.z / _meshSize.z);
        }
    }
}
