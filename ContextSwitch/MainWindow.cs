using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Globalization;
using WinUIEx;
using WindowApi = WinWrapper.Windowing.Window;
using static ContextSwitch.Controls;
using Windows.Storage;
using System.Reflection;
using System.IO;
using CommunityToolkit.Helpers;
using CommunityToolkit.WinUI;
namespace ContextSwitch;

class MainWindow : Window
{
    static Timer Timer => Timer.Instane;
    WindowApi w;
    public MainWindow()
    {
        Keyboard.Instane.WindowsToClose.Add(this);
        InitMain();
        w = WindowApi.FromWindowHandle((nint)AppWindow.Id.Value);
        w.Size = new(550, 450);
        var d = w.CurrentDisplay.WorkingAreaBounds;
        w.Location = new((d.Width - w.Size.Width) / 2 + d.Left, (d.Height - w.Size.Height) / 2 + d.Top);
#if UNPKG
        string path = AppContext.BaseDirectory;
#else
        string path = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
#endif
        w.LargeIcon = WinWrapper.Icon.FromHandle(
            new System.Drawing.Bitmap(Path.Join(path, "Assets", "ContextSwitchIcon.png")).GetHicon()
        );
    }
    void InitMain()
    {
        Timer.Initialize(DispatcherQueue);
        Keyboard.Instane.Initialize();
        FloatingTimer ft = new();
        TimePicker tp;
        ft.Show();
        SystemBackdrop = new MicaBackdrop();
        Button btn;
        Border titlebar;
        StackPanel main;
        Title = "Context Switch";
        ExtendsContentIntoTitleBar = true;
        Content = new Grid
        {
            RowDefinitions =
            {
                new() { Height = new(AppWindow.TitleBar.Height)},
                new()
            },
            Children =
            {
                (titlebar = new()),
                (main = VStack(
                    center: true,
                    ContextSwitchLogo(new(0, 0, 0, 50)),
                    Text("How long should we do work before switching tasks?"),
                    tp = new TimePicker {
                        MinuteIncrement = 5,
                        ClockIdentifier = ClockIdentifiers.TwentyFourHour,
#if UNPKG
                        SelectedTime = TimeSpan.FromMinutes(0),
#else
                        SelectedTime = TimeSpan.FromMinutes(25),
#endif
                    },
                    btn = new Button() { Content = "Start Timer" },
                    HStack(center: true, Text("Tip: Hold"), Key(Keyboard.HOTKEY_MAIN), Text("to show timer"))
                )
                .WithCustomCode(x =>
                {
                    x.VerticalAlignment = VerticalAlignment.Center;
                    x.HorizontalAlignment = HorizontalAlignment.Center;
                    x.Spacing = 16;
                }))
            }
        };
        Grid.SetRow(main, 1);
        SetTitleBar(titlebar);
#if !UNPKG
        tp.SelectedTimeChanged += delegate {
            if (tp.SelectedTime.HasValue) {
                if (tp.SelectedTime.Value == default) {
                    btn.Content = "Stop and close app";
                } else {
                    btn.Content = "Start Timer";
                }
            }
        };
#endif
        btn.Click += delegate
        {
            if (tp.SelectedTime.HasValue)
            {
#if !UNPKG
                if (tp.SelectedTime.Value == default)
                {
                    Close();
                    ft.Close();
                    return;
                }
#endif
                Timer.Start(tp.SelectedTime.Value);
            }
            w.Minimize();
        };
        tp.Loaded += delegate
        {
            if (tp.IsLoaded)
            {
                var hours = tp.FindDescendant<Border>(x => x.Name is "SecondPickerHost");
                var minutes = tp.FindDescendant<Border>(x => x.Name is "ThirdPickerHost");
                var ts2355 = TimeSpan.FromHours(23) + TimeSpan.FromMinutes(55);
                TimeSpan clampTime(TimeSpan value)
                {
                    if (value < TimeSpan.FromMinutes(5))
                        return TimeSpan.FromMinutes(5);
                    else if (value > ts2355)
                        return ts2355;
                    return value;
                }
                int hoursDelta = 0;
                int minutesDelta = 0;
                hours.PointerWheelChanged += (_, e) =>
                {
                    var prop = e.GetCurrentPoint(tp).Properties;
                    if (prop.IsHorizontalMouseWheel)
                        return;
                    hoursDelta += prop.MouseWheelDelta;
                    while (hoursDelta >= 120)
                    {
                        hoursDelta -= 120;
                        tp.SelectedTime = clampTime(tp.SelectedTime!.Value +
                            TimeSpan.FromHours(1));
                    }
                    while (hoursDelta <= -120)
                    {
                        hoursDelta += 120;
                        tp.SelectedTime = clampTime(
                            tp.SelectedTime!.Value -
                            TimeSpan.FromHours(1)
                        );
                    }
                };
                minutes.PointerWheelChanged += (_, e) =>
                {
                    var prop = e.GetCurrentPoint(tp).Properties;
                    if (prop.IsHorizontalMouseWheel)
                        return;
                    minutesDelta += prop.MouseWheelDelta;
                    while (minutesDelta >= 120)
                    {
                        minutesDelta -= 120;
                        tp.SelectedTime = clampTime(
                            tp.SelectedTime!.Value +
                            TimeSpan.FromMinutes(5)
                            );
                    }
                    while (minutesDelta <= -120)
                    {
                        minutesDelta += 120;
                        tp.SelectedTime = clampTime(
                            tp.SelectedTime!.Value -
                            TimeSpan.FromMinutes(5)
                        );
                    }
                };
            }
        };
        AppWindow.Closing += (_, e) =>
        {
            if (Timer.IsTimerRunning)
            {
                e.Cancel = true;
                w.Minimize();
            } else
            {
                ft.Close();
            }
        };
    }

    StackPanel ContextSwitchLogo(Thickness margin = default)
    {
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 16,
            Children =
            {
                new TextBlock { Text = "Context", FontSize = 48, VerticalAlignment = VerticalAlignment.Center },
                new Image { Source = new BitmapImage(new Uri("ms-appx:///Assets/ContextSwitchIcon.png")), Height = 100 },
                new TextBlock { Text = "Switch", FontSize = 48, VerticalAlignment = VerticalAlignment.Center },
            },
            Margin = margin
        };
    }
}
