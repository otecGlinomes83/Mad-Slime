using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShapeFill
{
    [RequireComponent(typeof(Image))]
    public sealed class FillTapInput : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private ShapeFiller _filler;

        private void Awake()
        {
            if (_filler == null)
            {
                throw new InvalidOperationException(
                    $"{name}: ShapeFiller is not assigned. Drag a ShapeFiller component into the _filler field.");
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (Time.timeScale <= 0f)
            {
                return;
            }

            _filler.Accelerate();
        }
    }
}
