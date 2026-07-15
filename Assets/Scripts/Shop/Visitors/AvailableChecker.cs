using YG;

namespace Skins
{
    public sealed class AvailableChecker : ISkinVisitor
    {
        public bool Result { get; private set; }

        public void Visit(SkinItem item)
        {
            Result = YG2.saves._openSkins.Contains(item.SkinType);
        }
    }
}