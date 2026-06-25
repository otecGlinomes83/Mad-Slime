using Game;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class LeaderboardMenu : BaseWindow
    {
        [SerializeField] private Button _closeButton;

        public override void Initialize(Pauser pauser)
        {
            base.Initialize(pauser);
            _closeButton.onClick.AddListener(Close);
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