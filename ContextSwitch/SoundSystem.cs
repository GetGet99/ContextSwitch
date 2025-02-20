using System.IO;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace ContextSwitch;

class SoundSystem
{
    public static SoundSystem Instance { get; } = new();
    private SoundSystem()
    {

    }
    MediaPlayer player { get; } = new()
    {
        Source = MediaSource.CreateFromStream(
                File.OpenRead(
                    Path.Combine(
                        Windows.ApplicationModel.Package.Current.InstalledLocation.Path,
                        "Assets",
                        "lol.🗿.wav"
                    )
                ).AsRandomAccessStream(),
                "audio/wav"
            ),
        IsLoopingEnabled = true,
    };
    public void Play()
    {
        if (player.CurrentState is MediaPlayerState.Paused)
            player.Play();
    }
    public void Pause()
    {
        if (player.CurrentState is not MediaPlayerState.Paused)
            player.Pause();
    }
}
