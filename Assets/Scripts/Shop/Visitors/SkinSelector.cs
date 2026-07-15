using YG;

namespace Skins
{
    public sealed class SkinSelector : ISkinVisitor
    {
        public void Visit(SkinItem item)
        {
            YG2.saves.SelectedSkinType = item.SkinType;
            YG2.SaveProgress();
        }
    }
}