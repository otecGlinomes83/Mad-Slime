using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace UI
{
    public class UiSpawner
    {
        private IObjectResolver _resolver;
        private Dictionary<UiLayer, int> _nextOrders = new Dictionary<UiLayer, int>();

        public UiSpawner(IObjectResolver resolver)
        {
            if (resolver == null)
            {
                throw new InvalidOperationException(
                    "UiSpawner: IObjectResolver was not injected. Check that the scene LifetimeScope registers UiSpawner.");
            }

            _resolver = resolver;
        }

        public T Spawn<T>(T prefab, UiLayer layer) where T : Component, IShowable
        {
            if (prefab == null)
            {
                throw new ArgumentOutOfRangeException(nameof(prefab),
                    "UiSpawner: the spawned prefab is null. Drag a prefab into the caller's serialized field.");
            }

            T instance = _resolver.Instantiate(prefab);

            ApplyLayer(instance, layer);

            return instance;
        }

        public void Release(Component window)
        {
            if (window == null)
            {
                throw new ArgumentOutOfRangeException(nameof(window),
                    "UiSpawner.Release: the released window is null.");
            }

            Destroy(window.gameObject);
        }

        public void Show(IShowable showable)
        {
            if (showable == null)
            {
                throw new ArgumentOutOfRangeException(nameof(showable),
                    "UiSpawner: the shown element is null. Assign the element in the caller's serialized field.");
            }

            showable.Show();
        }

        private void ApplyLayer(Component showable, UiLayer layer)
        {
            if (showable.TryGetComponent(out Canvas canvas) == false)
            {
                throw new InvalidOperationException(
                    $"{showable.name}: no Canvas on the shown object root. Add a root Canvas so UiSpawner can apply the {layer} layer.");
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = TakeSortingOrder(layer);
        }

        private int TakeSortingOrder(UiLayer layer)
        {
            if (_nextOrders.TryGetValue(layer, out int nextOrder) == false)
            {
                nextOrder = (int)layer;
            }

            _nextOrders[layer] = nextOrder + 1;

            return nextOrder;
        }

        private void Destroy(GameObject gameObject)
        {
            UnityEngine.Object.Destroy(gameObject);
        }

    }
}
