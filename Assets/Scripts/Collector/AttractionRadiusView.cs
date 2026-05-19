using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public sealed class AttractionRadiusView : MonoBehaviour
{
    [SerializeField] private CollectableAttractor _attractor;
    [SerializeField, Range(8, 128)] private int _segments = 64;
    [SerializeField] private float _heightOffset = 0.05f;

    private LineRenderer _lineRenderer;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.loop = true;
        _lineRenderer.useWorldSpace = false;
    }

    private void LateUpdate()
    {
        BuildCircle(_attractor.Radius);
    }

    private void BuildCircle(float radius)
    {
        _lineRenderer.positionCount = _segments;

        float angleStep = 360f / _segments;

        for (int i = 0; i < _segments; i++)
        {
            float angleRad = Mathf.Deg2Rad * angleStep * i;
            float x = Mathf.Cos(angleRad) * radius;
            float z = Mathf.Sin(angleRad) * radius;

            _lineRenderer.SetPosition(i, new Vector3(x, _heightOffset, z));
        }
    }
}
