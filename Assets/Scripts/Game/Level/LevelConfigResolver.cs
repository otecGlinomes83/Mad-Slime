using Scriptables;
using System;

namespace Game
{
    public sealed class LevelConfigResolver
    {
        private readonly LevelsCatalog _catalog;

        public LevelConfigResolver(LevelsCatalog catalog)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog),
                    "LevelConfigResolver requires a LevelsCatalog asset.");
            }

            _catalog = catalog;
        }

        public LevelConfig GetConfigFor(int levelNumber)
        {
            for (int i = 0; i < _catalog.Ranges.Count; i++)
            {
                if (levelNumber >= _catalog.Ranges[i].FromLevel && levelNumber <= _catalog.Ranges[i].ToLevel)
                {
                    return _catalog.Ranges[i].Config;
                }
            }

            if (_catalog.Ranges.Count > 0)
            {
                return _catalog.Ranges[_catalog.Ranges.Count - 1].Config;
            }

            throw new InvalidOperationException(
                $"LevelsCatalog '{_catalog.name}' has no ranges assigned. Add at least one LevelRange.");
        }
    }
}
