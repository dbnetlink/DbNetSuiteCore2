namespace DbNetSuiteCore.Services.Interfaces
{
    public interface IGridService
    {
        Task<Byte[]> Process(HttpContext context, string page);
    }
}