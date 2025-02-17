using Windows.UI;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using WinUIEx;
using WinWrapper;
using WinWrapper.Windowing;
using Window = Microsoft.UI.Xaml.Window;
using WindowApi = WinWrapper.Windowing.Window;
using Microsoft.UI.Xaml.Hosting;
using System.Numerics;
using Microsoft.UI.Composition;
using static ContextSwitch.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Get.Data.Properties;
using Get.Data.Helpers;
using System.Threading;
using Windows.Media.Playback;
using ABI.System.Numerics;
using Windows.Media.Core;
using System.IO;
using System.Net.Mime;
using Get.Data.Bindings;

namespace ContextSwitch;

class FloatingTimer : Window
{
    static Timer Timer => Timer.Instane;
    static Keyboard Keyboard => Keyboard.Instane;
    readonly WindowApi w;
    readonly TextBlock tb;
    readonly DispatcherQueueTimer ringtimer;
    readonly SolidColorBrush background = new(Color.FromArgb(255 / 2, 0x20, 0x20, 0x20));
    StackPanel resetTimerText;
    public FloatingTimer()
    {
        Keyboard.WindowsToClose.Add(this);
        Content = new StackPanel
        {
            Opacity = 0,
            CornerRadius = new(8),
            BorderBrush = new SolidColorBrush(Color.FromArgb(255 / 2, 255, 255, 255)),
            BorderThickness = new(1),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Background = background,
            Padding = new(8),
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children =
            {
                new SymbolIcon(Symbol.Clock),
                (tb = new() {
                    FontSize = 22,
                    Text = "00:00",
                    TextLineBounds = TextLineBounds.Tight,
                    VerticalAlignment = VerticalAlignment.Center
                }),
                HStack(center: true, Key($"{Keyboard.HOTKEY_MAIN} + Enter", new(0, 0, right: 5, 0)), Text("Reset Timer", TextLineBounds.Tight))
                .AssignTo(out resetTimerText)
            },
            RequestedTheme = ElementTheme.Dark,
        };
        resetTimerText.Visibility = Visibility.Collapsed;
        ringtimer = DispatcherQueue.CreateTimer();
        ringtimer.Interval = TimeSpan.FromMilliseconds(1000);
        ringtimer.Tick += (_, _) => ToggleRingState();
        w = WindowApi.FromWindowHandle((nint)AppWindow.Id.Value);
        w.SetTopMost();
        w.IsTtileBarVisible = false;
        w[WindowStyles.THICKFRAME] = false;
        w[WindowStyles.Border] = false;
        w[WindowExStyles.TOOLWINDOW] = true;
        // Do not hit-test this window
        w[WindowExStyles.Layered] = true;
        w[WindowExStyles.Transparent] = true;
        // Do not focus this window
        w[WindowExStyles.NOACTIVATE] = true;
        SystemBackdrop = new TransparentTintBackdrop();
        UpdateLocation();
        UpdateSize();
        bool isClosed = false;
        ((StackPanel)Content).Loaded += FloatingTimer_Loaded;
        Timer.TimeRemainingProperty.ApplyAndRegisterForNewValue(timeRemaining);
        Timer.TimerStarting += TimerTimerStarting;
        Keyboard.ToHideProperty.ApplyAndRegisterForNewValue(tohide);
        Closed += delegate
        {
            isClosed = true;
            ((StackPanel)Content).Loaded -= FloatingTimer_Loaded;
            Timer.TimeRemainingProperty.ValueChanged -= timeRemaining;
            Timer.TimerStarting -= TimerTimerStarting;
            Keyboard.ToHideProperty.ValueChanged -= tohide;
            ringtimer.Stop();
        };
        void TimerTimerStarting()
        {
            if (isClosed) return;
            ringtimer.Stop();
            if (ringTimerAbnormalState)
                ToggleRingState();
            var content = (StackPanel)Content;
            resetTimerText.Visibility = Visibility.Collapsed;
            UpdateSize();
            background.Color = Color.FromArgb(255 / 2, 0x20, 0x20, 0x20);
            TimerCallback(Timer.TimeRemaining);
            Content.Opacity = 1;
        }
        void timeRemaining(TimeSpan _, TimeSpan timeRemaining)
        {
            if (isClosed) return;
            TimerCallback(timeRemaining);
        }
        void tohide(DateTime? _, DateTime? _1)
        {
            if (isClosed) return;
            TimerCallback(Timer.TimeRemaining);
        }
    }
    void UpdateLocation()
    {
        var workAreaBounds = Display.FromPoint(default).WorkingAreaBounds;
        w.Location = new(workAreaBounds.Left + 16, workAreaBounds.Top + 16);
    }

    private void FloatingTimer_Loaded(object sender, RoutedEventArgs e)
    {
        var contentVisual = ElementCompositionPreview.GetElementVisual(Content);
        var compositor = contentVisual.Compositor;
        contentVisual.ImplicitAnimations = compositor.CreateImplicitAnimationCollection();
        KeyFrameAnimation animation = compositor.CreateVector2KeyFrameAnimation();
        animation.InsertExpressionKeyFrame(1.0f, "this.FinalValue", null);
        animation.Duration = TimeSpan.FromMilliseconds(1500);
        animation.Target = nameof(contentVisual.Size);
        contentVisual.ImplicitAnimations[nameof(contentVisual.Size)] = animation;
        animation = compositor.CreateScalarKeyFrameAnimation();
        animation.InsertExpressionKeyFrame(1.0f, "this.FinalValue", null);
        animation.Duration = TimeSpan.FromMilliseconds(500);
        animation.Target = nameof(contentVisual.Opacity);
        contentVisual.ImplicitAnimations[nameof(contentVisual.Opacity)] = animation;
    }
    void UpdateSize()
    {
        Content.Measure(new(double.PositiveInfinity, double.PositiveInfinity));
        var desiredSize = Content.DesiredSize;
        w.Size = new((int)Math.Ceiling(desiredSize.Width), (int)Math.Ceiling(desiredSize.Height));
    }
    void TimerCallback(TimeSpan diff)
    {
        DateTime now = DateTime.Now;
        if (diff > TimeSpan.Zero)
        {
            if (player.CurrentState is not MediaPlayerState.Paused)
                player.Pause();
            if (diff > TimeSpan.FromHours(1))
                tb.Text = $"{diff:hh\\:mm}";
            else
                tb.Text = $"{diff:mm\\:ss}";
            if (Keyboard.ToHide.HasValue)
            {
                if (Keyboard.ToHide.Value - now < TimeSpan.Zero)
                    TryToHide();
                else
                    TryToShow();
            }
            else
                TryToShow();
            if (diff < TimeSpan.FromSeconds(6))
            {
                TryToShow();
            }
        }
        else if (Timer.HasStartedOnce)
        {
            TryToShow();
            if (player.CurrentState is MediaPlayerState.Paused)
                player.Play();
            tb.Text = "00:00";
            ToggleRingState();
            ringtimer.Start();
            resetTimerText.Visibility = Visibility.Visible;
            UpdateSize();
        }
    }
    void TryToHide()
    {
        if (Timer.TimeRemaining < TimeSpan.FromSeconds(6))
            // do not hide
            return;
        Content.Opacity = 0;
    }
    void TryToShow()
    {
        if (Content.Opacity == 1)
            // already shown
            return;
        UpdateLocation();
        Content.Opacity = 1;
    }
    bool ringTimerAbnormalState;
    static MediaPlayer player { get; } = new()
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
    void ToggleRingState()
    {
        if (ringTimerAbnormalState)
        {
            background.Color = Color.FromArgb(255 / 2, 0x20, 0x20, 0x20);
        }
        else
        {
            Color c = Colors.Red;
            c.A = 255 / 2;
            background.Color = c;
        }
        ringTimerAbnormalState = !ringTimerAbnormalState;
    }
}
