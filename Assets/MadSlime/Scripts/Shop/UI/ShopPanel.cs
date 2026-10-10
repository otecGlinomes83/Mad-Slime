using System;
using UnityEngine;
using UnityEngine.UI;

namespace Shop
{
    public class ShopPanel : MonoBehaviour
    {
        [SerializeField] private GameObject _upgradesPage;
        [SerializeField] private GameObject _allSkinsPage;
        [SerializeField] private GameObject _roulettePage;
        [SerializeField] private Button _upgradesTabButton;
        [SerializeField] private Button _rouletteTabButton;
        [SerializeField] private Button _allSkinsTabButton;

        public event Action<int> PageChanged;

        private GameObject[] _pages;
        private ShopTabButton[] _tabs;
        private int _selectedIndex = -1;
        private bool _isSubscribed;

        public void Setup()
        {
            if (_pages != null)
            {
                return;
            }

            _pages = new GameObject[] { _upgradesPage, _roulettePage, _allSkinsPage };
            Button[] buttons = new Button[] { _upgradesTabButton, _rouletteTabButton, _allSkinsTabButton };
            _tabs = new ShopTabButton[buttons.Length];

            for (int i = 0; i < buttons.Length; i++)
            {
                if (_pages[i] == null || buttons[i] == null || buttons[i].TryGetComponent(out _tabs[i]) == false)
                {
                    throw new InvalidOperationException($"{name}: shop page and tab references are required.");
                }
            }

            if (isActiveAndEnabled == true)
            {
                Subscribe();
            }
        }

        private void OnEnable()
        {
            if (_pages != null)
            {
                Subscribe();
            }
        }

        private void OnDisable()
        {
            if (_isSubscribed == false)
            {
                return;
            }

            _upgradesTabButton.onClick.RemoveListener(ShowUpgradesTab);
            _rouletteTabButton.onClick.RemoveListener(ShowRouletteTab);
            _allSkinsTabButton.onClick.RemoveListener(ShowAllSkinsTab);
            _isSubscribed = false;
        }

        private void Subscribe()
        {
            if (_isSubscribed == true)
            {
                return;
            }

            _upgradesTabButton.onClick.AddListener(ShowUpgradesTab);
            _rouletteTabButton.onClick.AddListener(ShowRouletteTab);
            _allSkinsTabButton.onClick.AddListener(ShowAllSkinsTab);
            _isSubscribed = true;
        }

        public void ShowUpgradesTab()
        {
            SelectPage(0);
        }

        public void ShowRouletteTab()
        {
            SelectPage(1);
        }

        public void ShowAllSkinsTab()
        {
            SelectPage(2);
        }

        private void SelectPage(int index)
        {
            Setup();

            if (_selectedIndex == index)
            {
                return;
            }

            _selectedIndex = index;

            for (int i = 0; i < _pages.Length; i++)
            {
                _pages[i].SetActive(i == index);
                _tabs[i].SetSelected(i == index);
            }

            PageChanged?.Invoke(index);
        }
    }
}
