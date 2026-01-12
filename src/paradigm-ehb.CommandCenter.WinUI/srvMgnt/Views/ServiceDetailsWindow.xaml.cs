using Grpc.Core;
using Journal.V1;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using paradigm_ehb.CommandCenter.Core;
using paradigm_ehb.CommandCenter.Core.Models;
using Services.V3;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Protection.PlayReady;

namespace paradigm_ehb.CommandCenter.WinUI.srvMgnt.Views
{
    /// <summary>
    /// Service details window with non-blocking streaming log reader.
    /// </summary>
    public sealed partial class ServiceDetailsWindow : Window
    {
        public AgentClient Client { get; }
        public ServiceInfo Service { get; }

        private CancellationTokenSource? _cts;
        private Task? _logsTask;

        public ServiceDetailsWindow(AgentClient agentClient, ServiceInfo serviceInfo)
        {
            InitializeComponent();
            Client = agentClient;
            Service = serviceInfo;

            ExtendsContentIntoTitleBar = true;

            // Create a cancellation source and run the streaming reader on a background thread.
            _cts = new CancellationTokenSource();
            _logsTask = Task.Run(() => GetLogsAsync(_cts.Token));

            // Cancel streaming when the window closes
            this.Closed += (_, _) => _cts?.Cancel();
        }

        private async void ServiceStartMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem menuFlyoutItem = (MenuFlyoutItem)sender;
            ServiceInfo serviceInfo = Service;

            UnitActionReply response = await Client!.Service.PerformUnitActionAsync(new UnitActionRequest
            {
                UnitName = serviceInfo.Name,
                Action = UnitActionRequest.Types.UnitAction.Start
            });

            if (response.Success)
            {
                await ShowInfoBarAsync("Service Action Result", $"Successfully started the service!", InfoBarSeverity.Success);
                await UpdateServiceVisualStateAsync(serviceInfo, "started");
                await Task.Delay(500); // brief delay to allow logs to populate
                await GetLogsAsync(CancellationToken.None);
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
                XamlRoot = this.Content.XamlRoot,
                Title = $"Are you sure you want to Stop {serviceInfo.Name}?",
                CloseButtonText = "Cancel",
                PrimaryButtonText = "Stop",
            };

            ContentDialogResult result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                UnitActionReply response = await Client!.Service.PerformUnitActionAsync(request: new UnitActionRequest
                {
                    UnitName = serviceInfo.Name,
                    Action = UnitActionRequest.Types.UnitAction.Stop
                });

                if (response.Success)
                {
                    await ShowInfoBarAsync("Service Action Result", $"Successfully stopped the service!", InfoBarSeverity.Success);
                    await UpdateServiceVisualStateAsync(serviceInfo, "stopped");
                    await Task.Delay(500); // brief delay to allow logs to populate
                    await GetLogsAsync(CancellationToken.None);
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

            ServiceInfo serviceInfo = Service;

            ContentDialog dialog = new()
            {
                XamlRoot = this.Content.XamlRoot,
                Title = $"Are you sure you want to Restart {serviceInfo.Name}?",
                CloseButtonText = "Cancel",
                PrimaryButtonText = "Restart",
            };

            ContentDialogResult result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    UnitActionReply response = await Client!.Service.PerformUnitActionAsync(new UnitActionRequest
                    {
                        UnitName = serviceInfo.Name,
                        Action = UnitActionRequest.Types.UnitAction.Restart
                    });

                    if (response.Success)
                    {
                        await ShowInfoBarAsync("Service Action Result", $"Successfully restarted the service!", InfoBarSeverity.Success);
                        await UpdateServiceVisualStateAsync(serviceInfo, "restarted");
                        await Task.Delay(500); // brief delay to allow logs to populate
                        await GetLogsAsync(CancellationToken.None);
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
        }

        private async void ServiceEnableMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem menuFlyoutItem = (MenuFlyoutItem)sender;
            ServiceInfo serviceInfo = Service;

            UnitFileActionReply response = await Client!.Service.PerformUnitFileActionAsync(new UnitFileActionRequest
            {
                UnitName = serviceInfo.Name,
                Action = UnitFileActionRequest.Types.UnitFileAction.Enable
            });

            if (response.Success)
            {
                await ShowInfoBarAsync("Service Action Result", $"Successfully enabled the service!", InfoBarSeverity.Success);
                await UpdateServiceVisualStateAsync(serviceInfo, "enabled");
                await Task.Delay(500); // brief delay to allow logs to populate
                await GetLogsAsync(CancellationToken.None);
            }
            else
            {
                await ShowErrorInfoBarAsync(response.ErrorMessage ?? "Unknown error");
            }
        }

        private async void ServiceDisableMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem menuFlyoutItem = (MenuFlyoutItem)sender;
            ServiceInfo serviceInfo = Service;

            UnitFileActionReply response = await Client!.Service.PerformUnitFileActionAsync(new UnitFileActionRequest
            {
                UnitName = serviceInfo.Name,
                Action = UnitFileActionRequest.Types.UnitFileAction.Disable
            });

            if (response.Success)
            {
                await ShowInfoBarAsync("Service Action Result", $"Successfully disabled the service!", InfoBarSeverity.Success);
                await UpdateServiceVisualStateAsync(serviceInfo, "disabled");
                await Task.Delay(500); // brief delay to allow logs to populate
                await GetLogsAsync(CancellationToken.None);
            }
            else
            {
                await ShowErrorInfoBarAsync(response.ErrorMessage ?? "Unknown error");
            }
        }

        private async Task GetLogsAsync(CancellationToken cancellationToken)
        {
            if (Client == null) return;

            AsyncServerStreamingCall<JournalChunk>? call = null;

            try
            {
                call = Client.Journal.Action(new Journal.V1.JournalRequest()
                {
                    NumFromTail = 50,
                    Field = Journal.V1.JournalRequest.Types.Field.Systemd,
                    Value = Service.Name
                });

                var sb = new StringBuilder();
                int bufferedItems = 0;
                const int FlushThreshold = 10;

                // Read on background thread; cancellationToken is passed into ReadAllAsync so it stops promptly.
                await foreach (JournalChunk? response in call.ResponseStream.ReadAllAsync(cancellationToken))
                {
                    if (response is null) continue;

                    sb.AppendLine(response.Reply.ToStringUtf8());
                    bufferedItems++;

                    // Flush in batches to avoid UI churn
                    if (bufferedItems >= FlushThreshold)
                    {
                        string batch = sb.ToString();
                        sb.Clear();
                        bufferedItems = 0;

                        // Fast UI update without awaiting; TryEnqueue is synchronous and cheap.
                        _ = DispatcherQueue.TryEnqueue(() =>
                        {
                            ServiceLogs.Text += batch;
                        });
                    }

                    if (cancellationToken.IsCancellationRequested) break;
                }

                // flush remaining
                if (sb.Length > 0 && !cancellationToken.IsCancellationRequested)
                {
                    string rest = sb.ToString();
                    _ = DispatcherQueue.TryEnqueue(() =>
                    {
                        ServiceLogs.Text += rest;
                    });
                }
            }
            catch (OperationCanceledException)
            {
                // expected on close/cancel - ignore
            }
            catch (Exception ex)
            {
                // Surface a concise error message to the UI
                try
                {
                    _ = DispatcherQueue.TryEnqueue(() =>
                    {
                        ServiceLogs.Text += $"[Logs error] {ex.Message}{Environment.NewLine}";
                    });
                }
                catch
                {
                    // swallow any UI-thread issues
                }
            }
            finally
            {
                try
                {
                    call?.Dispose();
                }
                catch { /* best-effort cleanup */ }

                // If no logs were produced, show a centered placeholder message.
                try
                {
                    _ = DispatcherQueue.TryEnqueue(() =>
                    {
                        if (string.IsNullOrWhiteSpace(ServiceLogs.Text))
                        {
                            ServiceLogs.Text = "No logs available";
                            // Center the message within the control/window as best-effort.
                            ServiceLogs.TextAlignment = global::Microsoft.UI.Xaml.TextAlignment.Center;
                            ServiceLogs.HorizontalAlignment = global::Microsoft.UI.Xaml.HorizontalAlignment.Center;
                            ServiceLogs.VerticalAlignment = global::Microsoft.UI.Xaml.VerticalAlignment.Center;
                        }
                    });
                }
                catch
                {
                    // swallow any UI-thread issues
                }
            }
        }

        private async Task UpdateServiceVisualStateAsync(ServiceInfo serviceInfo, string action)
        {
            await DispatcherQueue.EnqueueAsync(() =>
            {
                switch (action)
                {
                    case "enabled":
                        serviceInfo.State = "enabled";
                        serviceInfo.StateFill = (SolidColorBrush)Application.Current.Resources["SystemFillColorAttentionBrush"];
                        break;
                    case "disabled":
                        serviceInfo.State = "disabled";
                        serviceInfo.StateFill = (SolidColorBrush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                        break;
                    case "started":
                        serviceInfo.ActiveState = "started";
                        serviceInfo.ActiveStateFill = (SolidColorBrush)Application.Current.Resources["SystemFillColorSuccessBrush"];
                        break;
                    case "restarted":
                        serviceInfo.ActiveState = "restarted";
                        serviceInfo.ActiveStateFill = (SolidColorBrush)Application.Current.Resources["SystemFillColorCautionBrush"];
                        break;
                    case "stopped":
                        serviceInfo.ActiveState = "stopped";
                        serviceInfo.ActiveStateFill = (SolidColorBrush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                        break;
                    default:
                        break;
                }
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
}
