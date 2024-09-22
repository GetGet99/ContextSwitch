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


namespace ContextSwitch;

class FloatingTimer : Window
{
    readonly WindowApi w;
    DateTime endtime;
    readonly TextBlock tb;
    readonly DispatcherQueueTimer timer;
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
        timer.Interval = TimeSpan.FromMilliseconds(500);
        timer.Tick += (_, _) => TimerCallback();
        w = WindowApi.FromWindowHandle((nint)AppWindow.Id.Value);
        w.SetTopMost();
        w.IsTtileBarVisible = false;
        w[WindowStyles.THICKFRAME] = false;
        w[WindowStyles.Border] = false;
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

    private void LowLevelKeyboard_KeyPressed(KeyboardHookInfo eventDetails, KeyboardState state, ref bool Handled)
    {
        if (eventDetails.KeyCode == WinWrapper.Input.VirtualKey.RCONTROL)
        {
            bool isDown = state is KeyboardState.KeyDown or KeyboardState.SystemKeyDown;
            if (keyReset)
            {
                if (isDown) Start(resetTimerDuration);
            }
            else
            {
                if (isDown)
                    Content.Opacity = 1;
                else
                    ToHide = DateTime.Now + TimeSpan.FromSeconds(3);
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
        timer.Start();
        lockHide = false;
        Content.Opacity = 1;
        ToHide = DateTime.Now + TimeSpan.FromSeconds(5);
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
            tb.Text = "00:00";
            background.Color = Colors.Red;
            ElementSoundPlayer.State = ElementSoundPlayerState.On;
            ElementSoundPlayer.Play(ElementSoundKind.Invoke);
            ElementSoundPlayer.State = ElementSoundPlayerState.Off;
            Content.Opacity = 1;
            timer.Stop();
            keyReset = true;
            ((StackPanel)Content).Children.Add(new TextBlock {
                FontSize = 11,
                Text = "Press R-CTRL to reset",
                TextLineBounds = TextLineBounds.Tight,
                VerticalAlignment = VerticalAlignment.Center
            });
            UpdateSize();
        }
    }
}
