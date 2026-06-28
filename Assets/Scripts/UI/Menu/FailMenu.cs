using Game;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class FailMenu : MonoBehaviour
    {
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _nextLevelButtonForADS;//тут будет предложение пройти уровень за рекламу

        [SerializeField] private Pauser _pauser;

        private Action _requestNextLevelAction;
        private Action _restartAction;

        public void Initialize(Pauser pauser, Action nextLevelAction, Action restartAction)
        {
            _restartButton.onClick.AddListener(RequestRestart);
            _nextLevelButtonForADS.onClick.AddListener(RequestNextLevel);

            _requestNextLevelAction = nextLevelAction;
            _restartAction = restartAction;

            _pauser = pauser;

            _pauser.RequestPause();
        }

        private void OnDisable()
        {
            _restartButton.onClick.RemoveListener(RequestRestart);
            _nextLevelButtonForADS.onClick.RemoveListener(RequestNextLevel);

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