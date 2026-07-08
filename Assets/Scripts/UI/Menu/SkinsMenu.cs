using Game;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class SkinsMenu : BaseWindow
    {
        [SerializeField] private TMP_Text _money;
        [SerializeField] private Button _closeButton;

        private Wallet _wallet;

        public void Initialize(Pauser pauser, Wallet wallet)
        {
            _closeButton.onClick.AddListener(Close);
            base.Initialize(pauser);

            _wallet = wallet;

            _money.text = _wallet.Balance.ToString();
        }

        protected override void OnDisable()
        {
            _closeButton?.onClick.RemoveListener(Close);
            base.OnDisable();
        }

        private void Close()
        {
            Destroy(gameObject);
        }
    }
}