using Game;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Skins
{
    public class Shop : BaseWindow
    {
        [SerializeField] private ShopContent _shopContent;
        [SerializeField] private ShopPanel _shopPanel;
        [SerializeField] private Button _closeButton;

        private GameObject _currentModel;

        public void Initialize(Pauser pauser, Wallet wallet)
        {
            base.Initialize(pauser);

            _closeButton.onClick.AddListener(Close);
            _shopPanel.Initialize(wallet);
            _shopPanel.Show(_shopContent.SkinItems);
            _shopPanel.ViewSelected += OnViewSelected;
        }

        protected override void OnDisable()
        {
            _closeButton?.onClick.RemoveListener(Close);
            _shopPanel.ViewSelected -= OnViewSelected;
            base.OnDisable();
        }

        private void Close()
        {
            Destroy(gameObject);
        }

        private void OnViewSelected(SkinItemView view)
        {
            _currentModel = view.Model;
        }
    }
}