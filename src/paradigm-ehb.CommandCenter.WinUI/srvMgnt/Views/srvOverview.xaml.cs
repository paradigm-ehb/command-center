using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using paradigm_ehb.CommandCenter.Core.Interfaces;
using paradigm_ehb.CommandCenter.Core.Models;
using Resources.V1;
using System;
using System.Linq;
using System.Timers;


namespace paradigm_ehb.CommandCenter.WinUI.srvMgnt.Views
{
    public sealed partial class srvOverview : Page
    {
        AgentClient? client = null;
        private static Timer aTimer;

        public srvOverview()
        {
            InitializeComponent();
            aTimer = new Timer();
            aTimer.Interval = 3000;
            aTimer.Start();
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
        }

        private async void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            DispatcherQueue.TryEnqueue(async () =>
            {
                updatePage();
            });
        }

        private async void updatePage()
        {
            try
            {
                IAgentEndpointFactory agentEndpointFactory = App.Services.GetRequiredService<IAgentEndpointFactory>();
                IAgentClientFactory agentClientFactory = App.Services.GetRequiredService<IAgentClientFactory>();

                var serveObj = ServerMainPage.Instance.serverObj;

                // Create the endpoint first
                AgentEndpoint endpoint = agentEndpointFactory.Create(serveObj.IpAddress, serveObj.Port, serveObj.UseTls);

                // Then create the client using that endpoint
                client = await agentClientFactory.CreateClientAsync(endpoint);

                var data = client.Resources.GetSystemResources(new GetSystemResourcesRequest());
                OS.Text = data.Resources.Device.OsVersion;
                UptimeTime.Text = data.Resources.Device.Uptime;

                var frequencyGHz = Math.Floor(float.Parse(data.Resources.Cpu.Frequency) / 10) / 100;
                CPU.Text = string.Format("{0} ({1:F2} GHz)", data.Resources.Cpu.Model, frequencyGHz);

                Processes.Text = string.Format("{0} running", data.Resources.Processes.Count());

                var totalRamGB = Math.Floor(float.Parse(data.Resources.Memory.Total) / 1024 / 1024);
                var usedRam = float.Parse(data.Resources.Memory.Total) - float.Parse(data.Resources.Memory.Free);
                var usedRamPercentage = (usedRam / float.Parse(data.Resources.Memory.Total)) * 100;

                RamUsageBar.Progress = usedRamPercentage;
                RamUsagePercent.Text = string.Format("{0:F2}%", usedRamPercentage);
            }
            catch(RpcException ex)
            {
            }
           
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            updatePage();
        }
    }
}
