using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VContainer;

namespace ShapeFill
{
    [RequireComponent(typeof(Image))]
    public sealed class FillTapInput : MonoBehaviour, IPointerDownHandler
    {
        private ShapeFiller _filler;

        [Inject]
        public void Construct(ShapeFiller filler)
        {
            _filler = filler;
        }

        private void Awake()
        {
            if (_filler == null)
            {
                throw new InvalidOperationException(
                    $"{name}: ShapeFiller was not injected. Check that FillLifetimeScope registers ShapeFiller and FillTapInput.");
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