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

namespace ContextSwitch;

class FloatingTimer : Window
{
    readonly WindowApi w;
    DateTime endtime;
    readonly TextBlock tb;
    readonly DispatcherQueueTimer timer, ringtimer;
    readonly SolidColorBrush background = new(Color.FromArgb(255 / 2, 0x20, 0x20, 0x20));
    public FloatingTimer()
    {
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
                })
            },
            RequestedTheme = ElementTheme.Dark,
        };

        timer = DispatcherQueue.CreateTimer();
        ringtimer = DispatcherQueue.CreateTimer();
        timer.Interval = TimeSpan.FromMilliseconds(500);
        ringtimer.Interval = TimeSpan.FromMilliseconds(1000);
        timer.Tick += (_, _) => TimerCallback();
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
        var workAreaBounds = Display.FromPoint(default).WorkingAreaBounds;
        w.Location = new(workAreaBounds.Left + 16, workAreaBounds.Top + 16);
        SystemBackdrop = new TransparentTintBackdrop();
        UpdateSize();
        ((StackPanel)Content).Loaded += FloatingTimer_Loaded;
        LowLevelKeyboard.KeyPressed += LowLevelKeyboard_KeyPressed;
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
    bool isCtrlDown = false;
#if UNPKG
    public const string HOTKEY_MAIN = "R-ALT";
#else
    public const string HOTKEY_MAIN = "R-CTRL";
#endif
    private void LowLevelKeyboard_KeyPressed(KeyboardHookInfo eventDetails, KeyboardState state, ref bool Handled)
    {
        bool isDown = state is KeyboardState.KeyDown or KeyboardState.SystemKeyDown;
#if UNPKG
        // Use RSHIFT for debugging
        if (eventDetails.KeyCode == WinWrapper.Input.VirtualKey.RMENU)
#else
        if (eventDetails.KeyCode == WinWrapper.Input.VirtualKey.RCONTROL)
#endif
        {
            Handled = true;
            isCtrlDown = isDown;
            if (!keyReset)
            {
                if (isCtrlDown)
                {
                    Content.Opacity = 1;
                    ToHide = null;
                }
                else
                    ToHide = DateTime.Now + TimeSpan.FromSeconds(3);
            }
        }
        if (isCtrlDown && eventDetails.KeyCode == WinWrapper.Input.VirtualKey.UP)
        {
            Handled = true;
            if (isDown)
            {
                if (IsTimerRunning)
                {
                    endtime += TimeSpan.FromMinutes(1);
                    TimerCallback();
                }
                else
                    Start(TimeSpan.FromMinutes(1));
            }
        }
        if (isCtrlDown && eventDetails.KeyCode == WinWrapper.Input.VirtualKey.DOWN)
        {
            Handled = true;
            if (isDown && IsTimerRunning)
            {
                endtime -= TimeSpan.FromMinutes(1);
                TimerCallback();
            }
        }
        if (isCtrlDown && eventDetails.KeyCode == WinWrapper.Input.VirtualKey.RETURN)
        {
            Handled = true;
            if (isDown && keyReset)
            {
                Start(resetTimerDuration);
            }
        }
        if (isCtrlDown && eventDetails.KeyCode == WinWrapper.Input.VirtualKey.LEFT)
        {
            Handled = true;
            if (isDown)
            {
                Flyout flyout = null!;
                flyout = new Flyout
                {
                    SystemBackdrop = new MicaBackdrop(),
                    Content = VStack(center: true,
                        Text("Quick Actions Page Coming Soon!"),
                        new Button { Content = "Cool!" }.WithCustomCode(x => x.Click += (_, _) => flyout.Hide())
                    ),
                    ShouldConstrainToRootBounds = false
                };
                flyout.ShowAt(Content, new() { Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft});
            }
        }
    }

    void UpdateSize()
    {
        Content.Measure(new(double.PositiveInfinity, double.PositiveInfinity));
        var desiredSize = Content.DesiredSize;
        w.Size = new((int)Math.Ceiling(desiredSize.Width), (int)Math.Ceiling(desiredSize.Height));
    }
    DateTime? ToHide;
    bool keyReset = false;
    TimeSpan resetTimerDuration;
    public bool IsTimerRunning => endtime > DateTime.Now;
    public void Start(TimeSpan timerDuration)
    {
        ringtimer.Stop();
        if (ringTimerAbnormalState)
            ToggleRingState();
        keyReset = false;
        var content = (StackPanel)Content;
        if (content.Children.Count == 3)
        {
            content.Children.RemoveAt(2);
            UpdateSize();
        }
        resetTimerDuration = timerDuration;
        background.Color = Color.FromArgb(255 / 2, 0x20, 0x20, 0x20);
        endtime = DateTime.Now + timerDuration;
        TimerCallback();
        if (IsTimerRunning)
            timer.Start();
        lockHide = false;
        Content.Opacity = 1;
        ToHide = DateTime.Now + TimeSpan.FromSeconds(5);
    }
    public void Stop()
    {
        endtime = DateTime.Now;
    }
    bool lockHide = false;
    void TimerCallback()
    {
        DateTime now = DateTime.Now;
        TimeSpan diff = endtime - now;
        if (diff > TimeSpan.Zero)
        {
            tb.Text = $"{diff:mm\\:ss}";
            if (ToHide.HasValue)
            {
                if (ToHide.Value - now < TimeSpan.Zero)
                {
                    Content.Opacity = 0;
                    ToHide = null;
                }
            }
            if (diff < TimeSpan.FromSeconds(6))
            {
                lockHide = true;
                Content.Opacity = 1;
            }
        } else
        {
            timer.Stop();
            tb.Text = "00:00";
            ElementSoundPlayer.State = ElementSoundPlayerState.On;
            ToggleRingState();
            ringtimer.Start();
            lockHide = true;
            Content.Opacity = 1;
            keyReset = true;
            ((StackPanel)Content).Children.Add(
                HStack(center: true, Key($"{HOTKEY_MAIN} + Enter", new(0, 0, right: 5, 0)), Text("Reset Timer", TextLineBounds.Tight)));
            UpdateSize();
        }
    }
    bool ringTimerAbnormalState;
    void ToggleRingState()
    {
        if (ringTimerAbnormalState)
        {
            background.Color = Color.FromArgb(255 / 2, 0x20, 0x20, 0x20);
            ElementSoundPlayer.State = ElementSoundPlayerState.Off;
        } else
        {
            Color c = Colors.Red;
            c.A = 255 / 2;
            background.Color = c;
            ElementSoundPlayer.Play(ElementSoundKind.Invoke);
        }
        ringTimerAbnormalState = !ringTimerAbnormalState;
    }
}
