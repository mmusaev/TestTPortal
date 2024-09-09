using System.Configuration;
using System.ServiceProcess;

namespace TPortalTest
{
    public partial class TPortalClient : ServiceBase
    {
        private FileProcessorManager _fileProcessorManager;
        private static ManualResetEvent _exitEvent = new ManualResetEvent(false);

        public static void Main(string[] args)
        {
            if (Environment.UserInteractive)
            {
                // Running as a console application for debugging
                var service = new TPortalClient();
                service.OnStart(args);

                Console.WriteLine("Service running... Press Ctl+C to exit.");

                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true;
                    _exitEvent.Set();
                };
                _exitEvent.WaitOne();
                service.OnStop();
            }
            else
            {
                // Running as a Windows Service
                Run(new TPortalClient());
            }
        }

        // When the service starts, we initialize the file processor
        protected override void OnStart(string[] args)
        {
            // Read directory paths from App.config
            string watchedDirectory = ConfigurationManager.AppSettings["WatchedDirectory"];
            string pluginDirectory = ConfigurationManager.AppSettings["PluginDirectory"];

            // Initialize the FileProcessorManager with the configured directories
            _fileProcessorManager = new FileProcessorManager(watchedDirectory, pluginDirectory);

        }

        // Clean up when the service stops
        protected override void OnStop()
        {
            _fileProcessorManager.Dispose(); // Ensure resources are disposed properly
        }
    }
}
