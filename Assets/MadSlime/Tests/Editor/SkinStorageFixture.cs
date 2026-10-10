using System.Collections.Generic;
using Saves;

namespace MadSlime.Tests
{
    public class SkinStorageFixture : ISkinStorage
    {
        private List<string> _open = new List<string>();
        private List<string> _showcase = new List<string>();
        private string _selected;
        private int _openCalls;
        private int _selectionCalls;

        public string SelectedSkinId => _selected;
        public int OpenCalls => _openCalls;
        public int SelectionCalls => _selectionCalls;

        public void SelectSkin(string skinId)
        {
            _selected = skinId;
            _selectionCalls++;
        }

        public bool IsSkinOpen(string skinId)
        {
            return _open.Contains(skinId);
        }

        public void OpenSkin(string skinId)
        {
            _open.Add(skinId);
            _openCalls++;
        }

        public List<string> ReadOpenSkinIds()
        {
            return new List<string>(_open);
        }

        public List<string> ReadShowcaseSkinIds()
        {
            return new List<string>(_showcase);
        }

        public void SetShowcaseSkinIds(IReadOnlyList<string> showcaseSkinIds)
        {
            _showcase.Clear();

            for (int i = 0; i < showcaseSkinIds.Count; i++)
            {
                _showcase.Add(showcaseSkinIds[i]);
            }
        }
    }
}
