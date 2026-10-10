namespace Saves
{
    public interface IAudioStorage
    {
        float MusicVolume { get; }

        float SfxVolume { get; }

        void SetVolumes(float musicVolume, float sfxVolume);
    }
}
