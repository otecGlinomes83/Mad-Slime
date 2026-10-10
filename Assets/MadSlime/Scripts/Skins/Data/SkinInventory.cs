using System;
using Saves;

namespace Skins
{
    public class SkinInventory
    {
        private ISkinStorage _storage;

        public string SelectedSkinId => _storage.SelectedSkinId;

        public SkinInventory(ISkinStorage storage)
        {
            if (storage == null)
            {
                throw new ArgumentNullException(nameof(storage));
            }

            _storage = storage;
        }

        public bool IsOpen(string skinId)
        {
            return _storage.IsSkinOpen(skinId);
        }

        public bool Grant(string skinId)
        {
            if (IsOpen(skinId) == true)
            {
                return false;
            }

            _storage.OpenSkin(skinId);
            return true;
        }

        public bool Select(string skinId)
        {
            if (IsOpen(skinId) == false)
            {
                return false;
            }

            if (SelectedSkinId != skinId)
            {
                _storage.SelectSkin(skinId);
            }

            return true;
        }
    }
}
