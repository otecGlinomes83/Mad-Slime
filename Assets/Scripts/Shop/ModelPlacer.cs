using System;
using UnityEngine;

namespace Skins
{
    public sealed class ModelPlacer : MonoBehaviour
    {
        [SerializeField] private float _rotationSpeed = 45f;
        [SerializeField] private float _padding = 0.85f;
        [SerializeField] private float _lift = 0f;
        [SerializeField] private Transform _modelsParent;
        [SerializeField] private Camera _camera;

        private readonly Vector3[] _localCorners = new Vector3[8];

        private GameObject _currentModel;
        private SkinModel _currentSkinModel;
        private Animator _currentAnimator;
        private Vector3 _rotationAnchor;

        private void Awake()
        {
            if (_modelsParent == null)
            {
                throw new InvalidOperationException(
                    $"{name}: ModelPlacer requires _modelsParent to be assigned in the inspector.");
            }

            if (_camera == null)
            {
                throw new InvalidOperationException(
                    $"{name}: ModelPlacer requires _camera to be assigned in the inspector.");
            }
        }

        private void Update()
        {
            if (_currentModel == null)
            {
                return;
            }

            _currentModel.transform.RotateAround(
                _rotationAnchor,
                Vector3.up,
                _rotationSpeed * Time.unscaledDeltaTime);
        }

        public void SetModel(GameObject model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model),
                    $"{name}: SetModel requires a non-null model.");
            }

            if (_currentModel != null)
            {
                Destroy(_currentModel);
            }

            _currentModel = Instantiate(model, _modelsParent);
            _currentModel.TryGetComponent(out _currentAnimator);

            if (_currentModel.TryGetComponent(out _currentSkinModel) == false)
            {
                throw new InvalidOperationException(
                    $"{name}: Model '{model.name}' has no SkinModel component. Add a SkinModel component to the model prefab and drag its Renderers and MeshFilters into it.");
            }

            FitToCamera(_currentSkinModel);
        }

        public void PlayWalk()
        {
            if (_currentAnimator == null)
            {
                return;
            }

            _currentAnimator.SetTrigger("Walk");
        }

        private void FitToCamera(SkinModel skinModel)
        {
            Bounds worldBounds = GetAccurateWorldBounds(skinModel);

            Vector3 centerOffset = _modelsParent.position - worldBounds.center;
            _currentModel.transform.position += centerOffset;
            _currentModel.transform.position += new Vector3(0f, _lift, 0f);

            _rotationAnchor = _modelsParent.position + new Vector3(0f, _lift, 0f);

            Bounds localBounds = ComputeLocalBounds(skinModel.Renderers);

            float maxVerticalExtent = Mathf.Max(localBounds.extents.y, localBounds.extents.x / _camera.aspect);

            if (maxVerticalExtent <= 0f)
            {
                return;
            }

            _camera.orthographicSize = Mathf.Max(0.1f, maxVerticalExtent / _padding);
        }

        private Bounds GetAccurateWorldBounds(SkinModel skinModel)
        {
            Bounds? result = null;

            MeshFilter[] meshFilters = skinModel.MeshFilters;

            if (meshFilters != null)
            {
                for (int i = 0; i < meshFilters.Length; i++)
                {
                    if (meshFilters[i].sharedMesh == null)
                    {
                        continue;
                    }

                    AccumulateByCorners(meshFilters[i].transform, meshFilters[i].sharedMesh.bounds, ref result);
                }
            }

            Renderer[] renderers = skinModel.Renderers;

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] is SkinnedMeshRenderer skinnedRenderer && skinnedRenderer.sharedMesh != null)
                {
                    AccumulateByCorners(skinnedRenderer.transform, skinnedRenderer.sharedMesh.bounds, ref result);
                }
            }

            if (result.HasValue == false)
            {
                throw new InvalidOperationException(
                    $"{name}: SkinModel '{skinModel.name}' has no MeshFilter or SkinnedMeshRenderer with sharedMesh. Cannot compute bounds.");
            }

            return result.Value;
        }

        private void AccumulateByCorners(Transform sourceTransform, Bounds localBounds, ref Bounds? accumulator)
        {
            Vector3 center = localBounds.center;
            Vector3 extents = localBounds.extents;

            _localCorners[0] = center + new Vector3(-extents.x, -extents.y, -extents.z);
            _localCorners[1] = center + new Vector3(+extents.x, -extents.y, -extents.z);
            _localCorners[2] = center + new Vector3(-extents.x, +extents.y, -extents.z);
            _localCorners[3] = center + new Vector3(+extents.x, -extents.y, -extents.z);
            _localCorners[4] = center + new Vector3(-extents.x, -extents.y, +extents.z);
            _localCorners[5] = center + new Vector3(+extents.x, -extents.y, +extents.z);
            _localCorners[6] = center + new Vector3(-extents.x, +extents.y, +extents.z);
            _localCorners[7] = center + new Vector3(+extents.x, +extents.y, +extents.z);

            Bounds cornersBounds = new Bounds(sourceTransform.TransformPoint(_localCorners[0]), Vector3.zero);

            for (int i = 1; i < 8; i++)
            {
                cornersBounds.Encapsulate(sourceTransform.TransformPoint(_localCorners[i]));
            }

            if (accumulator.HasValue)
            {
                Bounds existing = accumulator.Value;
                existing.Encapsulate(cornersBounds);
                accumulator = existing;
            }
            else
            {
                accumulator = cornersBounds;
            }
        }

        private Bounds ComputeLocalBounds(Renderer[] renderers)
        {
            Vector3 firstLocalCenter = _modelsParent.InverseTransformPoint(renderers[0].bounds.center);
            Bounds localBounds = new Bounds(firstLocalCenter, Vector3.zero);

            for (int i = 0; i < renderers.Length; i++)
            {
                Bounds world = renderers[i].bounds;
                Vector3 localMin = _modelsParent.InverseTransformPoint(world.min);
                Vector3 localMax = _modelsParent.InverseTransformPoint(world.max);
                Vector3 localCenter = (localMin + localMax) * 0.5f;
                Vector3 localSize = localMax - localMin;
                localBounds.Encapsulate(new Bounds(localCenter, localSize));
            }

            return localBounds;
        }
    }
}
