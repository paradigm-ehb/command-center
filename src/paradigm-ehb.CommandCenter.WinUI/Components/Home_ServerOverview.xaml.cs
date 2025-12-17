using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using paradigm_ehb.CommandCenter.Core.Interfaces;
using paradigm_ehb.CommandCenter.Core.Models;
using paradigm_ehb.CommandCenter.WinUI.srvMgnt;
using System;
using System.Diagnostics;

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

    private async void getServerStatus()
    {
        IServiceProvider services = App.Services;

        //Dependency injection to get factories
        IAgentEndpointFactory agentEndpointFactory = services.GetRequiredService<IAgentEndpointFactory>();
        IAgentClientFactory agentClientFactory = services.GetRequiredService<IAgentClientFactory>();

        ServerNameText.Text = ServerObject.DisplayName;

        AgentEndpoint agentEndpoint = ServerObject;
        AgentClient agent = await agentClientFactory.CreateClientAsync(agentEndpoint);
        try
        {
            HelloReply reply = await agent.Greeter.SayHelloAsync(new HelloRequest { Name = "Command Center" });
            Debug.WriteLine("Greeting: " + reply.Message);
        }
        catch (Grpc.Core.RpcException ex)
        {
            setupStatus(2);
        }
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

    public AgentEndpoint ServerObject
    {
        get => (AgentEndpoint)GetValue(ServerObjProperty);
        set => SetValue(ServerObjProperty, value);
    }


    public static readonly DependencyProperty ServerObjProperty =
    DependencyProperty.Register(
        nameof(ServerObject),
        typeof(AgentEndpoint),
        typeof(Home_ServerOverview),
        new PropertyMetadata(null, OnServerStatusChanged));

    private static void OnServerStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (Home_ServerOverview)d;
        var serverInfo = e.NewValue as AgentEndpoint;

        if (serverInfo != null)
        {
            control.ServerNameText.Text = serverInfo.DisplayName;
            control.getServerStatus();
        }
    }

    private void Grid_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        MainWindow.Instance.NavigateToServerPage(typeof(ServerMainPage), ServerObject);
    }
}
