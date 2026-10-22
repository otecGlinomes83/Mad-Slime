using UnityEngine;

namespace UI
{
    internal class MenuPopupInputState
    {
        private CanvasGroup _group;
        private bool _isInteractable;
        private bool _isBlockingRaycasts;

        public MenuPopupInputState(GameObject root)
        {
            if (root.TryGetComponent(out _group) == false)
            {
                _group = root.AddComponent<CanvasGroup>();
            }

            _isInteractable = _group.interactable;
            _isBlockingRaycasts = _group.blocksRaycasts;
        }

        public void Block()
        {
            _group.interactable = false;
            _group.blocksRaycasts = false;
        }

        public void Restore()
        {
            if (_group != null)
            {
                _group.interactable = _isInteractable;
                _group.blocksRaycasts = _isBlockingRaycasts;
            }
        }
    }
}
