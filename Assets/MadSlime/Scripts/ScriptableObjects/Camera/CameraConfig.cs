using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(menuName = "Mad Slime/Camera Config", fileName = "NewCameraConfig")]
    public class CameraConfig : ScriptableObject
    {
        [Header("Follow")]
        [Tooltip("Время сглаживания движения камеры (с). Меньше = быстрее догоняет игрока.")]
        [SerializeField, Min(0.01f)] private float _positionSmoothTime = 0.3f;

        [Tooltip("Максимальная скорость движения камеры (юнитов/с).")]
        [SerializeField, Min(0.01f)] private float _maxPositionSpeed = 150f;

        [Tooltip("Минимальная дистанция до игрока при подтяжке камеры (юнитов).")]
        [SerializeField, Min(0.01f)] private float _minDistance = 1f;

        [Header("Lens")]
        [Tooltip("Базовое поле зрения камеры (градусы), до добавления эффектов.")]
        [SerializeField, Range(1f, 179f)] private float _baseFov = 60f;

        [Header("Pull")]
        [Tooltip(
            "Кривая: масса съеденного предмета → сила подтяжки камеры к игроку. Ключи перестраивать под реальные массы TierTable.")]
        [SerializeField]
        private AnimationCurve _massToPullStrength = AnimationCurve.Linear(0f, 0.4f, 50f, 2.5f);

        [Tooltip("Потолок подтяжки камеры к игроку (юнитов).")] [SerializeField]
        private float _maxPull = 6f;

        [Tooltip("Разовый отъезд камеры при повышении тира (юнитов).")] [SerializeField]
        private float _tierPushStrength = 4f;

        [Tooltip("Потолок отъезда камеры (юнитов).")] [SerializeField]
        private float _maxPush = 8f;

        [Tooltip("Скорость возврата камеры в обычное состояние. Больше = быстрее.")] [SerializeField]
        private float _recoverSpeed = 4f;

        [Header("Shake")]
        [Tooltip("Частота шума тряски. Больше = быстрее дрожит камера.")]
        [SerializeField, Min(0.1f)] private float _shakeFrequency = 40f;

        [Tooltip("Сила тряски по локальным осям камеры: X — горизонталь, Y — вертикаль. 0 = ось выключена.")]
        [SerializeField, Range(0f, 1f)] private Vector2 _shakeAxes = Vector2.one;

        [Tooltip(
            "Минимальная масса предмета, включающая шейк. Сравнивается с массами TierTable: "
                + "при Small=1/Medium=1000 порог 1000 = трясёт только с крупного тира.")]
        [SerializeField, Min(0f)]
        private float _shakeMassThreshold = 1000f;

        [Tooltip("Добавка тряски за один тяжёлый пикап. Пикапы подряд складываются до Max Shake.")]
        [SerializeField, Min(0f)]
        private float _shakeStrength = 0.15f;

        [Tooltip("Потолок силы тряски. ~0.15 мягко, 0.35+ заметно, 0.8 жёстко.")] [SerializeField, Range(0f, 2f)]
        private float _maxShake = 0.35f;

        [Tooltip("Скорость затухания тряски. 3 = долго гудит, 10 = короткий клайк.")] [SerializeField, Min(0.1f)]
        private float _shakeRecoverSpeed = 7f;

        [Header("FOV Kick")]
        [Tooltip("Тычок поля зрения камеры на тир-апе (градусы). 0 = выключен.")]
        [SerializeField, Min(0f)]
        private float _tierFovKick = 7f;

        [Tooltip("Скорость возврата поля зрения к обычному. Больше = быстрее щелчок.")] [SerializeField, Min(0.1f)]
        private float _fovRecoverSpeed = 5f;

        public float PositionSmoothTime => _positionSmoothTime;
        public float MaxPositionSpeed => _maxPositionSpeed;
        public float MinDistance => _minDistance;
        public float BaseFov => _baseFov;
        public AnimationCurve MassToPullStrength => _massToPullStrength;
        public float MaxPull => _maxPull;
        public float TierPushStrength => _tierPushStrength;
        public float MaxPush => _maxPush;
        public float RecoverSpeed => _recoverSpeed;
        public float ShakeFrequency => _shakeFrequency;
        public Vector2 ShakeAxes => _shakeAxes;
        public float ShakeMassThreshold => _shakeMassThreshold;
        public float ShakeStrength => _shakeStrength;
        public float MaxShake => _maxShake;
        public float ShakeRecoverSpeed => _shakeRecoverSpeed;
        public float TierFovKick => _tierFovKick;
        public float FovRecoverSpeed => _fovRecoverSpeed;
    }
}
