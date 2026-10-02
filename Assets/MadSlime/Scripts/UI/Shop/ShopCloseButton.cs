using Cysharp.Threading.Tasks;
using Game;
using System;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Shop
{
    [RequireComponent(typeof(Button))]
    public sealed class ShopCloseButton : MonoBehaviour
    {
        [SerializeField] private ShopPanel _shopPanel;

        private GameDirector _gameDirector;
        private Pauser _pauser;
        private Button _button;

        [Inject]
        public void Construct(GameDirector gameDirector, Pauser pauser)
        {
            _gameDirector = gameDirector;
            _pauser = pauser;
        }

        private void Awake()
        {
            if (_shopPanel == null)
            {
                throw new InvalidOperationException(
                    $"{name}: ShopPanel is not assigned. Drag the ShopPanel component into the _shopPanel field.");
            }

            if (_gameDirector == null)
            {
                throw new InvalidOperationException(
                    $"{name}: GameDirector was not injected. Check that ShopLifetimeScope registers ShopCloseButton.");
            }

            if (_pauser == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Pauser was not injected. Check that ShopLifetimeScope registers the Pauser component and ShopCloseButton.");
            }

            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(OnCloseClicked);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnCloseClicked);
        }

        private void OnCloseClicked()
        {
            if (_shopPanel.IsRouletteSpinning == true || _gameDirector.IsTransitioning == true)
            {
                return;
            }

            NavigateToPrevious().Forget();
        }

        private async UniTaskVoid NavigateToPrevious()
        {
            SceneId targetSceneId = _gameDirector.PreviousSceneId;

            if (targetSceneId == SceneId.Shop)
            {
                targetSceneId = SceneId.Menu;
            }

            _pauser.ResetToPlay();

            await _gameDirector.LoadAsync(targetSceneId);
        }
    }
}
