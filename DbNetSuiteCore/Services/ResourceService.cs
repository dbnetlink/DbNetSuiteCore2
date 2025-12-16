using DbNetSuiteCore.Services.Interfaces;
using System.Reflection;
using System.Text;
using System.Data;


namespace DbNetSuiteCore.Services
{
    public class ResourceService : IResourceService
    {
        private HttpContext _context = null;

        public ResourceService()
        {
        }

        public Byte[] Process(HttpContext context, string page)
        {
            try
            {
                _context = context;
                switch (page.ToLower())
                {
                    case "css":
                    case "js":
                        return GetResources(page);
                    default:
                        return GetResource(page.Split(".").Last(), page.Split(".").First());
                }
            }
            catch (Exception ex)
            {
                context.Response.Headers.Append("error", ex.Message.Normalize(NormalizationForm.FormKD).Where(x => x < 128).ToArray().ToString());
                return new Byte[0];
            }
        }


        private Byte[] GetResources(string type)
        {
            var resources = new string[] { };
            switch (type)
            {
                case "css":
                    resources = new string[] { "output", "componentControl", "gridControl", "selectControl" };
                    break;
                case "js":
                    resources = new string[] { "htmx.min", "bundle" };

                    if (_context?.Request.Query.ContainsKey("mode") == true && _context.Request.Query["mode"].ToString() == "blazor")
                    {
                        resources = resources.Append("blazor").ToArray();
                    }
                    break;
            }

            return GetResource(type, resources);
        }

        private Byte[] GetResource(string type, string[] resources)
        {
            byte[] bytes = new byte[0];

            foreach (string resource in resources)
            {
                var resourceBytes = GetResource(type, resource);
                bytes = CombineByteArrays(bytes, resourceBytes);
                bytes = CombineByteArrays(bytes, Encoding.UTF8.GetBytes(Environment.NewLine));
            }
            return bytes;
        }

        private Byte[] GetResource(string type, string resource)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"{assembly.FullName!.Split(",").First()}.Resources.{type.ToUpper()}.{resource}.{type}";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName) ?? new MemoryStream())
            using (var binaryReader = new BinaryReader(stream))
            {
                var bytes = binaryReader.ReadBytes((int)stream.Length);
                return bytes;
            }
        }

        public static byte[] CombineByteArrays(byte[] first, byte[] second)
        {
            byte[] ret = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, ret, 0, first.Length);
            Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
            return ret;
        }
    }
}