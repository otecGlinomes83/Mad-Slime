using Game;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class WinMenu : MonoBehaviour
    {
        [SerializeField] private TMP_Text _moneyCount;
        [SerializeField] private Button _nextLevelButton;
        [SerializeField] private Button _doubleRewardButton;

        [SerializeField] private Pauser _pauser;

        private Action _requestNextLevelAction;
        private Action _doubleRewardAction;

        public void Initialize(int moneyCount, Pauser pauser, Action nextLevelAction, Action doubleRewardAction)
        {
            _requestNextLevelAction = nextLevelAction;
            _doubleRewardAction = doubleRewardAction;
            _pauser = pauser;

            _nextLevelButton.onClick.AddListener(RequestNextLevel);

            if (_doubleRewardButton != null && doubleRewardAction != null)
            {
                _doubleRewardButton.onClick.AddListener(RequestDoubleReward);
            }
            else if (_doubleRewardButton != null)
            {
                _doubleRewardButton.gameObject.SetActive(false);
            }

            _pauser.RequestPause();

            _moneyCount.text = $"{moneyCount}";
        }

        private void OnDisable()
        {
            _nextLevelButton.onClick.RemoveListener(RequestNextLevel);

            if (_doubleRewardButton != null)
            {
                _doubleRewardButton.onClick.RemoveListener(RequestDoubleReward);
            }

            _pauser.RequestResume();
        }

        private void RequestNextLevel()
        {
            _requestNextLevelAction?.Invoke();
            Close();
        }

        private void RequestDoubleReward()
        {
            _doubleRewardAction?.Invoke();
            _doubleRewardButton.gameObject.SetActive(false);
        }

        private void Close()
        {
            Destroy(gameObject);
        }
    }
}
