namespace Saves
{
    public interface ILanguageStorage
    {
        string Language { get; }

        void SetLanguage(string language);
    }
}
