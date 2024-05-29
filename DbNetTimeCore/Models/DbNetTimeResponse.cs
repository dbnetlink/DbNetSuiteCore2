namespace DbNetTime.Models
{
    public class DbNetTimeResponse
    {
        public bool Error { get; set; } = false;
        public string Message { get; set; } = string.Empty;
        public string Html { get; set; } = string.Empty;
    }
}