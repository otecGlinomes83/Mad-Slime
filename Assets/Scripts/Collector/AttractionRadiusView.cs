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
        _lineRenderer.useWorldSpace = true;
    }

    private void LateUpdate()
    {
        _lineRenderer.positionCount = _segments;

        float angleStep = 360f / _segments;

        for (int i = 0; i < _segments; i++)
        {
            float angleRad = Mathf.Deg2Rad * angleStep * i;
            float x = Mathf.Cos(angleRad) * _attractor.Radius;
            float z = Mathf.Sin(angleRad) * _attractor.Radius;

            _lineRenderer.SetPosition(i, new Vector3(x, _heightOffset, z));
        }
    }
}