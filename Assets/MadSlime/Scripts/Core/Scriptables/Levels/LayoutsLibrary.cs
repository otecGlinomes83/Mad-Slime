using System.Collections.Generic;
using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(menuName = "Mad Slime/Layouts Library", fileName = "NewLayoutsLibrary")]
    public sealed class LayoutsLibrary : ScriptableObject
    {
        [Tooltip("Библиотека раскладок: при генерации уровень выбирает одну из них.")]
        [SerializeField] private List<LayoutSet> _layouts = new List<LayoutSet>();

        public IReadOnlyList<LayoutSet> Layouts => _layouts;
    }
}
