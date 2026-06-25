using Game;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class DeathMenu : MonoBehaviour
    {
        [SerializeField] private Button _reviveButton;
        [SerializeField] private Button _restartButton;

        private Action _reviveAction;
        private Action _restartAction;
        private Pauser _pauser;

        public void Initialize(Pauser pauser, Action reviveAction, Action restartAction)
        {
            _restartButton.onClick.AddListener(RequestRestart);
            _reviveButton.onClick.AddListener(RequestRevive);

            _reviveAction = reviveAction;
            _restartAction = restartAction;

            _pauser = pauser;

            _pauser.RequestPause();
        }

        private void OnDisable()
        {
            _restartButton.onClick.RemoveListener(RequestRestart);
            _reviveButton.onClick.RemoveListener(RequestRevive);

            _pauser.RequestResume();
        }

        private void RequestRevive()
        {
            _reviveAction?.Invoke();
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