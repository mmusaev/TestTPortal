using System.ComponentModel.Composition;
using System.Diagnostics;
using IBloombergRest;

namespace BloombergRest
{
    [Export(typeof(IBloomberg))]
    public class BloombergRestImplementation : IBloomberg
    {
        public async Task GetHistoryAsync(string filePath)
        {
            Debug.WriteLine($"Processing rate history from file: {filePath}");
            await Task.Delay(1000); // Simulate processing delay
            Debug.WriteLine($"Completed processing rate history from file: {filePath}");
        }

        public async Task GetSecurityAsync(string filePath)
        {
            Debug.WriteLine($"Processing security from file: {filePath}");
            await Task.Delay(1000);
            Debug.WriteLine($"Completed processing security from file: {filePath}");
        }

        public async Task GetComplexAnalysesAsync(string filePath)
        {
            Debug.WriteLine($"Processing complex analyses from file: {filePath}");
            await Task.Delay(1000);
            Debug.WriteLine($"Completed processing complex analyses from file: {filePath}");
        }
    }

}
