using Microsoft.UI.Xaml;
using Microsoft.Extensions.DependencyInjection;
using paradigm_ehb.CommandCenter.Core.Interfaces;
using paradigm_ehb.CommandCenter.WinUI.Components;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace paradigm_ehb.CommandCenter.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;
        private ServiceProvider _serviceProvider;

        /// <summary>
        /// Gets the current App instance as a strongly-typed object.
        /// </summary>
        public static new App Current => (App)Application.Current;

        /// <summary>
        /// Gets the service provider for dependency injection.
        /// </summary>
        public static ServiceProvider Services => Current._serviceProvider;


        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            // Setup Dependency Injection
            IServiceCollection services = new ServiceCollection();

            // TODO: Register Logging services

            // Register CommandCenter Core services
            services.AddCommandCenterCore();

            services.AddSingleton<ICoreMethods, CoreMethods>();

            // TODO: Use MVVM pattern - register ViewModels and other services here

            _serviceProvider = services.BuildServiceProvider();

            InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            // TODO: Use MVVVM pattern
            IAgentEndpointRegistry agentEndpointRegistry = _serviceProvider.GetRequiredService<IAgentEndpointRegistry>();
            IAgentEndpointFactory agentEndpointFactory = _serviceProvider.GetRequiredService<IAgentEndpointFactory>();

            IAgentClientRegistry agentClientRegistry = _serviceProvider.GetRequiredService<IAgentClientRegistry>();
            IAgentClientFactory grpcClientFactory = _serviceProvider.GetRequiredService<IAgentClientFactory>();

            _window = new MainWindow();
            _window.Activate();
        }
    }
}
