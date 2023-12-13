using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using System.Text.RegularExpressions;

namespace ScriptPrepdocs
{
    public class BlobManager
    {
        private readonly BlobContainerClient _containerClient;

        public BlobManager(BlobContainerClient containerClient)
        {
            _containerClient = containerClient;
        }

        public async Task UploadBlob(File file)
        {

            if (!await _containerClient.ExistsAsync())
            {
                await _containerClient.CreateAsync();
            }


            using (FileStream fileStream = new FileStream(file.FileFullName(), FileMode.Open, FileAccess.Read))
            {
                // Crea un buffer para almacenar los datos del archivo
                byte[] buffer = new byte[fileStream.Length];

                // Lee los datos del archivo y los guarda en el buffer
                fileStream.Read(buffer, 0, buffer.Length);

                // Crea un objeto MemoryStream y carga los datos del buffer en él
                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    var blobName = BlobNameFromFile(file.Filename());
                    Console.WriteLine(value: $"\tUploading blob for whole file -> {blobName}");
                    await _containerClient.GetBlobClient(blobName).UploadAsync(memoryStream, true);

                }
            }
        }

        public async Task RemoveBlob(string path = null)
        {

            if (!await _containerClient.ExistsAsync())
            {
                return;
            }

            string prefix = null;
            IAsyncEnumerable<BlobHierarchyItem> blobs;

            if (path is null)
            {
                blobs = _containerClient.GetBlobsByHierarchyAsync(prefix: null);
            }
            else
            {
                prefix = Path.GetFileNameWithoutExtension(path);
                blobs = _containerClient.GetBlobsByHierarchyAsync(prefix: prefix);
            }

            await foreach (var blobPath in blobs)
            {
                if ((prefix != null && !Regex.IsMatch($"{prefix}-\\d+\\.pdf", blobPath.Blob.Name)) ||
                    (path != null && blobPath.Blob.Name == Path.GetFileName(path)))
                {
                    continue;
                }


                Console.WriteLine($"\tRemoving blob {blobPath.Blob.Name}");


                await _containerClient.GetBlobClient(blobPath.Blob.Name).DeleteIfExistsAsync();
            }
        }

        public static string SourcepageFromFilePage(string filename, int page = 0)
        {
            if (Path.GetExtension(filename)?.ToLower() == ".pdf")
            {
                return $"{Path.GetFileName(filename)}#page={page + 1}";
            }
            else
            {
                return Path.GetFileName(filename);
            }
        }

        public static string BlobNameFromFile(string filename)
        {
            return Path.GetFileName(filename);
        }
    }
}
