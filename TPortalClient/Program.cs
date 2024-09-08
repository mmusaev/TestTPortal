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

                Console.WriteLine("Service running... Press Enter to exit.");

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
            // Initialize the FileProcessorManager with the directory to watch and plugin directory
            _fileProcessorManager = new FileProcessorManager(
                @"D:\watched-directory",  // Directory to watch for XML files
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins") // Plugin directory where MEF plugins reside
            );
        }

        // Clean up when the service stops
        protected override void OnStop()
        {
            _fileProcessorManager.Dispose(); // Ensure resources are disposed properly
        }
    }
}
