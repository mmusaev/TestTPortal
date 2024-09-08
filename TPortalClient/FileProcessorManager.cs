using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using IBloombergRest;

namespace TPortalTest
{
    public class FileProcessorManager : IDisposable
    {
        private readonly string _directoryToWatch;
        private readonly string _pluginDirectory;
        private FileSystemWatcher _fileWatcher; // Watches the directory for new files
        private BlockingCollection<string> _fileQueue = new BlockingCollection<string>(); // Queue for files
        private CompositionContainer _container; // MEF container for resolving imports
        private Task _consumerTask;
        private CancellationTokenSource _cancellationTokenSource;
        private IEnumerable<IBloomberg> fileProcessors;

        // MEF will automatically import all IBloombergRest implementations found in the plugin directory
        [ImportMany(typeof(IBloomberg))]
        public IEnumerable<IBloomberg> FileProcessors { get => fileProcessors; set => fileProcessors = value; }

        public FileProcessorManager(string directoryToWatch, string pluginDirectory)
        {
            _directoryToWatch = directoryToWatch;
            _pluginDirectory = pluginDirectory;

            Initialize();
        }

        private void Initialize()
        {
            // Set up the MEF DirectoryCatalog to find plugins (i.e., IBloombergRest implementations) in the specified directory
            var catalog = new DirectoryCatalog(_pluginDirectory);
            _container = new CompositionContainer(catalog);
            _container.ComposeParts(this); // Perform the composition, which will populate the FileProcessors collection

            // Set up the FileSystemWatcher to monitor the directory for new .xml files
            _fileWatcher = new FileSystemWatcher(_directoryToWatch);
            _fileWatcher.Filter = "*.xml";
            _fileWatcher.Created += OnFileCreated; // Event triggered when a new file is created
            _fileWatcher.EnableRaisingEvents = true;

            // Start the consumer task that will process the files asynchronously
            _cancellationTokenSource = new CancellationTokenSource();
            _consumerTask = Task.Run(() => ConsumerTask(_cancellationTokenSource.Token));
        }

        // This method is triggered whenever a new file is created in the watched directory
        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            _fileQueue.Add(e.FullPath); // Producer adds the new file to the queue
        }

        // Consumer task: It continuously processes files from the queue
        private async Task ConsumerTask(CancellationToken cancellationToken)
        {
            foreach (var filePath in _fileQueue.GetConsumingEnumerable(cancellationToken))
            {
                try
                {
                    var fileName = Path.GetFileName(filePath);

                    // Based on the file name, decide which method to invoke from the plugin
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
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing file {filePath}: {ex.Message}");
                }
            }
        }

        // Helper method to process files with the appropriate plugin method
        private async Task ProcessFileAsync(string filePath, Func<IBloombergRest.IBloomberg, Task> action)
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
