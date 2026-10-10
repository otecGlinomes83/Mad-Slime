using System;
using Core;
using YG;

namespace Adapters
{
    public class Yg2LanguageProvider : ILanguageProvider
    {
        public string Language => YG2.lang;

        public event Action<string> LanguageSwitched
        {
            add
            {
                YG2.onSwitchLang += value;
            }
            remove
            {
                YG2.onSwitchLang -= value;
            }
        }
    }
}
