using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using paradigm_ehb.CommandCenter.Core.Interfaces;
using paradigm_ehb.CommandCenter.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.ApplicationModel.VoiceCommands;
using Resources.V2;
using System.Runtime.InteropServices.ObjectiveC;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace paradigm_ehb.CommandCenter.WinUI.srvMgnt.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ProcessesPage : Page
    {
        AgentClient? client = null;

        public ObservableCollection<ProcessInfo> processes { get; }

        private Collection<ProcessInfo> allProcesses { get; }

        // track initialization to avoid re-running when page is cached
        private bool _initialized = false;
        private Guid? _lastEndpointId;

        // Cancellation support for initialization
        private CancellationTokenSource? _initCts;

        public ProcessesPage()
        {
            // enable page caching so SelectorBar can reuse cached pages
            this.NavigationCacheMode = NavigationCacheMode.Enabled;

            processes = new ObservableCollection<ProcessInfo>();
            allProcesses = new Collection<ProcessInfo>();

            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Expect an AgentEndpoint parameter
            if (e.Parameter is not AgentEndpoint endpoint)
            {
                _ = ShowErrorInfoBarAsync("Invalid navigation parameter; expected AgentEndpoint.");
                return;
            }

            bool shouldInit = !_initialized || _lastEndpointId != endpoint.Id;

            // Cancel any previous initialization
            _initCts?.Cancel();
            _initCts?.Dispose();
            _initCts = new CancellationTokenSource();
            CancellationToken ct = _initCts.Token;

            // Resolve client from registry (parent is expected to have created/registered it)
            _ = ResolveClientAndMaybeInitAsync(endpoint, shouldInit, ct);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            // Cancel any in-progress initialization when leaving the page
            _initCts?.Cancel();
            _initCts?.Dispose();
            _initCts = null;
        }

        private async Task ResolveClientAndMaybeInitAsync(AgentEndpoint endpoint, bool shouldInit, CancellationToken ct)
        {
            try
            {
                var registry = App.Services.GetRequiredService<IAgentClientRegistry>();
                client = await registry.GetAsync(endpoint.Id).ConfigureAwait(false);

                if (shouldInit)
                {
                    await InitializeAsync(endpoint, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            catch (Exception ex)
            {
                await ShowErrorInfoBarAsync($"Failed to resolve AgentClient: {ex.Message}");
            }
        }

        private void OnFilterChanged(object sender, RoutedEventArgs args)
        {
            IEnumerable<ProcessInfo> filtered = allProcesses.Where(process => Filter(process));
            Remove_NonMatching(filtered);
            AddBack_Processes(filtered);
            Order_processes();
        }

        private bool Filter(ProcessInfo process)
        {
            if (string.IsNullOrWhiteSpace(FilterByName.Text))
            {
                return true;
            }
            return process.ProcessName.Contains(FilterByName.Text, StringComparison.OrdinalIgnoreCase)
                || process.ProcessId.ToString().Contains(FilterByName.Text, StringComparison.OrdinalIgnoreCase);
        }

        private void Remove_NonMatching(IEnumerable<ProcessInfo> filteredData)
        {
            for (int i = processes.Count - 1; i >= 0; i--)
            {
                ProcessInfo process = processes[i];
                if (!filteredData.Contains(process))
                    processes.Remove(process);
            }

        }

        private void AddBack_Processes(IEnumerable<ProcessInfo> filteredData)
        {
            foreach (ProcessInfo process in filteredData)
            {
                if (!processes.Contains(process))
                    processes.Add(process);
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await ClearAllProcesses();
            // Refresh is user-initiated; use non-cancellable token
            await LoadAllProcesses(CancellationToken.None);
            IEnumerable<ProcessInfo> filtered = allProcesses.Where(process => Filter(process));
            Remove_NonMatching(filtered);
            AddBack_Processes(filtered);
            Order_processes();
        }

        private async void ProcessTerminate_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem? menuFlyoutItem = sender as MenuFlyoutItem;
            ProcessInfo? processInfo = menuFlyoutItem?.DataContext as ProcessInfo;

            if (processInfo is null)
            {
                await ShowErrorInfoBarAsync("No process selected to terminate.");
                return;
            }

            ContentDialog dialog = new()
            {
                XamlRoot = this.XamlRoot,
                Title = $"Are you sure you want to Terminate {processInfo.ProcessName}?",
                CloseButtonText = "Cancel",
                PrimaryButtonText = "Terminate",
            };

            ContentDialogResult result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    ProcessActionReply terminateResult = await client!.Resources.ProcessActionAsync(new ProcessActionRequest
                    {
                        Pid = processInfo.ProcessId,
                        Signal = 15 // SIGTERM
                    });
                    if (terminateResult.Succes)
                    {
                        await ShowInfoBarAsync("Process Terminated", $"Process {processInfo.ProcessName} (PID {processInfo.ProcessId}) was terminated successfully.", InfoBarSeverity.Success);
                        await UpdateProcessVisualStateAsync(processInfo, "killed");
                    }
                    else
                    {
                        await ShowErrorInfoBarAsync($"Failed to terminate process {processInfo.ProcessName} (PID {processInfo.ProcessId})!");
                    }
                }
                catch (Exception ex)
                {
                    await ShowErrorInfoBarAsync($"Error terminating process: {ex.Message}");
                }
            }
            else
            {
                // Cancel, do nothing
            }
        }

        private async void ProcessKill_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem? menuFlyoutItem = sender as MenuFlyoutItem;
            ProcessInfo? processInfo = menuFlyoutItem?.DataContext as ProcessInfo;

            if (processInfo is null)
            {
                await ShowErrorInfoBarAsync("No process selected to kill.");
                return;
            }

            ContentDialog dialog = new()
            {
                XamlRoot = this.XamlRoot,
                Title = $"Are you sure you want to Kill {processInfo.ProcessName}?",
                CloseButtonText = "Cancel",
                PrimaryButtonText = "Kill",
            };

            ContentDialogResult result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    ProcessActionReply killResult = await client!.Resources.ProcessActionAsync(new ProcessActionRequest
                    {
                        Pid = processInfo.ProcessId,
                        Signal = 9 // SIGKILL
                    });

                    if (killResult.Succes)
                    {
                        await ShowInfoBarAsync("Process Killed", $"Process {processInfo.ProcessName} (PID {processInfo.ProcessId}) was killed successfully.", InfoBarSeverity.Success);
                        await UpdateProcessVisualStateAsync(processInfo, "killed");
                    }
                    else
                    {
                        await ShowErrorInfoBarAsync($"Failed to kill process {processInfo.ProcessName} (PID {processInfo.ProcessId})!");
                    }

                }
                catch (Exception ex)
                {
                    await ShowErrorInfoBarAsync($"Error killing process: {ex.Message}");
                }
            }
            else
            {
                // Cancel, do nothing
            }
        }

        private async void ProcessReload_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem? menuFlyoutItem = sender as MenuFlyoutItem;
            ProcessInfo? processInfo = menuFlyoutItem?.DataContext as ProcessInfo;

            if (processInfo is null)
            {
                await ShowErrorInfoBarAsync("No process selected to restart.");
                return;
            }

            ContentDialog dialog = new()
            {
                XamlRoot = this.XamlRoot,
                Title = $"Are you sure you want to Restart {processInfo.ProcessName}?",
                CloseButtonText = "Cancel",
                PrimaryButtonText = "Restart",
            };

            ContentDialogResult result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    ProcessActionReply restartResult = await client!.Resources.ProcessActionAsync(new ProcessActionRequest
                    {
                        Pid = processInfo.ProcessId,
                        Signal = 1
                    });
                    if (restartResult.Succes)
                    {
                        await ShowInfoBarAsync("Process Restarted", $"Process {processInfo.ProcessName} (PID {processInfo.ProcessId}) was restarted successfully.", InfoBarSeverity.Success);
                        await UpdateProcessVisualStateAsync(processInfo, "restarted");
                    }
                    else
                    {
                        await ShowErrorInfoBarAsync($"Failed to restart process {processInfo.ProcessName} (PID {processInfo.ProcessId})!");
                    }
                }
                catch (Exception ex)
                {
                    await ShowErrorInfoBarAsync($"Error restarting process: {ex.Message}");
                }
            }
            else
            {
                // Cancel, do nothing
            }
        }

        private async void ProcessPause_Click(object sender, RoutedEventArgs args)
        {
            MenuFlyoutItem? menuFlyoutItem = sender as MenuFlyoutItem;
            ProcessInfo? processInfo = menuFlyoutItem?.DataContext as ProcessInfo;
            if (processInfo is null)
            {
                await ShowErrorInfoBarAsync("No process selected to pause.");
                return;
            }
            ContentDialog dialog = new()
            {
                XamlRoot = this.XamlRoot,
                Title = $"Are you sure you want to Pause {processInfo.ProcessName}?",
                CloseButtonText = "Cancel",
                PrimaryButtonText = "Pause",
            };
            ContentDialogResult result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    ProcessActionReply pauseResult = await client!.Resources.ProcessActionAsync(new ProcessActionRequest
                    {
                        Pid = processInfo.ProcessId,
                        Signal = 19 // SIGSTOP
                    });
                    if (pauseResult.Succes)
                    {
                        await ShowInfoBarAsync("Process Paused", $"Process {processInfo.ProcessName} (PID {processInfo.ProcessId}) was paused successfully.", InfoBarSeverity.Success);
                        await UpdateProcessVisualStateAsync(processInfo, "paused");
                    }
                    else
                    {
                        await ShowErrorInfoBarAsync($"Failed to pause process {processInfo.ProcessName} (PID {processInfo.ProcessId})!");
                    }
                }
                catch (Exception ex)
                {
                    await ShowErrorInfoBarAsync($"Error pausing process: {ex.Message}");
                }
            }
            else
            {
                // Cancel, do nothing
            }
        }

        private async void ProcessResume_Click(object sender, RoutedEventArgs args)
        {
            MenuFlyoutItem? menuFlyoutItem = sender as MenuFlyoutItem;
            ProcessInfo? processInfo = menuFlyoutItem?.DataContext as ProcessInfo;
            if (processInfo is null)
            {
                await ShowErrorInfoBarAsync("No process selected to resume.");
                return;
            }
            ContentDialog dialog = new()
            {
                XamlRoot = this.XamlRoot,
                Title = $"Are you sure you want to Resume {processInfo.ProcessName}?",
                CloseButtonText = "Cancel",
                PrimaryButtonText = "Resume",
            };
            ContentDialogResult result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    ProcessActionReply resumeResult = await client!.Resources.ProcessActionAsync(new ProcessActionRequest
                    {
                        Pid = processInfo.ProcessId,
                        Signal = 18 // SIGCONT
                    });
                    if (resumeResult.Succes)
                    {
                        await ShowInfoBarAsync("Process Resumed", $"Process {processInfo.ProcessName} (PID {processInfo.ProcessId}) was resumed successfully.", InfoBarSeverity.Success);
                        await UpdateProcessVisualStateAsync(processInfo, "resumed");
                    }
                    else
                    {
                        await ShowErrorInfoBarAsync($"Failed to resume process {processInfo.ProcessName} (PID {processInfo.ProcessId})!");
                    }
                }
                catch (Exception ex)
                {
                    await ShowErrorInfoBarAsync($"Error resuming process: {ex.Message}");
                }
            }
            else
            {
                // Cancel, do nothing
            }
        }

        private async void ProcessCall_Click(object sender, RoutedEventArgs args)
        {
            MenuFlyoutItem? menuFlyoutItem = sender as MenuFlyoutItem;
            ProcessInfo? processInfo = menuFlyoutItem?.DataContext as ProcessInfo;
            if (processInfo is null)
            {
                await ShowErrorInfoBarAsync("No process selected to call.");
                return;
            }
            ContentDialog dialog = new()
            {
                XamlRoot = this.XamlRoot,
                Title = $"Are you sure you want to Call {processInfo.ProcessName}?",
                CloseButtonText = "Cancel",
                PrimaryButtonText = "Call",
            };
            ContentDialogResult result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    ProcessActionReply callResult = await client!.Resources.ProcessActionAsync(new ProcessActionRequest
                    {
                        Pid = processInfo.ProcessId,
                        Signal = 10 // SIGUSR1
                    });
                    if (callResult.Succes)
                    {
                        await ShowInfoBarAsync("Process Called", $"Process {processInfo.ProcessName} (PID {processInfo.ProcessId}) was called successfully.", InfoBarSeverity.Success);
                    }
                    else
                    {
                        await ShowErrorInfoBarAsync($"Failed to call process {processInfo.ProcessName} (PID {processInfo.ProcessId})!");
                    }
                }
                catch (Exception ex)
                {
                    await ShowErrorInfoBarAsync($"Error calling process: {ex.Message}");
                }
            }
            else
            {
                // Cancel, do nothing
            }
        }

        private async Task InitializeAsync(AgentEndpoint endpoint, CancellationToken ct)
        {
            try
            {
                await DispatcherQueue.EnqueueAsync(() => LoadingProgressRing.IsActive = true);

                // Clear previous state
                await ClearAllProcesses();

                // If client isn't resolved yet, try to get it now (parent should have registered it)
                if (client is null)
                {
                    IAgentClientRegistry registry = App.Services.GetRequiredService<IAgentClientRegistry>();
                    client = await registry.GetAsync(endpoint.Id).ConfigureAwait(false);
                }

                if (client is null)
                {
                    await ShowErrorInfoBarAsync("No AgentClient available for this server.");
                    return;
                }

                ct.ThrowIfCancellationRequested();

                // Load processes using resolved client
                await LoadAllProcesses(ct);

                // remember endpoint id when initialization successful
                _lastEndpointId = endpoint.Id;

                _initialized = true;
            }
            catch (OperationCanceledException)
            {
                // initialization was cancelled
            }
            catch (Exception ex)
            {
                // best-effort: show error in UI list
                await DispatcherQueue.EnqueueAsync(() =>
                {
                    processes.Clear();
                    processes.Add(new ProcessInfo
                    {
                        ProcessId = 0,
                        ProcessName = ex.Message,
                        State = ProcessState.Unspecified,
                        Uptime = 0,
                        NumThreads = 0
                    });
                }).ConfigureAwait(false);

                await ShowErrorInfoBarAsync($"Initialization failed: {ex.Message}");
            }
            finally
            {
                await DispatcherQueue.EnqueueAsync(() => LoadingProgressRing.IsActive = false);
            }
        }

        private async Task ClearAllProcesses()
        {
            await DispatcherQueue.EnqueueAsync(() =>
            {
                allProcesses.Clear();
                processes.Clear();
            });
        }

        private async Task LoadAllProcesses(CancellationToken ct)
        {
            if (client is null || client.Service is null)
            {
                await ShowErrorInfoBarAsync("No valid AgentClient available.");
                return;
            }

            ct.ThrowIfCancellationRequested();

            GetSystemResourcesResponse? response;
            try
            {
                response = await client.Resources.GetSystemResourcesAsync(request: new GetSystemResourcesRequest(), cancellationToken: ct).ConfigureAwait(false);
            }
            catch (RpcException rpcEx) when (rpcEx.StatusCode == StatusCode.Cancelled || ct.IsCancellationRequested)
            {
                // treated as cancellation
                return;
            }
            catch (Exception ex)
            {
                await ShowErrorInfoBarAsync($"Failed to load processes: {ex.Message}");
                return;
            }

            if (response is null)
            {
                throw new InvalidOperationException("Received null response from ActionAsync");
            }

            foreach (Process? process in response.Resources.Processes)
            {
                await DispatcherQueue.EnqueueAsync(() =>
                {
                    SolidColorBrush brush = process.State switch
                    {
                        ProcessState.Unspecified => (SolidColorBrush)Application.Current.Resources["SystemFillColorNeutralBrush"],
                        ProcessState.Running => (SolidColorBrush)Application.Current.Resources["SystemFillColorAttentionBrush"],
                        ProcessState.Sleeping => (SolidColorBrush)Application.Current.Resources["SystemFillColorSuccessBrush"],
                        ProcessState.Stopped => (SolidColorBrush)Application.Current.Resources["SystemFillColorCriticalBrush"],
                        _ => (SolidColorBrush)Application.Current.Resources["SystemFillColorCriticalBackgroundBrush"],
                    };
                    ProcessInfo processInfo = new()
                    {
                        Fill = brush,
                        ProcessId = process.Pid,
                        ProcessName = process.Name,
                        State = process.State,
                        Uptime = process.Utime,
                        NumThreads = (int)process.NumThreads
                    };
                    allProcesses.Add(processInfo);
                    processes.Add(processInfo);
                });
            }
            await DispatcherQueue.EnqueueAsync(() =>
            {
                Order_processes();
            });
        }

        public void OnOrderChanged(object sender, SelectionChangedEventArgs args)
        {
            Order_processes();
        }

        private void Order_processes()
        {
            string order = OrderByCombo?.SelectedValue as string ?? "State";

            List<ProcessInfo> ordered = order switch
            {
                "Name" => processes.OrderBy(p => p.ProcessName).ThenBy(p => p.ProcessId).ToList(),
                "Pid" => processes.OrderBy(p => p.ProcessId).ToList(),
                "Uptime" => processes.OrderByDescending(p => p.Uptime).ToList(),
                "State" => processes.OrderBy(p => p.State).ThenBy(p => p.ProcessName).ToList(),
                _ => processes
                    .OrderBy(p =>
                    {
                        if (p.State.Equals(ProcessState.Running))
                            return 0;
                        if (p.State.Equals(ProcessState.Sleeping))
                            return 1;
                        return 2;
                    })
                    .ThenBy(p => p.State)
                    .ThenBy(p => p.ProcessName)
                    .ToList()
            };

            processes.Clear();
            foreach (ProcessInfo process in ordered)
            {
                processes.Add(process);
            }
        }
        private async Task UpdateProcessVisualStateAsync(ProcessInfo processInfo, string action)
        {
            await DispatcherQueue.EnqueueAsync(() =>
            {
                ProcessState state = action switch
                {
                    "started" => ProcessState.Running,
                    "killed" => ProcessState.Stopped,
                    "restarted" => ProcessState.Running,
                    _ => processInfo.State
                };

                SolidColorBrush brush = action switch
                {
                    "started" => (SolidColorBrush)Application.Current.Resources["SystemFillColorAttentionBrush"],
                    "killed" => (SolidColorBrush)Application.Current.Resources["SystemFillColorCriticalBrush"],
                    "restarted" => (SolidColorBrush)Application.Current.Resources["SystemFillColorCautionBrush"],
                    _ => processInfo.Fill
                };

                processInfo.Fill = brush;
                processInfo.State = state;
            });
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
    }

    public class ProcessInfo
    {
        // Fill
        public SolidColorBrush Fill { get; set; } = new SolidColorBrush(Microsoft.UI.Colors.Transparent);

        // PID
        public int ProcessId { get; set; }

        // Process display name
        public string ProcessName { get; set; }

        // Process state as defined in the proto (Resources.V1.ProcessState)
        public ProcessState State { get; set; }

        // Uptime / user-time as provided by the agent (proto: utime). Kept as ulong to match proto uint64
        public ulong Uptime { get; set; }

        // Number of threads for the process
        public int NumThreads { get; set; }

        // Existing fields kept for compatibility with UI that may bind to them
        public double CpuUsage { get; set; }
        public long MemoryUsage { get; set; }
    }
}
