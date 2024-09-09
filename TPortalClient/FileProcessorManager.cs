using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Configuration;
using IBloombergRest;

namespace TPortalTest
{

    public class FileProcessorManager : IDisposable
    {
        private readonly string _directoryToWatch;
        private readonly string _processedDirectory;
        private readonly string _pluginDirectory;
        private FileSystemWatcher _fileWatcher;
        private BlockingCollection<string> _fileQueue = new BlockingCollection<string>();
        private CompositionContainer _container;
        private Task _consumerTask;
        private CancellationTokenSource _cancellationTokenSource;

        // Import many IBloombergRest implementations dynamically through MEF
        [ImportMany(typeof(IBloomberg))]
        public IEnumerable<IBloomberg> FileProcessors { get; set; }

        // Constructor initializes FileProcessorManager with directories for XML files, processed files, and plugins
        public FileProcessorManager(string directoryToWatch, string pluginDirectory)
        {
            _directoryToWatch = directoryToWatch;
            _processedDirectory = ConfigurationManager.AppSettings["ProcessedDirectory"]; // Load processed directory from config
            _pluginDirectory = pluginDirectory;

            Initialize();
        }

        // Initializes MEF and starts the file-watching mechanism
        private void Initialize()
        {
            // Set up the MEF DirectoryCatalog to find plugins (IBloombergRest implementations) in the specified directory
            var catalog = new DirectoryCatalog(_pluginDirectory);
            _container = new CompositionContainer(catalog);
            _container.ComposeParts(this); // Perform composition to import IBloombergRest instances

            // Initialize the FileSystemWatcher to monitor the directory for new .xml files
            _fileWatcher = new FileSystemWatcher(_directoryToWatch);
            _fileWatcher.Filter = "*.xml";
            _fileWatcher.Created += OnFileCreated;
            _fileWatcher.EnableRaisingEvents = true;

            // Start the consumer task that processes the files asynchronously
            _cancellationTokenSource = new CancellationTokenSource();
            _consumerTask = Task.Run(() => ConsumerTask(_cancellationTokenSource.Token));
        }

        // Event triggered when a new file is created in the watched directory
        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            _fileQueue.Add(e.FullPath); // Add the file to the queue (Producer)
        }

        // Consumer task that processes files from the queue
        private async Task ConsumerTask(CancellationToken cancellationToken)
        {
            foreach (var filePath in _fileQueue.GetConsumingEnumerable(cancellationToken))
            {
                try
                {
                    var fileName = Path.GetFileName(filePath);

                    if (fileName.Equals("ratehistory.xml", StringComparison.OrdinalIgnoreCase))
                    {
                        await ProcessFileAsync(filePath, processor => processor.GetHistoryAsync(filePath));
                    }
                    else if (fileName.Equals("ratelimits.xml", StringComparison.OrdinalIgnoreCase))
                    {
                        await ProcessFileAsync(filePath, processor => processor.GetSecurityAsync(filePath));
                        await ProcessFileAsync(filePath, processor => processor.GetComplexAnalysesAsync(filePath));
                    }
                    else
                    {
                        Console.WriteLine($"Unknown file {filePath}.");
                    }

                    // Move the processed file to the processed directory
                    MoveFile(filePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing file {filePath}: {ex.Message}");
                }
            }
        }

        // Moves the processed file to the processed directory
        private void MoveFile(string filePath)
        {
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var fileExtension = Path.GetExtension(filePath);
                var dateTimeSuffix = DateTime.Now.ToString("yyyyMMddHHmmss");
                var newFileName = $"{fileName}_{dateTimeSuffix}{fileExtension}";
                var destinationPath = Path.Combine(_processedDirectory, newFileName);

                // Ensure the processed directory exists
                if (!Directory.Exists(_processedDirectory))
                {
                    Directory.CreateDirectory(_processedDirectory);
                }

                // Move the file
                File.Move(filePath, destinationPath);
                Console.WriteLine($"Moved file {filePath} to {destinationPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error moving file {filePath} to processed directory: {ex.Message}");
            }
        }


        // Processes the file by calling the appropriate method on the IBloombergRest implementations
        private async Task ProcessFileAsync(string filePath, Func<IBloomberg, Task> action)
        {
            foreach (var processor in FileProcessors)
            {
                try
                {
                    await action(processor); // Call the appropriate method asynchronously
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing file {filePath}: {ex.Message}");
                }
            }
        }

        // Dispose resources to stop file processing and cleanup

        public void Dispose()
        {
            // Cancel the consumer task and dispose of resources
            _cancellationTokenSource.Cancel();
            try
            {
                _consumerTask.Wait();
            }
            catch (AggregateException ae)
            {
                ae.Handle(e => e is TaskCanceledException);
            }
            _fileWatcher.Dispose();
            _fileQueue.Dispose();
            _container.Dispose();
        }
    }
}
