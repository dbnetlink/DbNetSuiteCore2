namespace DbNetSuiteCore.Services.Interfaces
{
    public interface IComponentService
    {
        Task<Byte[]> Process(HttpContext context, string page);
    }
}