using System.Text.Json;
namespace DbNetTime.Services.Interfaces
{
    public interface IDbNetTimeService
    {
        Task<Byte[]> Process(HttpContext context, string page);
    }
}