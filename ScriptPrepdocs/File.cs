using System.Text;
using System.Text.RegularExpressions;

namespace ScriptPrepdocs
{
    public class File
    {
        public Stream Content { get; }
        public Dictionary<string, List<string>> Acls { get; }

        public File(Stream content, Dictionary<string, List<string>> acls = null)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Acls = acls ?? new Dictionary<string, List<string>>();
        }

        public string Filename()
        {
            return Path.GetFileName(((FileStream)Content).Name);
        }

        public string FileFullName()
        {
            return ((FileStream)Content).Name;
        }

        public string FilenameToId()
        {
            var filenameAscii = Regex.Replace(Filename(), "[^0-9a-zA-Z_-]", "_");
            var filenameHashBytes = Encoding.UTF8.GetBytes(Filename());
            var filenameHash = BitConverter.ToString(filenameHashBytes).Replace("-", "");
            return $"file-{filenameAscii}-{filenameHash}";
        }

        public void Close()
        {
            Content?.Close();
        }
    }
}
