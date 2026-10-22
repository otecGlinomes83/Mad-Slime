using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shop
{
    public class ModelPlacer : MonoBehaviour
    {
        private const string PreviewLayerName = "SkinsRender";
        private const float FaceTurnDegrees = 45f;
        private const float ClipMargin = 0.05f;
        private const float HalfFactor = 0.5f;

        [SerializeField] private PreviewRotator _rotator;
        [SerializeField] private float _padding = 0.85f;
        [SerializeField, Min(0.1f), Tooltip("Общий масштаб после нормализации размеров моделей. 1 — стандартный размер.")]
        private float _previewZoom = 1f;
        [SerializeField, Min(0.1f), Tooltip("Размер модели до применения общего масштаба: максимальная высота или диаметр при вращении.")]
        private float _normalizedSize = 2f;
        [SerializeField] private Transform _modelsParent;
        [SerializeField] private Camera _camera;

        private List<Vector3> _worldCorners = new List<Vector3>();
        private List<Vector3> _modelCorners = new List<Vector3>();

        private GameObject _currentModel;
        private Vector3 _rotationAnchor;

        private void Awake()
        {
            if (_rotator == null)
            {
                throw new InvalidOperationException($"{name}: PreviewRotator is required.");
            }

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

        private void OnEnable()
        {
            _rotator.Rotated += OnModelRotated;
        }

        private void OnDisable()
        {
            _rotator.Rotated -= OnModelRotated;
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

            if (_currentModel.TryGetComponent(out SkinModel skinModel) == false)
            {
                throw new InvalidOperationException(
                    $"{name}: Model '{model.name}' has no SkinModel component. Add a SkinModel component to the model prefab root.");
            }

            SetPreviewLayer(_currentModel);
            FaceCamera(_currentModel.transform.rotation);
            FitToCamera(skinModel);
        }

        public void Clear()
        {
            _rotator.Setup(null, Vector3.zero);
            _modelCorners.Clear();

            if (_currentModel != null)
            {
                Destroy(_currentModel);
                _currentModel = null;
            }
        }

        private void SetPreviewLayer(GameObject model)
        {
            int layer = LayerMask.NameToLayer(PreviewLayerName);

            if (layer < 0)
            {
                throw new InvalidOperationException($"{name}: layer '{PreviewLayerName}' is required for skin previews.");
            }

            Transform[] transforms = model.GetComponentsInChildren<Transform>(true);

            for (int i = 0; i < transforms.Length; i++)
            {
                transforms[i].gameObject.layer = layer;
            }
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
            Bounds worldBounds = GetWorldBounds(skinModel);
            float radius = 0f;
            foreach (Vector3 corner in _worldCorners)
            {
                Vector3 offset = corner - worldBounds.center;
                radius = Mathf.Max(radius, new Vector2(offset.x, offset.z).magnitude);
            }

            float originalSize = Mathf.Max(worldBounds.size.y, radius * 2f);
            if (originalSize <= Mathf.Epsilon)
            {
                throw new InvalidOperationException($"{name}: preview model '{skinModel.name}' has empty bounds.");
            }

            float targetSize = Mathf.Max(0.1f, _normalizedSize);
            float scale = targetSize / originalSize * Mathf.Max(0.1f, _previewZoom);
            Transform modelTransform = _currentModel.transform;
            Vector3 originalPosition = modelTransform.position;
            _modelCorners.Clear();

            for (int i = 0; i < _worldCorners.Count; i++)
            {
                _modelCorners.Add(modelTransform.InverseTransformPoint(_worldCorners[i]));
            }

            modelTransform.localScale *= scale;
            Vector3 scaledCenter = originalPosition + (worldBounds.center - originalPosition) * scale;
            float halfHeight = worldBounds.extents.y * scale;
            radius *= scale;
            float normalizedExtent = targetSize * HalfFactor;
            float commonSize = CalculateCameraSize(normalizedExtent, normalizedExtent);
            float requiredSize = CalculateCameraSize(radius, halfHeight);
            _camera.orthographicSize = Mathf.Max(commonSize, requiredSize);

            Vector3 forward = _camera.transform.forward;
            float depthExtent = ProjectCylinder(radius, halfHeight, forward);
            float minimumDepth = _camera.nearClipPlane + depthExtent + ClipMargin;
            Vector3 anchor = GetViewCenter(worldBounds.center, minimumDepth);
            modelTransform.position += anchor - scaledCenter;
            _rotationAnchor = anchor;
            float farthestDepth = Vector3.Dot(anchor - _camera.transform.position, forward) + depthExtent;
            _camera.farClipPlane = Mathf.Max(_camera.farClipPlane, farthestDepth + ClipMargin);
            AlignToCameraBottom();
        }

        private float CalculateCameraSize(float radius, float halfHeight)
        {
            float horizontalExtent = ProjectCylinder(radius, halfHeight, _camera.transform.right);
            float verticalExtent = ProjectCylinder(radius, halfHeight, _camera.transform.up);
            float requiredSize = Mathf.Max(verticalExtent, horizontalExtent / _camera.aspect);
            float fill = Mathf.Clamp(_padding, 0.1f, 0.95f);
            return requiredSize / fill;
        }

        private void OnModelRotated()
        {
            AlignToCameraBottom();
        }

        private void AlignToCameraBottom()
        {
            if (_currentModel == null)
            {
                return;
            }

            Transform modelTransform = _currentModel.transform;
            Vector3 cameraUp = _camera.transform.up;
            Vector3 cameraPosition = _camera.transform.position;
            float minimumHeight = Mathf.Infinity;

            for (int i = 0; i < _modelCorners.Count; i++)
            {
                Vector3 corner = modelTransform.TransformPoint(_modelCorners[i]);
                float height = Vector3.Dot(corner - cameraPosition, cameraUp);
                minimumHeight = Mathf.Min(minimumHeight, height);
            }

            Vector3 offset = cameraUp * (-_camera.orthographicSize - minimumHeight);
            modelTransform.position += offset;
            _rotationAnchor += offset;
            _rotator.Setup(modelTransform, _rotationAnchor);
        }

        private static float ProjectCylinder(float radius, float halfHeight, Vector3 axis)
        {
            return radius * new Vector2(axis.x, axis.z).magnitude + halfHeight * Mathf.Abs(axis.y);
        }

        private Vector3 GetViewCenter(Vector3 position, float minimumDepth)
        {
            Vector3 forward = _camera.transform.forward;
            float depth = Mathf.Max(minimumDepth, Vector3.Dot(position - _camera.transform.position, forward));

            return _camera.transform.position + forward * depth;
        }

        private Bounds GetWorldBounds(SkinModel skinModel)
        {
            _worldCorners.Clear();
            foreach (MeshFilter meshFilter in skinModel.GetComponentsInChildren<MeshFilter>())
            {
                if (meshFilter.sharedMesh != null)
                {
                    AddWorldCorners(meshFilter.transform, meshFilter.sharedMesh.bounds);
                }
            }
            foreach (SkinnedMeshRenderer renderer in skinModel.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (renderer.sharedMesh != null)
                {
                    AddWorldCorners(renderer.transform, renderer.localBounds);
                }
            }
            if (_worldCorners.Count == 0)
            {
                throw new InvalidOperationException($"{name}: SkinModel '{skinModel.name}' has no preview mesh.");
            }
            Bounds worldBounds = new Bounds(_worldCorners[0], Vector3.zero);
            foreach (Vector3 corner in _worldCorners)
            {
                worldBounds.Encapsulate(corner);
            }
            return worldBounds;
        }

        private void AddWorldCorners(Transform sourceTransform, Bounds bounds)
        {
            for (int cornerX = -1; cornerX <= 1; cornerX += 2)
            {
                for (int cornerY = -1; cornerY <= 1; cornerY += 2)
                {
                    for (int cornerZ = -1; cornerZ <= 1; cornerZ += 2)
                    {
                        Vector3 direction = new Vector3(cornerX, cornerY, cornerZ);
                        Vector3 corner = bounds.center + Vector3.Scale(bounds.extents, direction);
                        _worldCorners.Add(sourceTransform.TransformPoint(corner));
                    }
                }
            }
        }
    }
}
