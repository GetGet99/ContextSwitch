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
namespace ContextSwitch;

class MainWindow : Window
{
    WindowApi w;
    public MainWindow()
    {
        InitMain();
        w = WindowApi.FromWindowHandle((nint)AppWindow.Id.Value);
        w.Size = new(550, 450);
        var d = w.CurrentDisplay.WorkingAreaBounds;
        w.Location = new((d.Width - w.Size.Width) / 2 + d.Left, (d.Height - w.Size.Height) / 2 + d.Top);
    }
    void InitMain()
    {
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
                (main = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Spacing = 16,
                    Children = {
                        ContextSwitchLogo(new(0, 0, 0, 50)),
                        new TextBlock { Text = "How long should we do work before switching tasks?", HorizontalAlignment = HorizontalAlignment.Center },
                        (tp = new TimePicker {
                            MinuteIncrement = 5,
                            ClockIdentifier = ClockIdentifiers.TwentyFourHour,
                            SelectedTime = TimeSpan.FromMinutes(25),
                            HorizontalAlignment = HorizontalAlignment.Center,
                        }),
                        (btn = new Button() { Content = "Start Timer" , HorizontalAlignment = HorizontalAlignment.Center }),
                        new TextBlock { Text = "Tip: Hold R-CTRL to show timer", HorizontalAlignment = HorizontalAlignment.Center }
                    }
                })
            }
        };
        Grid.SetRow(main, 1);
        SetTitleBar(titlebar);
        btn.Click += delegate
        {
            if (tp.SelectedTime.HasValue)
                ft.Start(tp.SelectedTime.Value);
            w.Minimize();
        };
        AppWindow.Closing += (_, e) =>
        {
            if (ft.IsTimerRunning)
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
