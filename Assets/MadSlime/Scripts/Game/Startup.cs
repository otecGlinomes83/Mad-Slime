using System;
using UnityEngine;
using VContainer;

namespace Game
{
    public sealed class Startup : MonoBehaviour
    {
        private GameDirector _gameDirector;

        [Inject]
        public void Construct(GameDirector gameDirector)
        {
            _gameDirector = gameDirector;
        }

        private void Awake()
        {
            if (_gameDirector == null)
            {
                throw new InvalidOperationException(
                    $"{name}: GameDirector was not injected. Check that ProjectLifetimeScope registers GameDirector and MenuLifetimeScope registers the Startup component.");
            }

            _gameDirector.EnsureInitialized();
            Time.timeScale = 1f;
        }
    }
}
