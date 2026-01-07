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
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace paradigm_ehb.CommandCenter.WinUI.srvMgnt.Views
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ServiceDetailsWindow : Window
    {
        public AgentClient Agent { get; }
        public ServiceInfo Service { get; }

        public ServiceDetailsWindow(AgentClient agentClient, ServiceInfo serviceInfo)
        {
            InitializeComponent();
            Agent = agentClient;
            Service = serviceInfo;
            ExtendsContentIntoTitleBar = true;
            _ = GetLogs();
        }

        private async Task GetLogs()
        {
            AsyncServerStreamingCall<JournalChunk> call = Agent.Journal.Action(new Journal.V1.JournalRequest() { NumFromTail = 20, Field = Journal.V1.JournalRequest.Types.Field.Systemd, Value = Service.Name });

            await foreach (JournalChunk? response in call.ResponseStream.ReadAllAsync())
            {
                ServiceLogs.Text += response.Reply.ToStringUtf8() + Environment.NewLine;
            }
        }
    }
}
