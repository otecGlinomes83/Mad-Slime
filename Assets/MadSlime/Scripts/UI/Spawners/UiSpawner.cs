using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace UI
{
    public sealed class UiSpawner
    {
        private readonly IObjectResolver _resolver;

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
            canvas.sortingOrder = (int)layer;
        }

        private static void SubscribeClosed(IShowable showable, Action onClosed)
        {
            if (onClosed == null)
            {
                return;
            }

            new ClosedRelay(showable, onClosed);
        }

        private sealed class ClosedRelay
        {
            private readonly IShowable _showable;
            private readonly Action _onClosed;

            public ClosedRelay(IShowable showable, Action onClosed)
            {
                _showable = showable;
                _onClosed = onClosed;
                _showable.Closed += OnShowableClosed;
            }

            private void OnShowableClosed()
            {
                _showable.Closed -= OnShowableClosed;
                _onClosed.Invoke();
            }
        }
    }
}
