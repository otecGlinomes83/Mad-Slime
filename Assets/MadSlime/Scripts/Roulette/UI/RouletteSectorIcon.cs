using UnityEngine;

namespace Roulette
{
    public struct RouletteSectorIcon
    {
        public Sprite Icon;
        public string Label;

        public RouletteSectorIcon(Sprite icon, string label)
        {
            Icon = icon;
            Label = label;
        }
    }
}
