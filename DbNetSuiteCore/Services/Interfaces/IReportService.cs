namespace DbNetSuiteCore.Services.Interfaces
{
    public interface IReportService
    {
        Task<Byte[]> Process(HttpContext context, string page);
    }
}