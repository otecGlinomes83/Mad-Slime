using Game;
using UnityEngine;
using VContainer;

namespace Skills
{
    public sealed class SkillUnlocker : MonoBehaviour
    {
        private PlayerProgress _progress;

        [Inject]
        public void Construct(PlayerProgress progress)
        {
            _progress = progress;
        }

        public bool IsUnlocked(SkillConfig config)
        {
            if (_progress == null)
            {
                return false;
            }

            return config.RequiredLevel <= _progress.CurrentLevel;
        }
    }
}
