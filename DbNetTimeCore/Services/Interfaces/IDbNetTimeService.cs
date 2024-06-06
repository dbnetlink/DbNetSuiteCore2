namespace DbNetTimeCore.Services.Interfaces
{
    public interface IDbNetTimeService
    {
        Task<Byte[]> Process(HttpContext context, string page);
    }
}