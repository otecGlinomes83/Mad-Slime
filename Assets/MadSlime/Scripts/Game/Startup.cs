using System;
using UnityEngine;
using VContainer;

namespace Game
{
    public sealed class Startup : MonoBehaviour
    {
        private GameDirector _gameDirector;
        private Pauser _pauser;

        [Inject]
        public void Construct(GameDirector gameDirector, Pauser pauser)
        {
            _gameDirector = gameDirector;
            _pauser = pauser;
        }

        private void Awake()
        {
            if (_gameDirector == null)
            {
                throw new InvalidOperationException(
                    $"{name}: GameDirector was not injected. Check that ProjectLifetimeScope registers GameDirector and MenuLifetimeScope registers the Startup component.");
            }

            if (_pauser == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Pauser was not injected. Check that MenuLifetimeScope registers the Pauser component and Startup.");
            }

            _gameDirector.EnsureInitialized();
            _pauser.ResetToPlay();
        }
    }
}
