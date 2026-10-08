using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace UI
{
    public sealed class UiSpawner
    {
        private readonly IObjectResolver _resolver;
        private readonly Dictionary<UiLayer, int> _nextOrders = new Dictionary<UiLayer, int>();

        public UiSpawner(IObjectResolver resolver)
        {
            if (resolver == null)
            {
                throw new InvalidOperationException(
                    "UiSpawner: IObjectResolver was not injected. Check that the scene LifetimeScope registers UiSpawner.");
            }

            _resolver = resolver;
        }

        public T Spawn<T>(T prefab, UiLayer layer, Action onClosed = null) where T : Component, IShowable
        {
            if (prefab == null)
            {
                throw new ArgumentOutOfRangeException(nameof(prefab),
                    "UiSpawner: the spawned prefab is null. Drag a prefab into the caller's serialized field.");
            }

            T instance = _resolver.Instantiate(prefab);

            ApplyLayer(instance, layer);
            instance.Show();
            SubscribeClosed(instance, onClosed);

            return instance;
        }

        public T Show<T>(T showable, UiLayer layer, Action onClosed = null) where T : Component, IShowable
        {
            if (showable == null)
            {
                throw new ArgumentOutOfRangeException(nameof(showable),
                    "UiSpawner: the shown element is null. Assign the element in the caller's serialized field.");
            }

            ApplyLayer(showable, layer);
            showable.Show();
            SubscribeClosed(showable, onClosed);

            return showable;
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

        public void Hide(IShowable showable)
        {
            if (showable == null)
            {
                throw new ArgumentOutOfRangeException(nameof(showable),
                    "UiSpawner: the hidden element is null.");
            }

            showable.Hide();
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

        private static void SubscribeClosed(Component showable, Action onClosed)
        {
            if (onClosed == null)
            {
                return;
            }

            ClosedRelay relay = showable.gameObject.AddComponent<ClosedRelay>();
            relay.Initialize((IShowable)showable, onClosed);
        }

        private sealed class ClosedRelay : MonoBehaviour
        {
            private IShowable _showable;
            private Action _onClosed;

            public void Initialize(IShowable showable, Action onClosed)
            {
                _showable = showable;
                _onClosed = onClosed;
                _showable.Closed += OnShowableClosed;
            }

            private void OnDestroy()
            {
                if (_showable == null)
                {
                    return;
                }

                _showable.Closed -= OnShowableClosed;
            }

            private void OnShowableClosed()
            {
                _showable.Closed -= OnShowableClosed;
                _onClosed.Invoke();
            }
        }
    }
}
