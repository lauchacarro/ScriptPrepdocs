using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;

using HtmlAgilityPack;

using System.Text;
namespace ScriptPrepdocs;
public class DocumentAnalysisPdfParser
{
    private readonly string modelId = "prebuilt-layout";

    private readonly DocumentAnalysisClient _documentAnalysisClient;

    public DocumentAnalysisPdfParser(DocumentAnalysisClient documentAnalysisClient)
    {
        _documentAnalysisClient = documentAnalysisClient;
    }

    public async IAsyncEnumerable<Page> ParseAsync(ScriptPrepdocs.File file)
    {




        Console.WriteLine($"Extracting text from '{file.Filename()}' using Azure Document Intelligence");


        using (FileStream fileStream = new FileStream(file.FileFullName(), FileMode.Open, FileAccess.Read))
        {
            // Crea un buffer para almacenar los datos del archivo
            byte[] buffer = new byte[fileStream.Length];

            // Lee los datos del archivo y los guarda en el buffer
            fileStream.Read(buffer, 0, buffer.Length);

            // Crea un objeto MemoryStream y carga los datos del buffer en él
            using (MemoryStream memoryStream = new MemoryStream(buffer))
            {
                var poller = await _documentAnalysisClient.AnalyzeDocumentAsync(WaitUntil.Started, modelId, memoryStream);
                var formRecognizerResults = await poller.WaitForCompletionAsync();

                var formRecognizerValues = formRecognizerResults.Value;

                var offset = 0;

                foreach (var (page, pageContent) in formRecognizerValues.Pages.Select((page, index) => (index, page)))
                {
                    var tablesOnPage = formRecognizerValues.Tables?.Where(table =>
                        table.BoundingRegions.Any(region => region.PageNumber == page + 1)).ToList() ?? new List<DocumentTable>();

                    var pageOffset = pageContent.Spans[0].Index;
                    var pageLength = pageContent.Spans[0].Length;
                    var tableChars = new int[pageLength];
                    Array.Fill(tableChars, -1);

                    foreach (var (tableId, table) in tablesOnPage.Select((table, index) => (index, table)))
                    {
                        foreach (var span in table.Spans)
                        {
                            for (var i = 0; i < span.Length; i++)
                            {
                                var idx = span.Index - pageOffset + i;
                                if (idx >= 0 && idx < pageLength)
                                {
                                    tableChars[idx] = tableId;
                                }
                            }
                        }
                    }

                    var pageText = new StringBuilder();
                    var addedTables = new HashSet<int>();
                    foreach (var (idx, tableId) in tableChars.Select((value, index) => (index, value)))
                    {
                        if (tableId == -1)
                        {
                            pageText.Append(formRecognizerValues.Content[pageOffset + idx]);
                        }
                        else if (!addedTables.Contains(tableId))
                        {
                            pageText.Append(TableToHtml(tablesOnPage[tableId]));
                            addedTables.Add(tableId);
                        }
                    }

                    yield return new Page(page + 1, offset, pageText.ToString());
                    offset += pageText.Length;
                }

            }
        }










    }

    private static string TableToHtml(DocumentTable table)
    {
        var tableHtml = new StringBuilder("<table>");
        var rows = table.Cells.GroupBy(cell => cell.RowIndex)
            .Select(rowCells => rowCells.OrderBy(cell => cell.ColumnIndex))
            .ToList();

        foreach (var rowCells in rows)
        {
            tableHtml.Append("<tr>");
            foreach (var cell in rowCells)
            {
                var tag = cell.Kind == "columnHeader" || cell.Kind == "rowHeader" ? "th" : "td";
                var cellSpans = "";
                if (cell.ColumnSpan > 1)
                {
                    cellSpans += $" colspan={cell.ColumnSpan}";
                }
                if (cell.RowSpan > 1)
                {
                    cellSpans += $" rowspan={cell.RowSpan}";
                }

                tableHtml.Append($"<{tag}{cellSpans}>{HtmlEncode(cell.Content)}</{tag}>");
            }

            tableHtml.Append("</tr>");
        }

        tableHtml.Append("</table>");
        return tableHtml.ToString();
    }

    private static string HtmlEncode(string content)
    {
        return HtmlEntity.Entitize(content);
    }
}

public class Page
{
    public int PageNum { get; }
    public int Offset { get; }
    public string Text { get; }

    public Page(int pageNum, int offset, string text)
    {
        PageNum = pageNum;
        Offset = offset;
        Text = text;
    }
}
