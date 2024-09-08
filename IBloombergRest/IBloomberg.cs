namespace IBloombergRest
{
    using System.Threading.Tasks;

    public interface IBloomberg
    {
        Task GetHistoryAsync(string filePath);
        Task GetSecurityAsync(string filePath);
        Task GetComplexAnalysesAsync(string filePath);
    }

}
