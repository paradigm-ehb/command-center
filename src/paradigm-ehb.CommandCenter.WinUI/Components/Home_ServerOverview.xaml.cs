using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using paradigm_ehb.CommandCenter.Core.Interfaces;
using paradigm_ehb.CommandCenter.Core.Models;
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

        getServerStatus();
    }

    private async void getServerStatus()
    {
        IServiceProvider services = new ServiceCollection()
                .AddCommandCenterCore()
                .BuildServiceProvider();

        //Dependency injection to get factories
        IAgentEndpointFactory agentEndpointFactory = services.GetRequiredService<IAgentEndpointFactory>();
        IAgentClientFactory agentClientFactory = services.GetRequiredService<IAgentClientFactory>();


        AgentEndpoint agentEndpoint = agentEndpointFactory.Create("127.0.0.1", 50051, false);
        var agent = await agentClientFactory.CreateClientAsync(agentEndpoint);
        try
        {
            var reply = await agent.Greeter.SayHelloAsync(new HelloRequest { Name = "Command Center" });
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
