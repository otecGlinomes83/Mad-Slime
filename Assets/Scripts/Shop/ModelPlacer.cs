using System;
using UnityEngine;

public class ModelPlacer : MonoBehaviour
{
    [SerializeField] private float _rotationSpeed = 45f;
    [SerializeField] private float _padding = 0.85f;
    [SerializeField] private float _lift = 0f;
    [SerializeField] private Transform _modelsParent;
    [SerializeField] private Camera _camera;

    private GameObject _currentModel;
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
        if (_currentModel == null || _modelsParent == null)
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
        FitToCamera(_currentModel);
    }

    private void FitToCamera(GameObject instance)
    {
        Bounds worldBounds = GetAccurateWorldBounds(instance);

        Vector3 centerOffset = _modelsParent.position - worldBounds.center;
        instance.transform.position += centerOffset;
        instance.transform.position += new Vector3(0f, _lift, 0f);

        _rotationAnchor = _modelsParent.position + new Vector3(0f, _lift, 0f);

        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
        Bounds localBounds = ComputeLocalBounds(renderers);

        float maxVerticalExtent = Mathf.Max(localBounds.extents.y, localBounds.extents.x / _camera.aspect);

        if (maxVerticalExtent <= 0f)
        {
            return;
        }

        _camera.orthographicSize = Mathf.Max(0.1f, maxVerticalExtent / _padding);
    }

    private Bounds GetAccurateWorldBounds(GameObject instance)
    {
        Bounds? result = null;

        foreach (MeshFilter meshFilter in instance.GetComponentsInChildren<MeshFilter>())
        {
            if (meshFilter.sharedMesh == null)
            {
                continue;
            }

            AccumulateByCorners(meshFilter.transform, meshFilter.sharedMesh.bounds, ref result);
        }

        foreach (SkinnedMeshRenderer skinnedRenderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            if (skinnedRenderer.sharedMesh == null)
            {
                continue;
            }

            AccumulateByCorners(skinnedRenderer.transform, skinnedRenderer.sharedMesh.bounds, ref result);
        }

        if (!result.HasValue)
        {
            throw new InvalidOperationException(
                $"{name}: Model '{instance.name}' has no MeshFilter or SkinnedMeshRenderer with sharedMesh. Cannot compute bounds.");
        }

        return result.Value;
    }

    private void AccumulateByCorners(Transform sourceTransform, Bounds localBounds, ref Bounds? accumulator)
    {
        Vector3 center = localBounds.center;
        Vector3 extents = localBounds.extents;

        Vector3[] localCorners = new Vector3[8];
        localCorners[0] = center + new Vector3(-extents.x, -extents.y, -extents.z);
        localCorners[1] = center + new Vector3(+extents.x, -extents.y, -extents.z);
        localCorners[2] = center + new Vector3(-extents.x, +extents.y, -extents.z);
        localCorners[3] = center + new Vector3(+extents.x, +extents.y, -extents.z);
        localCorners[4] = center + new Vector3(-extents.x, -extents.y, +extents.z);
        localCorners[5] = center + new Vector3(+extents.x, -extents.y, +extents.z);
        localCorners[6] = center + new Vector3(-extents.x, +extents.y, +extents.z);
        localCorners[7] = center + new Vector3(+extents.x, +extents.y, +extents.z);

        Bounds cornersBounds = new Bounds(sourceTransform.TransformPoint(localCorners[0]), Vector3.zero);

        for (int i = 1; i < 8; i++)
        {
            cornersBounds.Encapsulate(sourceTransform.TransformPoint(localCorners[i]));
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