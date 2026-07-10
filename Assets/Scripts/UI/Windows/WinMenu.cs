using Game;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class WinMenu : MonoBehaviour
    {
        [SerializeField] private TMP_Text _moneyCount;
        [SerializeField] private Button _nextLevelButton;
        [SerializeField] private Button _restartButton;

        [SerializeField] private Pauser _pauser;

        private Action _requestNextLevelAction;
        private Action _restartAction;

        public void Initialize(int moneyCount, Pauser pauser, Action nextLevelAction, Action restartAction)
        {
            _restartButton.onClick.AddListener(RequestRestart);
            _nextLevelButton.onClick.AddListener(RequestNextLevel);

            _requestNextLevelAction = nextLevelAction;
            _restartAction = restartAction;

            _pauser = pauser;
            
            _pauser.RequestPause();
            
            _moneyCount.text = $"{moneyCount}";
        }

        private void OnDisable()
        {
            _restartButton.onClick.RemoveListener(RequestRestart);
            _nextLevelButton.onClick.RemoveListener(RequestNextLevel);

            _pauser.RequestResume();
        }

        private void RequestNextLevel()
        {
            _requestNextLevelAction?.Invoke();
            Close();
        }

        private void RequestRestart()
        {
            _restartAction?.Invoke();
            Close();
        }

        private void Close()
        {
            Destroy(gameObject);
        }
    }
}