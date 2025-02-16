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
using static Get.Data.Properties.AutoTyper;
namespace ContextSwitch;
[AutoProperty]
partial class Timer
{
    public static Timer Instane { get; } = new();
    public IReadOnlyProperty<TimeSpan> TimeRemainingProperty { get; }
    IProperty<TimeSpan> TimeRemainingProperty_ = Auto(TimeSpan.Zero);
    public IReadOnlyProperty<bool> IsTimerRunningProperty { get; }
    IProperty<bool> IsTimerRunningProperty_ = Auto(false);
    public event Action UserShow;
    public event Action TimerStarting;
    DateTime endtime;
    private Timer()
    {
        TimeRemainingProperty = TimeRemainingProperty_;
        IsTimerRunningProperty = IsTimerRunningProperty_;
    }
    DispatcherQueueTimer timer;
    public void Initialize(DispatcherQueue dispatcherQueue)
    {
        timer = dispatcherQueue.CreateTimer();
        timer.Interval = TimeSpan.FromMilliseconds(500);
        timer.Tick += (_, _) => TimerCallback();
    }

    TimeSpan resetTimerDuration;
    private void UpdateIsTimerRunning()
    {
        IsTimerRunningProperty_.CurrentValue = endtime > DateTime.Now;
    }
    public void Start(TimeSpan timerDuration, bool setAsResetTimerDuration = true)
    {
        if (setAsResetTimerDuration)
            resetTimerDuration = timerDuration;
        endtime = DateTime.Now + timerDuration;
        TimerCallback();
        UpdateIsTimerRunning();
        if (IsTimerRunning)
            timer.Start();
        TimerStarting();
    }
    public void Adjust(TimeSpan diff)
    {
        UpdateIsTimerRunning();
        if (IsTimerRunning)
        {
            endtime += diff;
            TimerCallback();
        } else
        {
            Start(diff, setAsResetTimerDuration: false);
        }
    }
    public void Reset()
    {
        Start(resetTimerDuration);
    }
    public void SetToEnd()
    {
        UpdateIsTimerRunning();
        endtime = DateTime.Now;
    }
    void TimerCallback()
    {
        UpdateIsTimerRunning();
        DateTime now = DateTime.Now;
        TimeSpan diff = endtime - now;
        if (diff > TimeSpan.Zero)
        {
            TimeRemainingProperty_.CurrentValue = diff;
        }
        else
        {
            TimeRemainingProperty_.CurrentValue = TimeSpan.Zero;
        }
    }
}
