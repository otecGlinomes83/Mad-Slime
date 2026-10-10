using Scriptables;

namespace Audio
{
    public interface IGameSoundPlayer
    {
        void Play(SfxClip clip);

        void Play(SfxClip clip, float throttleSeconds);

        void Play(SfxClip clip, float pitch, float throttleSeconds);

        void StartLoop(SfxClip clip);

        void StopLoop();
    }
}
