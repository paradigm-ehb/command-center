using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
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
        IAgentClientRegistry _agentClientRegistry;
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
                GetSystemResourcesResponse data = client.Resources.GetSystemResources(new GetSystemResourcesRequest());
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

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            AgentEndpoint serverObj = ServerMainPage.Instance.serverObj;

            _agentClientRegistry = App.Services.GetRequiredService<IAgentClientRegistry>();

            client = await _agentClientRegistry.GetAsync(serverObj.Id);

            updatePage();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            aTimer.Stop();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            aTimer.Start();
        }
    }
}
