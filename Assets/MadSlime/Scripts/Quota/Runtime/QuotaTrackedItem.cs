using Items;
using UnityEngine;

namespace Quota
{
    internal struct QuotaTrackedItem
    {
        private GhostAnimator _animator;
        private Collider _collider;
        private QuotaTypeState _type;

        public GhostAnimator Animator => _animator;
        public Collider Collider => _collider;
        public QuotaTypeState Type => _type;

        public QuotaTrackedItem(GhostAnimator animator, Collider collider, QuotaTypeState type)
        {
            _animator = animator;
            _collider = collider;
            _type = type;
        }
    }
}
