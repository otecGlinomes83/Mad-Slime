using Game;
using TMPro;
using UnityEngine;

namespace Shop
{
    public class ShopBalanceView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _moneyText;

        private Wallet _wallet;

        public void Setup(Wallet wallet)
        {
            Unsubscribe();
            _wallet = wallet;

            if (isActiveAndEnabled == true)
            {
                Subscribe();
            }
        }

        private void OnEnable()
        {
            if (_wallet != null)
            {
                Subscribe();
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            _wallet.BalanceChanged += OnBalanceChanged;
            OnBalanceChanged(_wallet.Balance, _wallet.Balance);
        }

        private void Unsubscribe()
        {
            if (_wallet != null)
            {
                _wallet.BalanceChanged -= OnBalanceChanged;
            }
        }

        private void OnBalanceChanged(int previousBalance, int currentBalance)
        {
            _moneyText.text = currentBalance.ToString();
        }
    }
}
