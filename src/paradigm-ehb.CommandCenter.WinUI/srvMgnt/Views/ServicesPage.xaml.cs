using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using paradigm_ehb.CommandCenter.Core.Interfaces;
using paradigm_ehb.CommandCenter.Core.Models;
using Resources.V2;
using Services.V3;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace paradigm_ehb.CommandCenter.WinUI.srvMgnt.Views
{
    public sealed partial class ServicesPage : Page
    {
        AgentClient? client = null;

        // Observable collection used by x:Bind in XAML
        public ObservableCollection<ServiceInfo> services { get; set; }

        private Collection<ServiceInfo> allServices { get; }

        private bool _initialized;
        private Guid? _lastEndpointId;
        private System.Threading.CancellationTokenSource? _initCts;

        public ServicesPage()
        {
            // Explicitly enable page caching so behavior is clear when this page is hosted in a frame
            this.NavigationCacheMode = NavigationCacheMode.Enabled;

            // Initialize collections when the page instance is created so page caching can work correctly
            services = new ObservableCollection<ServiceInfo>();
            allServices = new Collection<ServiceInfo>();

            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Resolve AgentClient quickly (non-blocking) and decide whether we need to init.
            Guid? incomingId = null;
            if (e.Parameter is AgentEndpoint ep) incomingId = ep.Id;

            bool shouldInit = !_initialized || (incomingId.HasValue && incomingId != _lastEndpointId);

            if (shouldInit)
            {
                // Cancel any previous init, create a fresh token
                _initCts?.Cancel();
                _initCts = new System.Threading.CancellationTokenSource();

                // Fire-and-forget; InitializeAsync is cancellable and idempotent
                _ = InitializeAsync(e, _initCts.Token);
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            // Cancel initialization if leaving the page (optional: release resources)
            _initCts?.Cancel();
            _initCts?.Dispose();
            _initCts = null;
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await ClearAllServices();
            await DispatcherQueue.EnqueueAsync(() => LoadingProgressRing.IsActive = true);
            await LoadAllServices();
            await DispatcherQueue.EnqueueAsync(() => LoadingProgressRing.IsActive = false);
        }

        private async void ServiceStartMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem menuFlyoutItem = (MenuFlyoutItem)sender;
            ServiceInfo serviceInfo = (ServiceInfo)menuFlyoutItem.DataContext;

            UnitActionReply response = await client!.Service.PerformUnitActionAsync(new UnitActionRequest
            {
                UnitName = serviceInfo.Name,
                Action = UnitActionRequest.Types.UnitAction.Start
            });

            if (response.Success)
            {
                await ShowInfoBarAsync("Service Action Result", $"Successfully started the service!", InfoBarSeverity.Success);
                await UpdateServiceVisualStateAsync(serviceInfo, "started");
            }
            else
            {
                await ShowErrorInfoBarAsync(response.ErrorMessage ?? "Unknown error");
            }
        }

        private async void ServiceStopMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem menuFlyoutItem = (MenuFlyoutItem)sender;

            ServiceInfo serviceInfo = (ServiceInfo)menuFlyoutItem.DataContext;

            ContentDialog dialog = new()
            {
                XamlRoot = this.XamlRoot,
                Title = $"Are you sure you want to Stop {serviceInfo.Name} ?",
                CloseButtonText = "Cancel",
                PrimaryButtonText = "Kill",
            };

            ContentDialogResult result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                UnitActionReply response = await client!.Service.PerformUnitActionAsync(request: new UnitActionRequest
                {
                    UnitName = serviceInfo.Name,
                    Action = UnitActionRequest.Types.UnitAction.Stop
                });

                if (response.Success)
                {
                    await ShowInfoBarAsync("Service Action Result", $"Successfully stopped the service!", InfoBarSeverity.Success);
                    await UpdateServiceVisualStateAsync(serviceInfo, "stopped");
                }
                else
                {
                    await ShowErrorInfoBarAsync(response.ErrorMessage ?? "Unknown error");
                }
            }
            else
            {
                // Cancel, do nothing
            }

        }

        private async void ServiceRestartMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem menuFlyoutItem = (MenuFlyoutItem)sender;

            ServiceInfo serviceInfo = (ServiceInfo)menuFlyoutItem.DataContext;

            try
            {
                UnitActionReply response = await client!.Service.PerformUnitActionAsync(new UnitActionRequest
                {
                    UnitName = serviceInfo.Name,
                    Action = UnitActionRequest.Types.UnitAction.Restart
                });

                if (response.Success)
                {
                    await ShowInfoBarAsync("Service Action Result", $"Successfully restarted the service!", InfoBarSeverity.Success);
                    await UpdateServiceVisualStateAsync(serviceInfo, "restarted");
                }
                else
                {
                    await ShowErrorInfoBarAsync(response.ErrorMessage ?? "Unknown error");
                }
            }
            catch (Exception ex)
            {
                if (ex is Grpc.Core.RpcException && serviceInfo.Name == "agent.service")
                {
                    await ShowInfoBarAsync("Service Action Result", $"Successfully restarted the Agent!", InfoBarSeverity.Success);
                }
                else
                {
                    await ShowErrorInfoBarAsync($"Exception during restart: {ex.Message}");
                }
            }
        }

        private async void ServiceEnableMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem menuFlyoutItem = (MenuFlyoutItem)sender;
            ServiceInfo serviceInfo = (ServiceInfo)menuFlyoutItem.DataContext;

            UnitFileActionReply response = await client!.Service.PerformUnitFileActionAsync(new UnitFileActionRequest
            {
                UnitName = serviceInfo.Name,
                Action = UnitFileActionRequest.Types.UnitFileAction.Enable
            });

            if (response.Success)
            {
                await ShowInfoBarAsync("Service Action Result", $"Successfully enabled the service!", InfoBarSeverity.Success);
                await UpdateServiceVisualStateAsync(serviceInfo, "enabled");
            }
            else
            {
                await ShowErrorInfoBarAsync(response.ErrorMessage ?? "Unknown error");
            }
        }

        private async void ServiceDisableMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem menuFlyoutItem = (MenuFlyoutItem)sender;
            ServiceInfo serviceInfo = (ServiceInfo)menuFlyoutItem.DataContext;

            UnitFileActionReply response = await client!.Service.PerformUnitFileActionAsync(new UnitFileActionRequest
            {
                UnitName = serviceInfo.Name,
                Action = UnitFileActionRequest.Types.UnitFileAction.Disable
            });

            if (response.Success)
            {
                await ShowInfoBarAsync("Service Action Result", $"Successfully disabled the service!", InfoBarSeverity.Success);
                await UpdateServiceVisualStateAsync(serviceInfo, "disabled");
            }
            else
            {
                await ShowErrorInfoBarAsync(response.ErrorMessage ?? "Unknown error");
            }
        }

        private async void ServiceViewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is not ListView listView) return;

                // Find the ListViewItem that was double-clicked
                DependencyObject original = (DependencyObject)e.OriginalSource;
                ListViewItem? container = listView.ContainerFromItem(original) as ListViewItem;

                if (container?.Content is not ServiceInfo serviceInfo)
                {
                    return;
                }

                AgentClient? clientToPass = client;

                if (clientToPass == null)
                {
                    await ShowErrorInfoBarAsync("Unable to open details: no agent endpoint available.");
                    return;
                }

                var detailsWindow = new ServiceDetailsWindow(clientToPass, serviceInfo);
                detailsWindow.Activate();
            }
            catch (Exception ex)
            {
                await ShowErrorInfoBarAsync($"Failed to open service details: {ex.Message}");
            }
        }

        // Make InitializeAsync accept a cancellation token and be idempotent
        private async Task InitializeAsync(NavigationEventArgs e, System.Threading.CancellationToken ct)
        {
            try
            {
                await DispatcherQueue.EnqueueAsync(() => LoadingProgressRing.IsActive = true);

                // Clear previous lists (but only if first init or endpoint changed)
                await ClearAllServices();

                // Resolve client
                if (e.Parameter is AgentEndpoint endpoint)
                {
                    var registry = App.Services.GetRequiredService<IAgentClientRegistry>();
                    client = await registry.GetAsync(endpoint.Id).ConfigureAwait(false);

                    if (client is null)
                    {
                        var factory = App.Services.GetRequiredService<IAgentClientFactory>();
                        await factory.CreateAndRegisterClientAsync(endpoint).ConfigureAwait(false);
                        client = await registry.GetAsync(endpoint.Id).ConfigureAwait(false);
                    }

                    if (client is null)
                    {
                        await ShowErrorInfoBarAsync("Unable to resolve AgentClient for the selected server.");
                        return;
                    }

                    _lastEndpointId = endpoint.Id;
                }
                else if (e.Parameter is AgentClient passedClient)
                {
                    client = passedClient;
                    _lastEndpointId = null;
                }
                else
                {
                    await ShowErrorInfoBarAsync("Invalid navigation parameter; expected AgentEndpoint or AgentClient.");
                    return;
                }

                // Early cancel check
                ct.ThrowIfCancellationRequested();

                // Do the real work (also pass ct to any long-running calls)
                await LoadAllServices(ct); // keep LoadAllServices robust for being cancelled if you wire it
                _initialized = true;
            }
            catch (OperationCanceledException)
            {
                // initialization was cancelled - safe to ignore
            }
            catch (Exception ex)
            {
                await DispatcherQueue.EnqueueAsync(() =>
                {
                    services.Clear();
                    services.Add(new ServiceInfo { Name = "(error)", Description = ex.Message, StateFill = new SolidColorBrush(Colors.Red) });
                }).ConfigureAwait(false);

                await ShowErrorInfoBarAsync($"Initialization failed: {ex.Message}");
            }
            finally
            {
                await DispatcherQueue.EnqueueAsync(() => LoadingProgressRing.IsActive = false);
            }
        }

        private async Task ClearAllServices()
        {
            await DispatcherQueue.EnqueueAsync(() =>
            {
                services.Clear();
                allServices.Clear();
            });
        }

        /// <summary>
        /// Asynchronously loads all available services from the connected agent client and updates the internal service
        /// collections.
        /// </summary>
        /// <remarks>This method clears the existing service lists before loading new data. If the agent
        /// client or its service is not available, an error message is displayed and no services are loaded.</remarks>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the agent client returns a null response when requesting the list of services.</exception>
        private async Task LoadAllServices(CancellationToken cancellationToken = default)
        {
            if (client is null || client.Service is null)
            {
                await ShowErrorInfoBarAsync("No valid AgentClient available for refreshing services.");
                return;
            }

            GetUnitsReply? response = await client.Service.GetAllUnitsAsync(request: new GetUnitsRequest(), cancellationToken: cancellationToken);
            GetUnitsReply? loadedResponse = await client.Service.GetLoadedUnitsAsync(request: new GetUnitsRequest(), cancellationToken: cancellationToken);
            if (response is null || loadedResponse is null)
            {
                await ShowErrorInfoBarAsync("Failed to retrieve services: received null response from agent.");
                return;
            }

            IEnumerable<LoadedUnit> units = response.Units;
            IEnumerable<LoadedUnit> loadedUnits = loadedResponse.Units;

            // Returns the union of both, updating LoadState where possible
            units = units.Join(
                loadedUnits,
                unit => ExtractShortUnitName(unit.Name),
                loadedUnit => loadedUnit.Name,
                (unit, loadedUnit) =>
                {
                    if (loadedUnit is not null)
                    {
                        loadedUnit.LoadState = unit.LoadState;
                        return loadedUnit;
                    }
                    return unit;
                });

            foreach (LoadedUnit? unit in units)
            {
                string unitName = ExtractShortUnitName(unit.Name);
                // Choose a color/brush based on unit state
                string state = (unit.LoadState ?? string.Empty).ToLowerInvariant();
                SolidColorBrush brush = state switch
                {
                    "enabled" => (SolidColorBrush)Application.Current.Resources["SystemFillColorAttentionBrush"],
                    "loaded" => (SolidColorBrush)Application.Current.Resources["SystemFillColorAttentionBrush"],
                    "static" => (SolidColorBrush)Application.Current.Resources["SystemFillColorCriticalBrush"],
                    "disabled" => (SolidColorBrush)Application.Current.Resources["SystemFillColorCriticalBrush"],
                    _ => new SolidColorBrush(Colors.Goldenrod)
                };

                SolidColorBrush activeStateFill = unit.ActiveState switch
                {
                    "running" => (SolidColorBrush)Application.Current.Resources["SystemFillColorSuccessBrush"],
                    "exited" => (SolidColorBrush)Application.Current.Resources["SystemFillColorNeutralBrush"],
                    "dead" => (SolidColorBrush)Application.Current.Resources["SystemFillColorNeutralBrush"],
                    "failed" => (SolidColorBrush)Application.Current.Resources["SystemFillColorCriticalBackgroundBrush"],
                    _ => new SolidColorBrush(Colors.Goldenrod)
                };

                ServiceInfo serviceInfo = new ServiceInfo
                {
                    Name = unitName,
                    Description = unit.Description ?? unitName,
                    State = unit.LoadState ?? "Unknown",
                    ActiveState = unit.ActiveState ?? "",
                    StateFill = brush,
                    ActiveStateFill = activeStateFill
                };

                await DispatcherQueue.EnqueueAsync(() =>
                {
                    allServices.Add(serviceInfo);
                    services.Add(serviceInfo);
                });
            }

            Order_Services();
        }

        /// <summary>
        /// Returns only the last path segment from a unit name.
        /// Handles both '/' and '\' as separators and null/empty inputs.
        /// Examples:
        ///  - "/path/to/foo.service" -> "foo.service"
        ///  - "C:\path\to\bar.service" -> "bar.service"
        ///  - "simple.service" -> "simple.service"
        /// </summary>
        private static string ExtractShortUnitName(string? fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return string.Empty;

            int lastSlash = fullName.LastIndexOf('/');
            int lastBackslash = fullName.LastIndexOf('\\');
            int lastSep = Math.Max(lastSlash, lastBackslash);

            return lastSep >= 0 && lastSep < fullName.Length - 1
                ? fullName.Substring(lastSep + 1)
                : fullName;
        }

        private void OnFilterChanged(object sender, TextChangedEventArgs args)
        {
            IEnumerable<ServiceInfo> filtered = allServices.Where(service => Filter(service));
            Remove_NonMatching(filtered);
            AddBack_Services(filtered);
            Order_Services();
        }

        private bool Filter(ServiceInfo serviceInfo)
        {
            return serviceInfo.Name.Contains(FilterByName.Text, StringComparison.InvariantCultureIgnoreCase);
        }

        private void Remove_NonMatching(IEnumerable<ServiceInfo> filteredData)
        {
            for (int i = services.Count - 1; i >= 0; i--)
            {
                ServiceInfo item = services[i];
                if (!filteredData.Contains(item)) services.Remove(item);
            }
        }

        private void AddBack_Services(IEnumerable<ServiceInfo> filteredData)
        {
            foreach (ServiceInfo item in filteredData)
            {
                if (!services.Contains(item)) services.Add(item);
            }
        }

        private async Task UpdateServiceVisualStateAsync(ServiceInfo serviceInfo, string action)
        {
            await DispatcherQueue.EnqueueAsync(() =>
            {
                SolidColorBrush brush = action switch
                {
                    "enabled" => (SolidColorBrush)Application.Current.Resources["SystemFillColorAttentionBrush"],
                    "disabled" => (SolidColorBrush)Application.Current.Resources["SystemFillColorCriticalBrush"],
                    _ => serviceInfo.StateFill
                };
                serviceInfo.StateFill = brush;
            });
        }

        public void OnOrderChanged(object sender, SelectionChangedEventArgs args)
        {
            Order_Services();
        }

        private void Order_Services()
        {
            string order = OrderByCombo?.SelectedValue as string ?? "State";

            List<ServiceInfo> ordered = order switch
            {
                "Name" => services.OrderBy(s => s.Name).ThenBy(s => s.Description).ToList(),
                "State" => services.OrderBy(p => p.State).ThenBy(s => s.Name).ToList(),
                _ => services
                    .OrderBy(s =>
                    {
                        if (!string.IsNullOrEmpty(s.State) && s.State.Contains("enabled", StringComparison.InvariantCultureIgnoreCase))
                            return 0;
                        if (!string.IsNullOrEmpty(s.State) && s.State.Contains("disabled", StringComparison.InvariantCultureIgnoreCase))
                            return 1;
                        return 2;
                    })
                    .ThenBy(s => s.State, StringComparer.InvariantCultureIgnoreCase)
                    .ThenBy(s => s.Name, StringComparer.InvariantCultureIgnoreCase)
                    .ToList()
            };

            services.Clear();
            foreach (ServiceInfo process in ordered)
            {
                services.Add(process);
            }
        }

        private async Task ShowInfoBarAsync(string title, string message, InfoBarSeverity severity)
        {
            // Ensure UI thread
            await DispatcherQueue.EnqueueAsync(() =>
            {
                if (GridRoot == null)
                {
                    // fallback to page XamlRoot; but InfoBar must be in visual tree to be visible
                    // if GridRoot is not available the InfoBar won't be shown persistently.
                    return;
                }

                InfoBar infoBar = new()
                {
                    Title = title,
                    Message = message,
                    Severity = severity,
                    IsOpen = true,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 0, 0, 0)
                };

                // Add to visual tree so it is visible
                GridRoot.Children.Add(infoBar);

                // Remove from visual tree when closed
                void OnClosed(object? s, InfoBarClosedEventArgs args)
                {
                    infoBar.Closed -= OnClosed;
                    if (GridRoot.Children.Contains(infoBar))
                    {
                        GridRoot.Children.Remove(infoBar);
                    }
                }

                infoBar.Closed += OnClosed;
            });
        }

        private async Task ShowErrorInfoBarAsync(string message)
        {
            await ShowInfoBarAsync("Service Action Result", $"Error: {message}", InfoBarSeverity.Error);
        }

        // Double-click handler for the ListView
        private async void ServiceItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            try
            {
                if (sender is FrameworkElement fe && fe.DataContext is ServiceInfo serviceInfo)
                {
                    AgentClient agentClient = client;
                    if (agentClient == null)
                    {
                        await ShowErrorInfoBarAsync("Unable to open details: no agent endpoint available.");
                        return;
                    }

                    await DispatcherQueue.EnqueueAsync(() =>
                    {
                        // Create and show the details window
                        var detailsWindow = new ServiceDetailsWindow(agentClient, serviceInfo);
                        detailsWindow.Activate();
                    });
                }
            }
            catch (Exception ex)
            {
                await ShowErrorInfoBarAsync($"Failed to open service details: {ex.Message}");
            }
        }
    }

    // Small view-model used by the DataTemplate in XAML.
    public sealed class ServiceInfo : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
                }
            }
        }

        private string _description = string.Empty;
        public string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Description)));
                }
            }
        }

        private string _state = string.Empty;
        public string State
        {
            get => _state;
            set
            {
                if (_state != value)
                {
                    _state = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(State)));
                }
            }
        }

        private string _activeState = string.Empty;
        public string ActiveState
        {
            get => _activeState;
            set
            {
                if (_activeState != value)
                {
                    _activeState = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActiveState)));
                }
            }
        }

        private SolidColorBrush _stateFill = new SolidColorBrush(Colors.Transparent);
        public SolidColorBrush StateFill
        {
            get => _stateFill;
            set
            {
                if (_stateFill != value)
                {
                    _stateFill = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StateFill)));
                }
            }
        }

        private SolidColorBrush _activeStateFill = new SolidColorBrush(Colors.Transparent);
        public SolidColorBrush ActiveStateFill
        {
            get => _activeStateFill;
            set
            {
                if (_activeStateFill != value)
                {
                    _activeStateFill = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActiveStateFill)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
