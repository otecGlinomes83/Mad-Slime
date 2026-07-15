using Game;
using UI;
using UnityEngine;
using UnityEngine.UI;
using YG;

namespace Skins
{
    [RequireComponent(typeof(ModelPlacer))]
    [RequireComponent(typeof(LevelTransitor))]
    [RequireComponent(typeof(Wallet))]
    public class Shop : MonoBehaviour
    {
        [SerializeField] private ShopContent _shopContent;
        [SerializeField] private ShopPanel _shopPanel;
        [SerializeField] private Button _closeButton;

        private LevelTransitor _levelTransitor;
        private ModelPlacer _placer;
        private Wallet _wallet;
        private bool _isInitialized;

        private void Awake()
        {
            _wallet = GetComponent<Wallet>();
            _placer = GetComponent<ModelPlacer>();
            _levelTransitor = GetComponent<LevelTransitor>();

            _closeButton.onClick.AddListener(Close);
            _shopPanel.ViewSelected += OnViewSelected;
        }

        private void OnEnable()
        {
            YG2.onGetSDKData += OnSDKDataLoaded;

            if (YG2.isSDKEnabled)
            {
                InitializeShop();
            }
        }

        private void OnDisable()
        {
            YG2.onGetSDKData -= OnSDKDataLoaded;

            _closeButton?.onClick.RemoveListener(Close);
            _shopPanel.ViewSelected -= OnViewSelected;
        }

        private void OnSDKDataLoaded()
        {
            if (_isInitialized)
            {
                return;
            }

            InitializeShop();
        }

        private void InitializeShop()
        {
            _shopPanel.Initialize(_wallet);
            _shopPanel.Show(_shopContent.SkinItems);
            OnViewSelected(_shopPanel.SelectedView);

            _isInitialized = true;
        }

        private void Close()
        {
            string previousScene = YG2.saves.PreviousScene;
            _levelTransitor.LoadScene(previousScene);
        }

        private void OnViewSelected(SkinItemView view)
        {
            if (_placer == null)
            {
                return;
            }

            _placer.SetModel(view.Model);
        }
    }
}
