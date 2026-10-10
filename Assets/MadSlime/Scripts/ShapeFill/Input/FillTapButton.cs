using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VContainer;

namespace ShapeFill
{
    [RequireComponent(typeof(Image))]
    public class FillTapButton : MonoBehaviour, IPointerDownHandler
    {
        private ShapeFillOrchestrator _fillOrchestrator;

        [Inject]
        public void Construct(ShapeFillOrchestrator fillOrchestrator)
        {
            _fillOrchestrator = fillOrchestrator;
        }

        private void Awake()
        {
            if (_fillOrchestrator == null)
            {
                throw new InvalidOperationException(
                    $"{name}: ShapeFillOrchestrator was not injected. Check that FillLifetimeScope registers ShapeFillOrchestrator and FillTapButton.");
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (Time.timeScale <= 0f)
            {
                return;
            }

            _fillOrchestrator.Accelerate();
        }
    }
}
