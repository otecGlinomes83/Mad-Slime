using System;

namespace Core
{
    public interface ILanguageProvider
    {
        string Language { get; }

        event Action<string> LanguageSwitched;
    }
}
