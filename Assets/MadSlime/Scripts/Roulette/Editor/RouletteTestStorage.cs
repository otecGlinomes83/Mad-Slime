using System.Collections.Generic;
using Saves;

namespace Roulette.Editor
{
    public class RouletteTestStorage : IBalanceStorage, ISkinStorage, IRouletteStorage
    {
        private int _balance = 1000;
        private List<string> _openSkins = new List<string>();
        private List<string> _showcase = new List<string>();
        private List<long> _adSpins = new List<long>();
        private string _selectedSkin;
        private long _lastFreeSpin;
        private int _skinSpinCount;

        public int Balance => _balance;
        public string SelectedSkinId => _selectedSkin;
        public long LastFreeSpinUnixTime => _lastFreeSpin;
        public int SkinSpinCount => _skinSpinCount;

        public void SetBalance(int balance)
        {
            _balance = balance;
        }

        public bool IsSkinOpen(string id)
        {
            return _openSkins.Contains(id);
        }

        public void OpenSkin(string id)
        {
            _openSkins.Add(id);
        }

        public void SelectSkin(string id)
        {
            _selectedSkin = id;
        }

        public List<string> ReadOpenSkinIds()
        {
            return new List<string>(_openSkins);
        }

        public List<string> ReadShowcaseSkinIds()
        {
            return new List<string>(_showcase);
        }

        public void SetShowcaseSkinIds(IReadOnlyList<string> ids)
        {
            _showcase.Clear();

            for (int i = 0; i < ids.Count; i++)
            {
                _showcase.Add(ids[i]);
            }
        }

        public void SetLastFreeSpinUnixTime(long time)
        {
            _lastFreeSpin = time;
        }

        public int CountAdSpinsSince(long time)
        {
            int count = 0;

            for (int i = 0; i < _adSpins.Count; i++)
            {
                if (_adSpins[i] >= time)
                {
                    count++;
                }
            }

            return count;
        }

        public void PruneAdSpinsBefore(long time)
        {
            for (int i = _adSpins.Count - 1; i >= 0; i--)
            {
                if (_adSpins[i] < time)
                {
                    _adSpins.RemoveAt(i);
                }
            }
        }

        public void RegisterAdSpin(long time)
        {
            _adSpins.Add(time);
        }

        public void SetSkinSpinCount(int count)
        {
            _skinSpinCount = count;
        }
    }
}
