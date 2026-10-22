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
        private Transform _parent;

        public UiSpawner(IObjectResolver resolver)
        {
            if (resolver == null)
            {
                throw new InvalidOperationException(
                    "UiSpawner: IObjectResolver was not injected. Check that the scene LifetimeScope registers UiSpawner.");
            }

            _resolver = resolver;
        }

        public void SetParent(Transform parent)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            _parent = parent;
            _parent.gameObject.SetActive(true);
        }

        public T Spawn<T>(T prefab, UiLayer layer) where T : Component, IShowable
        {
            if (prefab == null)
            {
                throw new ArgumentOutOfRangeException(nameof(prefab),
                    "UiSpawner: the spawned prefab is null. Drag a prefab into the caller's serialized field.");
            }

            T instance;

            if (_parent == null)
            {
                instance = _resolver.Instantiate(prefab);
            }
            else
            {
                instance = _resolver.Instantiate(prefab, _parent, false);
            }

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

            window.gameObject.SetActive(false);
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
            canvas.sortingOrder = TakeFreeSortingOrder((int)layer, canvas);
        }

        private int TakeFreeSortingOrder(int startOrder, Canvas ownCanvas)
        {
            Canvas[] canvases = UnityEngine.Object.FindObjectsOfType<Canvas>();
            HashSet<int> occupiedOrders = new HashSet<int>();

            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];

                if (canvas != ownCanvas && canvas.isRootCanvas == true)
                {
                    occupiedOrders.Add(canvas.sortingOrder);
                }
            }

            int order = startOrder;

            while (occupiedOrders.Contains(order) == true)
            {
                order++;
            }

            return order;
        }

        private void Destroy(GameObject gameObject)
        {
            UnityEngine.Object.Destroy(gameObject);
        }

    }
}
