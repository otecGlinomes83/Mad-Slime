using UnityEngine;

namespace Roulette
{
    public struct RouletteSectorView
    {
        public string Label;
        public Sprite Icon;
        public Color Color;

        public RouletteSectorView(string label, Sprite icon, Color color)
        {
            Label = label;
            Icon = icon;
            Color = color;
        }
    }
}
