using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "NewAttractConfig", menuName = "Mad Slime/Attract Config")]
    public sealed class AttractConfig : ScriptableObject
    {
        [Tooltip("Базовая скорость притяжения предметов магнитом (юнитов/с) в точке первого контакта.")]
        [SerializeField] private float _attractionForce = 6f;

        [Tooltip("Во сколько раз разгоняется притяжение у точки захвата по сравнению с краем радиуса.")]
        [SerializeField] private float _approachMultiplier = 3f;

        [Tooltip("Крутизна кривой разгона. 1 = линейный разгон по всему радиусу, 2+ = разгон сосредоточен ближе к захвату.")]
        [SerializeField] private float _approachPower = 2f;

        [Tooltip("Доля боковой (орбитальной) скорости от радиальной. 0 = предметы летят по прямой, 0.5 = заметная спираль.")]
        [SerializeField] private float _orbitStrength = 0.5f;

        public float AttractionForce => _attractionForce;
        public float ApproachMultiplier => _approachMultiplier;
        public float ApproachPower => _approachPower;
        public float OrbitStrength => _orbitStrength;
    }
}
