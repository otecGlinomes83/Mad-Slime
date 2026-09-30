using Game;
using System;
using UI.Animations;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace UI
{
    public class BaseWindow : MonoBehaviour, IShowable
    {
        protected Pauser Pauser { get; private set; }

        public event Action Closed;

        [Inject]
        public void Construct(Pauser pauser)
        {
            Pauser = pauser;
        }

        public void Show()
        {
            if (Pauser == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Pauser was not injected. The window prefab must be shown through the DI container (UiSpawner.Spawn).");
            }

            Pauser.RequestPause();
            UiAnimations.ScaleIn((RectTransform)transform, UiAnimations.WindowScaleInDuration);
        }

        public void Hide()
        {
            CloseAnimated();
        }

        protected void CloseAnimated()
        {
            UiAnimations.ScaleOut((RectTransform)transform, UiAnimations.WindowScaleOutDuration, DestroyWindow);
        }

        protected virtual void OnDisable()
        {
            Pauser?.RequestResume();
        }

        private void DestroyWindow()
        {
            Action closed = Closed;
            closed?.Invoke();

            Destroy(gameObject);
        }
    }
}
