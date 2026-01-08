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
using paradigm_ehb.CommandCenter.Core.Models;
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

namespace paradigm_ehb.CommandCenter.WinUI.srvMgnt.Views
{
    /// <summary>
    /// Service details window with non-blocking streaming log reader.
    /// </summary>
    public sealed partial class ServiceDetailsWindow : Window
    {
        public AgentClient Agent { get; }
        public ServiceInfo Service { get; }

        private CancellationTokenSource? _cts;
        private Task? _logsTask;

        public ServiceDetailsWindow(AgentClient agentClient, ServiceInfo serviceInfo)
        {
            InitializeComponent();
            Agent = agentClient;
            Service = serviceInfo;

            ExtendsContentIntoTitleBar = true;

            // Create a cancellation source and run the streaming reader on a background thread.
            _cts = new CancellationTokenSource();
            _logsTask = Task.Run(() => GetLogsAsync(_cts.Token));

            // Cancel streaming when the window closes
            this.Closed += (_, _) => _cts?.Cancel();
        }

        private async Task GetLogsAsync(CancellationToken cancellationToken)
        {
            if (Agent == null) return;

            AsyncServerStreamingCall<JournalChunk>? call = null;

            try
            {
                call = Agent.Journal.Action(new Journal.V1.JournalRequest()
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
    }
}
