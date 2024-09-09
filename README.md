# Windows Service with MEF, FileSystemWatcher, BlockingCollection, and Asynchronous File Processing
Overview
This solution demonstrates how to create a Windows Service that monitors a directory for incoming XML files, processes them asynchronously using Managed Extensibility Framework (MEF) plugins, and moves the files to a "processed" directory after processing. This design adheres to the Dependency Inversion Principle (DIP), with both the high-level module and the low-level module depending on abstractions.

## Conceptual Overview
What is MEF (Managed Extensibility Framework)?
MEF is a framework that helps you design extensible and pluggable applications. It allows parts of an application to be dynamically composed at runtime. This is especially useful when you need to extend functionality without modifying the core logic.

#### Exports: Components (like classes) that are exposed to the system for usage. In this case, our BloombergRestImplementation class is an export.
#### Imports: Components that the system can use from external sources. For example, FileProcessorManager imports implementations of the IBloombergRest interface.
MEF automatically resolves dependencies, meaning you don’t have to manually wire up all the objects.

### Windows Service with MEF, FileSystemWatcher, BlockingCollection, and Asynchronous File Processing
Overview
This solution demonstrates how to create a Windows Service that monitors a directory for incoming XML files, processes them asynchronously using Managed Extensibility Framework (MEF) plugins, and moves the files to a "processed" directory after processing. This design adheres to the Dependency Inversion Principle (DIP), with both the high-level module and the low-level module depending on abstractions.

Key technologies and concepts:

## 1. Windows Service: Watches for file creation and processes files asynchronously.
## 2. FileSystemWatcher: Monitors a directory for changes (new XML files).
## 3. MEF (Managed Extensibility Framework): Dynamically loads plugins at runtime to process XML files based on file types.
## 4. BlockingCollection: Manages a queue for the files to ensure efficient producer-consumer patterns.
## 5. Dependency Inversion Principle (DIP): Both the high-level and low-level components depend on an abstraction (IBloombergRest interface), ensuring flexibility and maintainability.
## 6. ConfigurationManager: Configures directories for watching and processing through the app's configuration file (App.config).
## 7. Asynchronous Programming (Tasks): Processes files asynchronously to ensure non-blocking, efficient file processing.

### 1. App Configuration (App.config)
WatchedDirectory: Directory where new XML files are added and watched by the service.
ProcessedDirectory: Directory where processed files will be moved after processing.
PluginDirectory: Directory containing MEF plugins (DLLs) for XML processing.

### 2. File Processing Manager (FileProcessorManager)
The FileProcessorManager is responsible for watching the directory, queuing files for processing, invoking MEF-based plugins to process files, and moving files to the "processed" directory.
Explanation:

#### FileSystemWatcher:

Monitors the specified directory for new XML files. When a file is created, it triggers the OnFileCreated event, adding the file path to the BlockingCollection queue.

#### BlockingCollection:

Acts as a thread-safe queue that stores file paths added by the producer (FileSystemWatcher). The consumer thread processes files asynchronously from this collection.

#### MEF (Managed Extensibility Framework):

[ImportMany] attribute is used to import multiple implementations of IBloombergRest. MEF dynamically loads these plugins from the configured directory. This allows easy extension of the system without changing the core code.
The CompositionContainer and DirectoryCatalog are used to load and compose parts dynamically from the plugin directory.

#### Asynchronous Processing:

The ConsumerTask method runs as an asynchronous task that dequeues files from BlockingCollection and processes them using the imported IBloombergRest implementations.
Each file is processed based on its name, calling the appropriate methods: GetHistoryAsync for ratehistory.xml and GetSecurityAsync/GetComplexAnalysesAsync for ratelimits.xml.

#### File Moving:

After processing, files are moved to the "processed" directory defined in the configuration. This logic ensures that files are moved only after they have been successfully processed.

### 3. IBloombergRest Interface
Defines the contract for the plugin modules.
IBloombergRest is the abstraction that both the high-level module (FileProcessorManager) and the low-level modules (e.g., BloombergRestImplementation) depend on, ensuring that they interact through a common interface, following the Dependency Inversion Principle.

### 4. BloombergRestImplementation
This is a low-level implementation of the IBloombergRest interface. It provides the actual logic for processing XML files.

### 5. Installation of Windows Service
To run this as a Windows Service:

#### Service Project Setup:

Create a new Windows Service project.
Add the FileProcessorManager as a part of your service class.
Use the InstallUtil.exe tool to install the service.
Service Lifecycle:

In the OnStart method, initialize and start the FileProcessorManager.
In the OnStop method, dispose of the manager and stop file processing.

## Concepts Covered
### Dependency Inversion Principle (DIP):

Both the high-level FileProcessorManager and the low-level BloombergRestImplementation depend on the abstraction (IBloombergRest), making the system flexible and extensible.

### Managed Extensibility Framework (MEF):

MEF allows the dynamic loading of plugins (IBloombergRest implementations) from external directories. This makes it easy to extend the system by adding new processing logic without modifying existing code.
BlockingCollection:

Provides a thread-safe way to manage the producer-consumer pattern, ensuring that files are processed asynchronously without locking resources.

### Asynchronous Programming:

Files are processed asynchronously using Task.Run() to avoid blocking the main service thread.

This solution can be a ###reference### for building more complex, dynamic, and flexible file processing systems in production environments. The concepts demonstrated here are applicable to any scenario involving real-time file processing, modular design, and dynamic extensibility using plugins.



