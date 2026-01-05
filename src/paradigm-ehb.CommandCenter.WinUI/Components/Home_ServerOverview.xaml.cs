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

        // Defer attaching pointer handlers to Loaded so we can re-attach when the control is re-used.
        this.Loaded += Home_ServerOverview_Loaded;
        this.Unloaded += Home_ServerOverview_Unloaded;
    }

    private void Home_ServerOverview_Loaded(object? sender, RoutedEventArgs e)
    {
        // Attach pointer handlers (idempotent — SubscribeToServer defensively avoids double-subscription)
        rootBorder.PointerEntered += RootBorder_PointerEntered;
        rootBorder.PointerExited += RootBorder_PointerExited;

        // If a ServerObject is already set (property set before load), subscribe to its events and update UI.
        if (ServerObject != null)
        {
            SubscribeToServer(ServerObject);
            ServerNameText.Text = ServerObject.DisplayName;
            getServerStatus();
        }
    }

    private void RootBorder_PointerEntered(object? sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        VisualStateManager.GoToState(this, "PointerOver", true); // Starts the animation
    }

    private void RootBorder_PointerExited(object? sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        VisualStateManager.GoToState(this, "Normal", true); // Ends the animation
    }

    private void Home_ServerOverview_Unloaded(object? sender, RoutedEventArgs e)
    {
        // Unsubscribe from any server events to break references that prevent GC
        UnsubscribeFromServer(this.ServerObject);

        // Detach UI event handlers (will be re-attached on Loaded)
        rootBorder.PointerEntered -= RootBorder_PointerEntered;
        rootBorder.PointerExited -= RootBorder_PointerExited;

        // Note: do NOT detach the Unloaded/Loaded handlers themselves here.
        // They must remain attached so the control can re-subscribe when re-loaded.
    }

    private async void getServerStatus()
    {
        if (ServerObject == null)
        {
            setupStatus(2);
            return;
        }

        ServerNameText.Text = ServerObject.DisplayName;

        AgentEndpoint agentEndpoint = ServerObject;

        int status = 2; // Default to Unknown

        switch (agentEndpoint.Reachability)
        {
            case Core.Enums.AgentReachability.Offline:
                status = 0; // Offline
                break;
            case Core.Enums.AgentReachability.Online:
                switch (agentEndpoint.HealthStatus)
                {
                    case Core.Enums.AgentHealth.Healthy:
                        status = 4; // Healthy
                        break;
                    case Core.Enums.AgentHealth.Degraded:
                        status = 1; // Degraded
                        break;
                    case Core.Enums.AgentHealth.Unknown:
                        status = 3; // Online (but health unknown)
                        break;
                }
                break;
            case Core.Enums.AgentReachability.Unknown:
            default:
                status = 2; // Unknown
                break;
        }

        setupStatus(status);
    }

    private void setupStatus(int Status)
    {
        switch(Status)
        {
            case 0:
                SetText(Windows.UI.Color.FromArgb(255, 255, 153, 164), "Offline");
                break;
            case 1:
                SetText(Windows.UI.Color.FromArgb(255, 252, 225, 0), "Degraded");
                break;
            case 2:
                SetText(Windows.UI.Color.FromArgb(255, 154, 154, 154), "Unknown");
                break;
            case 3:
                SetText(Windows.UI.Color.FromArgb(255, 76, 194, 255), "Online");
                break;
            case 4:
                SetText(Windows.UI.Color.FromArgb(255, 108, 203, 95), "Healthy");
                break;
        }
    }

    private void SetText(Windows.UI.Color kleur, String text)
    {
        StatusColor.Fill = new SolidColorBrush(kleur);  // TODO: use ThemeResource!
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
        Home_ServerOverview control = (Home_ServerOverview)d;
        AgentEndpoint? oldServer = e.OldValue as AgentEndpoint;
        AgentEndpoint? newServer = e.NewValue as AgentEndpoint;

        // Unsubscribe from previous server events
        if (oldServer != null)
        {
            control.UnsubscribeFromServer(oldServer);
        }

        // Subscribe to new server events and update UI immediately
        if (newServer != null)
        {
            control.ServerNameText.Text = newServer.DisplayName;
            control.getServerStatus();

            // Subscribe so the control updates when the endpoint reports changes.
            // The AgentEndpoint events may fire from background threads, so handlers marshal to the UI thread.
            control.SubscribeToServer(newServer);
        }
        else
        {
            // Clear UI when null
            control.ServerNameText.Text = string.Empty;
            control.setupStatus(2);
        }
    }

    private void SubscribeToServer(AgentEndpoint server)
    {
        if (server == null) return;

        // Avoid double-subscription
        try
        {
            server.ReachabilityChanged -= OnAgentReachabilityChanged;
            server.HealthStatusChanged -= OnAgentHealthStatusChanged;
        }
        catch
        {
            // defensive - ignore if not subscribed
        }

        server.ReachabilityChanged += OnAgentReachabilityChanged;
        server.HealthStatusChanged += OnAgentHealthStatusChanged;
    }

    private void UnsubscribeFromServer(AgentEndpoint? server)
    {
        if (server == null) return;

        try
        {
            server.ReachabilityChanged -= OnAgentReachabilityChanged;
            server.HealthStatusChanged -= OnAgentHealthStatusChanged;
        }
        catch
        {
            // swallow - defensive
        }
    }

    // Event handlers invoked by AgentEndpoint. Match the signatures used elsewhere in the codebase:
    // (AgentEndpoint sender, ReachabilityChangedEventArgs args)
    private void OnAgentReachabilityChanged(object? sender, ReachabilityChangedEventArgs args)
    {
        // Ensure UI updates run on UI thread
        _ = this.DispatcherQueue.TryEnqueue(() => getServerStatus());
    }

    private void OnAgentHealthStatusChanged(object? sender, HealthStatusChangedEventArgs args)
    {
        // Ensure UI updates run on UI thread
        _ = this.DispatcherQueue.TryEnqueue(() => getServerStatus());
    }

    private void Grid_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        MainWindow.Instance.NavigateToServerPage(typeof(ServerMainPage), ServerObject);
    }
}
