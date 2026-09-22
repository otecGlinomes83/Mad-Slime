using Game;
using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace UI
{
    public class BaseWindow : MonoBehaviour
    {
        protected Pauser Pauser { get; private set; }

        [Inject]
        public void Construct(Pauser pauser)
        {
            Pauser = pauser;
        }

        public virtual void Initialize()
        {
            if (Pauser == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Pauser was not injected. The window prefab must be instantiated through the DI container (IObjectResolver.Instantiate).");
            }

            Pauser.RequestPause();
        }

        protected virtual void OnDisable()
        {
            Pauser?.RequestResume();
        }
    }
}