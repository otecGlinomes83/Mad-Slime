using UnityEngine;

namespace UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class MenuLabelFloat : MonoBehaviour
    {
        [Tooltip("Отклонение вверх и вниз от исходной позиции, в единицах Canvas.")]
        [SerializeField, Min(0f)] private float _amplitude = 10f;
        [Tooltip("Длительность полного движения вверх-вниз, в секундах.")]
        [SerializeField, Min(0.1f)] private float _period = 4.5f;
        [SerializeField] private bool _useUnscaledTime = true;

        private RectTransform _rect;
        private Vector2 _restPosition;
        private float _elapsed;

        private void OnEnable()
        {
            _rect = (RectTransform)transform;
            _restPosition = _rect.anchoredPosition;
            _elapsed = 0f;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            if (_useUnscaledTime)
            {
                deltaTime = Time.unscaledDeltaTime;
            }

            _elapsed += deltaTime;
            float offset = Mathf.Sin(_elapsed * (Mathf.PI * 2f / Mathf.Max(0.1f, _period))) * _amplitude;
            _rect.anchoredPosition = _restPosition + Vector2.up * offset;
        }

        private void OnDisable()
        {
            if (_rect != null)
            {
                _rect.anchoredPosition = _restPosition;
            }
        }
    }
}
