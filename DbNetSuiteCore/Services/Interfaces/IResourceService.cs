namespace DbNetSuiteCore.Services.Interfaces
{
    public interface IResourceService
    {
        Byte[] Process(HttpContext context, string page);
    }
}