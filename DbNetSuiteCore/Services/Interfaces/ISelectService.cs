namespace DbNetSuiteCore.Services.Interfaces
{
    public interface ISelectService
    {
        Task<Byte[]> Process(HttpContext context, string page);
    }
}