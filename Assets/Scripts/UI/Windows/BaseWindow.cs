using Game;
using System;
using UnityEngine;

namespace UI
{
    public class BaseWindow : MonoBehaviour
    {
        protected Pauser Pauser { get; private set; }

        public virtual void Initialize(Pauser pauser)
        {
            if (pauser == null)
            {
                throw new ArgumentNullException(nameof(pauser));
            }

            Pauser = pauser;
            Pauser.RequestPause();
        }

        protected virtual void OnDisable()
        {
            Pauser?.RequestResume();
        }
    }
}