using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptPrepdocs
{
    public class App
    {

        private readonly DocumentAnalysisPdfParser _documentAnalysisPdfParser;
        private readonly BlobManager _blobManager;
        private readonly SearchManager _searchManager;

        public App(DocumentAnalysisPdfParser documentAnalysisPdfParser, BlobManager blobManager, SearchManager searchManager)
        {
            _documentAnalysisPdfParser = documentAnalysisPdfParser;
            _blobManager = blobManager;
            _searchManager = searchManager;
        }

        public async Task Run(string folderPath)
        {

            var files = Directory.GetFiles(folderPath).Where(x => x.EndsWith(".pdf"));

            await _searchManager.CreateIndex();

            foreach (var filename in files)
            {
                try
                {
                    // Abre el archivo en modo de lectura
                    using (FileStream fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                    {
                        // Crea un buffer para almacenar los datos del archivo
                        byte[] buffer = new byte[fileStream.Length];

                        // Lee los datos del archivo y los guarda en el buffer
                        fileStream.Read(buffer, 0, buffer.Length);


                        var file = new ScriptPrepdocs.File(fileStream);
                        await _blobManager.UploadBlob(file);



                        List<Section> sections = new List<Section>();

                        await foreach (var page in _documentAnalysisPdfParser.ParseAsync(file))
                        {
                            sections.Add(new Section(page, file));
                        }

                        await _searchManager.UpdateContent(sections);


                        Console.WriteLine($"");
                        Console.WriteLine($"Finished!!");


                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }


            }



        }
    }
}
