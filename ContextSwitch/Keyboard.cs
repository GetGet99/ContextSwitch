using System;
using WinWrapper;

namespace ContextSwitch;

class Keyboard
{
    static Timer Timer => Timer.Instane;
    public DateTime? ToHide { get; private set; }
    public void SetToHideToNull()
    {
        ToHide = null;
    }
    public static Keyboard Instane { get; } = new();
    private Keyboard()
    {
        Timer.Instane.TimerStarting += delegate
        {
            ToHide = DateTime.Now + TimeSpan.FromSeconds(5);
        };
        LowLevelKeyboard.KeyPressed += LowLevelKeyboard_KeyPressed;
    }
    public void Initialize()
    {
        // do nothing, making sure constructor is invoked
    }
    bool isCtrlDown = false;
#if UNPKG
    public const string HOTKEY_MAIN = "R-ALT";
    public const WinWrapper.Input.VirtualKey HOTKEY_MAIN_VK = WinWrapper.Input.VirtualKey.RMENU;
#else
    public const string HOTKEY_MAIN = "R-ALT";
    public const WinWrapper.Input.VirtualKey HOTKEY_MAIN_VK = WinWrapper.Input.VirtualKey.RMENU;
    //public const string HOTKEY_MAIN = "R-CTRL";
    //public const WinWrapper.Input.VirtualKey HOTKEY_MAIN_VK = WinWrapper.Input.VirtualKey.RCONTROL;
#endif
    private void LowLevelKeyboard_KeyPressed(KeyboardHookInfo eventDetails, KeyboardState state, ref bool Handled)
    {
        bool isDown = state is KeyboardState.KeyDown or KeyboardState.SystemKeyDown;
        if (eventDetails.KeyCode == HOTKEY_MAIN_VK)
        {
            Handled = true;
            isCtrlDown = isDown;
            if (Timer.IsTimerRunning)
            {
                if (isCtrlDown)
                {
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
                Timer.Adjust(TimeSpan.FromMinutes(1));
            }
        }
        if (isCtrlDown && eventDetails.KeyCode == WinWrapper.Input.VirtualKey.DOWN)
        {
            Handled = true;
            if (isDown && Timer.IsTimerRunning)
            {
                Timer.Adjust(-TimeSpan.FromMinutes(1));
            }
        }
        if (isCtrlDown && eventDetails.KeyCode == WinWrapper.Input.VirtualKey.RETURN)
        {
            Handled = true;
            if (isDown && !Timer.IsTimerRunning)
            {
                Timer.Reset();
            }
        }
        if (isCtrlDown && eventDetails.KeyCode == (WinWrapper.Input.VirtualKey)0xBF)
        {
            Handled = true;
            if (isDown)
            {
                if (Timer.IsTimerRunning)
                {
                    Timer.SetToEnd();
                }
                else
                {
                    Environment.Exit(0);
                }
            }
        }
        if (isCtrlDown && eventDetails.KeyCode == WinWrapper.Input.VirtualKey.LEFT)
        {
            Handled = true;
            if (isDown)
            {
                //Flyout flyout = null!;
                //flyout = new Flyout
                //{
                //    SystemBackdrop = new MicaBackdrop(),
                //    Content = VStack(center: true,
                //        Text("Quick Actions Page Coming Soon!"),
                //        new Button { Content = "Cool!" }.WithCustomCode(x => x.Click += (_, _) => flyout.Hide())
                //    ),
                //    ShouldConstrainToRootBounds = false
                //};
                //flyout.ShowAt(Content, new() { Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft });
            }
        }
    }
}
