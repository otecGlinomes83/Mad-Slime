using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(menuName = "Mad Slime/Level Config", fileName = "NewLevelConfig")]
    public sealed class LevelConfig : ScriptableObject
    {
        [SerializeField] private LevelTheme _theme;
        [SerializeField] private LayoutSet _layout;
        [SerializeField] private float _timerDuration = 90f;

        [Header("Quota Generation")]
        [SerializeField] private int _quotaTypesMin = 1;
        [SerializeField] private int _quotaTypesMax = 3;
        [SerializeField] private int _quotaTargetMin = 3;
        [SerializeField] private int _quotaTargetMax = 8;
        [SerializeField] private int _defaultCountDivisor = 4;

        public LevelTheme Theme => _theme;
        public LayoutSet Layout => _layout;
        public float TimerDuration => _timerDuration;
        public int QuotaTypesMin => _quotaTypesMin;
        public int QuotaTypesMax => _quotaTypesMax;
        public int QuotaTargetMin => _quotaTargetMin;
        public int QuotaTargetMax => _quotaTargetMax;
        public int DefaultCountDivisor => _defaultCountDivisor;
    }
}
