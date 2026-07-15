using YG;

namespace Skins
{
    public sealed class SelectedChecker : ISkinVisitor
    {
        public bool Result { get; private set; }

        public void Visit(SkinItem item)
        {
            Result = YG2.saves.SelectedSkinType == item.SkinType;
        }
    }
}