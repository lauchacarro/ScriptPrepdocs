using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Storage.Blobs;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ScriptPrepdocs;


// Crear el objeto de configuración y leer el archivo appsettings.json
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();


var AzureBlobStorageConnectionString = configuration["AzureBlobStorage:ConnectionString"];
var AzureBlobStorageContainer = configuration["AzureBlobStorage:Container"];

var AzureFormRecognizerEndpoint = configuration["AzureFormRecognizer:Endpoint"];
var AzureFormRecognizerKey = configuration["AzureFormRecognizer:Key"];

var AzureSearchEndpoint = configuration["AzureSearch:Endpoint"];
var AzureSearchKey = configuration["AzureSearch:Key"];
var AzureSearchIndexName = configuration["AzureSearch:IndexName"];

var AzureOpenAIEndpoint = configuration["AzureOpenAI:Endpoint"];
var AzureOpenAIKey = configuration["AzureOpenAI:Key"];

var FolderPdf = configuration["FolderPdf"];


// Crear el contenedor de servicios y registrar las dependencias
var services = new ServiceCollection();

services.AddSingleton<BlobContainerClient>(_ =>
{
    var blobServiceClient = new BlobServiceClient(AzureBlobStorageConnectionString);
    return blobServiceClient.GetBlobContainerClient(AzureBlobStorageContainer);
});

services.AddSingleton<DocumentAnalysisClient>(_ =>
{
    AzureKeyCredential credential = new AzureKeyCredential(AzureFormRecognizerKey);

    return new DocumentAnalysisClient(new Uri(AzureFormRecognizerEndpoint), credential);
});

services.AddSingleton<OpenAIClient>(_ =>
{
    return new OpenAIClient(
      new Uri(AzureOpenAIEndpoint),
      new AzureKeyCredential(AzureOpenAIKey));
});

services.AddSingleton<SearchClient>(_ =>
{
    return new SearchClient(new Uri(AzureSearchEndpoint), AzureSearchIndexName, new AzureKeyCredential(AzureSearchKey));
});

services.AddSingleton<SearchIndexClient>(_ =>
{
    return new SearchIndexClient(new Uri(AzureSearchEndpoint), new AzureKeyCredential(AzureSearchKey));
});


services.AddSingleton<SearchManager>();
services.AddSingleton<BlobManager>();
services.AddSingleton<DocumentAnalysisPdfParser>();
services.AddSingleton<OpenAIEmbeddings>();


services.AddSingleton<App>();

// Construir el proveedor de servicios
var serviceProvider = services.BuildServiceProvider();

var app = serviceProvider.GetRequiredService<App>();


await app.Run(FolderPdf);