using System;
using UnityEngine;

namespace Shop
{
    public sealed class ModelPlacer : MonoBehaviour
    {
        private const float FaceTurnDegrees = 45f;

        [SerializeField] private float _rotationSpeed = 45f;
        [SerializeField] private float _padding = 0.85f;
        [SerializeField, Tooltip("Подъём модели над центром экрана после подгонки.")]
        private float _lift = 0f;
        [SerializeField] private Transform _modelsParent;
        [SerializeField] private Camera _camera;

        private readonly Vector3[] _localCorners = new Vector3[8];

        private GameObject _currentModel;
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

            if (_currentModel.TryGetComponent(out SkinModel skinModel) == false)
            {
                throw new InvalidOperationException(
                    $"{name}: Model '{model.name}' has no SkinModel component. Add a SkinModel component to the model prefab root.");
            }

            FaceCamera(_currentModel.transform.rotation);
            FitToCamera(skinModel);
        }

        public void PlayWalk()
        {
            if (_currentAnimator == null)
            {
                return;
            }

            _currentAnimator.SetTrigger(SkinModel.WalkTrigger);
        }

        private void FaceCamera(Quaternion baseRotation)
        {
            Vector3 toCamera = _camera.transform.position - _currentModel.transform.position;
            toCamera.y = 0f;

            if (toCamera.sqrMagnitude <= 0f)
            {
                throw new InvalidOperationException(
                    $"{name}: the preview camera is directly above the spawn point. Move the camera so the model can face it.");
            }

            float yaw = Mathf.Atan2(toCamera.x, toCamera.z) * Mathf.Rad2Deg;
            _currentModel.transform.rotation =
                Quaternion.Euler(0f, yaw + FaceTurnDegrees, 0f) * baseRotation;
        }

        private void FitToCamera(SkinModel skinModel)
        {
            Bounds worldBounds = GetAccurateWorldBounds(skinModel);
            Vector3 anchor = GetViewCenter(worldBounds.center) + Vector3.up * _lift;
            _currentModel.transform.position += anchor - worldBounds.center;
            _rotationAnchor = anchor;

            float maxVerticalExtent = Mathf.Max(worldBounds.extents.y, worldBounds.extents.x / _camera.aspect);

            if (maxVerticalExtent <= 0f)
            {
                return;
            }

            _camera.orthographicSize = Mathf.Max(0.1f, maxVerticalExtent / _padding);
        }

        private Vector3 GetViewCenter(Vector3 position)
        {
            Vector3 forward = _camera.transform.forward;
            float depth = Vector3.Dot(position - _camera.transform.position, forward);

            return _camera.transform.position + forward * depth;
        }

        private Bounds GetAccurateWorldBounds(SkinModel skinModel)
        {
            MeshFilter meshFilter = skinModel.MeshFilter;

            if (meshFilter == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SkinModel '{skinModel.name}' has no MeshFilter component. SkinModel requires one on the same object.");
            }

            if (meshFilter.sharedMesh == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SkinModel '{skinModel.name}' has no mesh in its MeshFilter. Cannot compute bounds.");
            }

            return TransformCornersToBounds(meshFilter.transform, meshFilter.sharedMesh.bounds);
        }

        private Bounds TransformCornersToBounds(Transform sourceTransform, Bounds localBounds)
        {
            Vector3 center = localBounds.center;
            Vector3 extents = localBounds.extents;

            _localCorners[0] = center + new Vector3(-extents.x, -extents.y, -extents.z);
            _localCorners[1] = center + new Vector3(+extents.x, -extents.y, -extents.z);
            _localCorners[2] = center + new Vector3(-extents.x, +extents.y, -extents.z);
            _localCorners[3] = center + new Vector3(+extents.x, +extents.y, -extents.z);
            _localCorners[4] = center + new Vector3(-extents.x, -extents.y, +extents.z);
            _localCorners[5] = center + new Vector3(+extents.x, -extents.y, +extents.z);
            _localCorners[6] = center + new Vector3(-extents.x, +extents.y, +extents.z);
            _localCorners[7] = center + new Vector3(+extents.x, +extents.y, +extents.z);

            Bounds worldBounds = new Bounds(sourceTransform.TransformPoint(_localCorners[0]), Vector3.zero);

            for (int i = 1; i < 8; i++)
            {
                worldBounds.Encapsulate(sourceTransform.TransformPoint(_localCorners[i]));
            }

            return worldBounds;
        }
    }
}
