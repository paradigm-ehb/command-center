using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using static System.Net.WebRequestMethods;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace paradigm_ehb.CommandCenter.WinUI.Components.Reusable;

public sealed partial class Home_ServerOverview : UserControl
{
    public Home_ServerOverview()
    {
        InitializeComponent();

        rootBorder.PointerEntered += (s, e) =>
        {
            VisualStateManager.GoToState(this, "PointerOver", true); //Starts the animation
        };

        rootBorder.PointerExited += (s, e) =>
        {
            VisualStateManager.GoToState(this, "Normal", true); //Ends the animation
        };
    }

    private void setupStatus(int Status)
    {
        switch(Status)
        {
            case 0:
                SetText(Windows.UI.Color.FromArgb(255, 184, 6, 6), "Offline");
                break;
            case 1:
                SetText(Windows.UI.Color.FromArgb(255, 255, 111, 0), "Degraded");
                break;
            case 2:
                SetText(Windows.UI.Color.FromArgb(255, 138, 138, 138), "Unknown");
                break;
            case 3:
                SetText(Windows.UI.Color.FromArgb(255, 105, 168, 54), "Online");
                break;
        }
    }

    private void SetText(Windows.UI.Color kleur, String text)
    {
        StatusColor.Fill = new SolidColorBrush(kleur);
        StatusText.Text = text;
    }

    public string ServerName
    {
        get => (string)GetValue(ServerNameProperty);
        set => SetValue(ServerNameProperty, value);
    }

    public int ServerStatus
    {
        get => (int)GetValue(ServerStatusProperty);
        set => SetValue(ServerStatusProperty, value);
    }

    public static readonly DependencyProperty ServerNameProperty = 
        DependencyProperty.Register(
            nameof(ServerName),          // Property name
            typeof(string),              // Property Datatype
            typeof(Home_ServerOverview), // Coming from...
            new PropertyMetadata(null)); // Default Value

    public static readonly DependencyProperty ServerStatusProperty =
    DependencyProperty.Register(
        nameof(ServerStatus),
        typeof(int),
        typeof(Home_ServerOverview),
        new PropertyMetadata(0, OnServerStatusChanged));

    private static void OnServerStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (Home_ServerOverview)d;
        control.setupStatus((int)e.NewValue);
    }
}
