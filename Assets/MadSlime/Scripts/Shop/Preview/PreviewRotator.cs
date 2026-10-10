using UnityEngine;

namespace Shop
{
    public class PreviewRotator : MonoBehaviour
    {
        [SerializeField] private float _rotationSpeed = 45f;

        private Transform _model;
        private Vector3 _anchor;

        public void Setup(Transform model, Vector3 anchor)
        {
            _model = model;
            _anchor = anchor;
        }

        private void Update()
        {
            if (_model == null)
            {
                return;
            }

            _model.RotateAround(_anchor, Vector3.up, _rotationSpeed * Time.unscaledDeltaTime);
        }
    }
}
