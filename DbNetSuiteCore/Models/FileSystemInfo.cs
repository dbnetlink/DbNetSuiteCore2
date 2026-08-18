using Microsoft.Extensions.FileProviders;

namespace DbNetSuiteCore.Models
{
    public class FileSystemInfo
    {
        public bool Icon { get; set; }
        public bool IsDirectory { get; set; }
        public string Name { get; set; }
        public string Extension { get; set; }
        public long? Length { get; set; }
        public DateTime LastModified { get; set; }
        public string Folder { get; set; }
        public string ParentFolder { get; set; }
        public string Path { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;

        public FileSystemInfo()
        {
            Icon = false;
            IsDirectory = false;
            Name = string.Empty;
            Extension = string.Empty;
            Length = null;
            LastModified = DateTime.MinValue;
            Folder = string.Empty;
            ParentFolder = string.Empty;
            Content = string.Empty;
        }

        public FileSystemInfo(IFileInfo file, IWebHostEnvironment env)
        {
            Icon = file.IsDirectory;
            IsDirectory = file.IsDirectory;
            Name = file.Name;
            Extension = file.IsDirectory ? string.Empty : file.Name.Split(".").Last();
            Length = file.IsDirectory ? null : file.Length;
            LastModified = file.LastModified.UtcDateTime;
            Folder = file.IsDirectory ? file.Name : GetParentFolder(GetPath(file.PhysicalPath, env));
            ParentFolder = GetParentFolder(GetPath(file.PhysicalPath, env));
            Path = GetPath(file.PhysicalPath, env);
            Content = file.IsDirectory ? string.Empty : ReadFileContent(file);
        }

        private string GetParentFolder(string path)
        {
            return path.Split("/").Count() > 1 ? path.Split("/").Reverse().Skip(1).First() : string.Empty;
        }

        private string GetPath(string physicalPath, IWebHostEnvironment env)
        {
            if (physicalPath == null)
            {
                return string.Empty;
            }

            return physicalPath.Replace(env.WebRootPath, string.Empty).Replace("\\", "/");
        }

        private static string ReadFileContent(IFileInfo fileInfo)
        {
            return string.Empty;
            /*
            using (var reader = new StreamReader(fileInfo.CreateReadStream()))
            {
                return reader.ReadToEnd();
            }
            */
        }
    }
}
