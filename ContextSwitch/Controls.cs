using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;

namespace ContextSwitch;

static class Controls
{
    public static Border Key(string key, Thickness? margin = null)
        => new()
        {
            Margin = margin ?? new(4),
            Padding = new(4),
            CornerRadius = new(4),
            BorderThickness = new(1),
            BorderBrush = new SolidColorBrush(Colors.White),
            Child = new TextBlock { Text = key, FontSize = 10, TextLineBounds = TextLineBounds.Tight }
        };
    public static TextBlock Text(string txt, TextLineBounds lineBounds = TextLineBounds.Full)
        => new()
        {
            Text = txt,
            TextLineBounds = lineBounds
        };
    public static StackPanel HStack(bool center = false, params UIElement[] uIElements)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (var element in uIElements)
        {
            if (center && element is FrameworkElement fe)
                fe.VerticalAlignment = VerticalAlignment.Center;
            sp.Children.Add(element);
        }
        return sp;
    }
    public static StackPanel VStack(bool center = false, params UIElement[] uIElements)
    {
        var sp = new StackPanel { Orientation = Orientation.Vertical };
        foreach (var element in uIElements)
        {
            if (center && element is FrameworkElement fe)
                fe.HorizontalAlignment = HorizontalAlignment.Center;
            sp.Children.Add(element);
        }
        return sp;
    }
    public static T WithCustomCode<T>(this T input, Action<T> act)
    {
        act(input);
        return input;
    }
}
