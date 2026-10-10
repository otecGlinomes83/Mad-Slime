using System.Collections.Generic;

namespace Saves
{
    public interface ISkinStorage
    {
        string SelectedSkinId { get; }

        void SelectSkin(string skinId);

        bool IsSkinOpen(string skinId);

        void OpenSkin(string skinId);

        List<string> ReadOpenSkinIds();

        List<string> ReadShowcaseSkinIds();

        void SetShowcaseSkinIds(IReadOnlyList<string> showcaseSkinIds);
    }
}
