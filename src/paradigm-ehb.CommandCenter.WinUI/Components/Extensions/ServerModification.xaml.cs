using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using paradigm_ehb.CommandCenter.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace paradigm_ehb.CommandCenter.WinUI.srvMgnt.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class ServerModification : Page
{
    public ServerModification()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is AgentEndpoint ip)
        {
            serverObj = ip;
            if (ip.Metadata != null && ip.Metadata.TryGetValue("folder", out var folderValue))
            {
                ServerNameTextBox.Text = folderValue;
            }

        }
    }

    public AgentEndpoint serverObj
    {
        get => (AgentEndpoint)GetValue(serverIP_Property);
        set => SetValue(serverIP_Property, value);
    }

    public static readonly DependencyProperty serverIP_Property =
    DependencyProperty.Register(
        nameof(serverObj),
        typeof(AgentEndpoint),
        typeof(ServerMainPage),
        new PropertyMetadata(null));
}
