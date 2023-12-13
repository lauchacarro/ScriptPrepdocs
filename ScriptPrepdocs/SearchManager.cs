namespace ScriptPrepdocs
{
    using Azure;
    using Azure.Search.Documents;
    using Azure.Search.Documents.Indexes;
    using Azure.Search.Documents.Indexes.Models;

    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class Section
    {
        public Page SplitPage { get; set; }
        public File Content { get; set; }
        public string Category { get; set; }

        public Section(Page splitPage, File content, string category = null)
        {
            SplitPage = splitPage;

            Category = category;
            Content = content;
        }
    }

    public class SearchManager
    {
        private readonly OpenAIEmbeddings _embeddings;
        private readonly SearchClient _searchClient;
        private readonly SearchIndexClient _searchIndexClient;

        public SearchManager(SearchClient searchClient, SearchIndexClient searchIndexClient, OpenAIEmbeddings embeddings)
        {
            _embeddings = embeddings;
            _searchClient = searchClient;
            _searchIndexClient = searchIndexClient;

        }

        public async Task CreateIndex()
        {

            var searchIndexName = _searchClient.IndexName;


            Console.WriteLine($"Ensuring search index {searchIndexName} exists");


            var fields = new List<SearchField>
        {
            new SimpleField("id", SearchFieldDataType.String) { IsKey = true },
            new SearchableField("content")
            {
                AnalyzerName = LexicalAnalyzerName.EnMicrosoft
            },
            new SearchField("embedding", SearchFieldDataType.Collection(SearchFieldDataType.Single))
            {
                IsHidden = false,
                IsSearchable = true,
                IsFilterable = false,
                IsSortable = false,
                IsFacetable = false,
                VectorSearchDimensions = 1536,
                VectorSearchProfileName = "embedding_config",
            },
            new SimpleField("category", SearchFieldDataType.String)
            {
                IsFilterable = true,
                IsFacetable = true
            },
            new SimpleField("sourcepage", SearchFieldDataType.String)
            {
                IsFilterable = true,
                IsFacetable = true
            },
            new SimpleField("sourcefile", SearchFieldDataType.String)
            {
                IsFilterable = true,
                IsFacetable = true
            }
        };



            var vectorSearch = new VectorSearch();
            vectorSearch.Profiles.Add(new VectorSearchProfile("embedding_config", "hnsw"));
            vectorSearch.Algorithms.Add(new HnswAlgorithmConfiguration("hnsw")
            {
                Parameters = new HnswParameters { Metric = VectorSearchAlgorithmMetric.Cosine },

            });


            var semanticSearch = new SemanticSearch
            {
                DefaultConfigurationName = "default"
            };

            var semanticPrioritizedFields = new SemanticPrioritizedFields();
            semanticPrioritizedFields.ContentFields.Add(new SemanticField("content"));

            semanticSearch.Configurations.Add(new SemanticConfiguration(
                    "default",
                    semanticPrioritizedFields
                ));

            var newIndex = new SearchIndex(searchIndexName)
            {
                Fields = fields,
                VectorSearch = vectorSearch,
                SemanticSearch = semanticSearch,


            };

            bool exists = false;

            await foreach (var indexesNames in _searchIndexClient.GetIndexNamesAsync())
            {
                if (indexesNames == searchIndexName)
                {
                    exists = true;
                }
            }


            if (!exists)
            {

                Console.WriteLine($"Creating {searchIndexName} search index");


                await _searchIndexClient.CreateIndexAsync(newIndex);
            }
            else
            {

                Console.WriteLine($"Search index {searchIndexName} already exists");

            }
        }

        public async Task UpdateContent(List<Section> sections)
        {
            const int MaxBatchSize = 1000;
            var sectionBatches = new List<List<Section>>();
            for (var i = 0; i < sections.Count; i += MaxBatchSize)
            {
                sectionBatches.Add(sections.GetRange(i, Math.Min(MaxBatchSize, sections.Count - i)));
            }


            for (int batchIndex = 0; batchIndex < sectionBatches.Count; batchIndex++)
            {
                var batch = sectionBatches[batchIndex];

                var documents = batch.Select((section, sectionIndex) => new Document
                {
                    Id = $"{section.Content.FilenameToId()}-page-{sectionIndex + batchIndex * MaxBatchSize}",
                    Content = section.SplitPage.Text,
                    Category = section.Category,
                    Sourcepage = BlobManager.SourcepageFromFilePage(section.Content.Filename(), section.SplitPage.PageNum),
                    Sourcefile = section.Content.Filename(),
                    Embedding = Array.Empty<float>()
                }).ToList();

                if (_embeddings != null)
                {
                    var embeddings = await _embeddings.CreateEmbeddings(batch.Select(section => section.SplitPage.Text).ToList());
                    for (var i = 0; i < documents.Count; i++)
                    {
                        documents[i].Embedding = embeddings[i].Embedding.ToArray();
                    }
                }

                await _searchClient.UploadDocumentsAsync(documents);


            }

        }

        public async Task RemoveContent(string path = null)
        {

            Console.WriteLine($"Removing sections from '{path ?? "<all>"}' from search index '{_searchClient.IndexName}'");

            while (true)
            {
                var filter = path == null ? null : $"sourcefile eq '{System.IO.Path.GetFileName(path)}'";
                var result = await _searchClient.SearchAsync<Document>("", new SearchOptions
                {
                    Filter = filter,
                    Size = 1000,
                    IncludeTotalCount = true
                });

                List<Document> documents = new List<Document>();

                await foreach (var document in result.Value.GetResultsAsync())
                {
                    documents.Add(document.Document);



                }

                var removedDocs = await _searchClient.DeleteDocumentsAsync(documents.Select(document => new { id = document.Id }));


                Console.WriteLine($"\tRemoved {removedDocs.Value.Results.Count} sections from index");


                // It can take a few seconds for search results to reflect changes, so wait a bit
                await Task.Delay(2000);
            }
        }
    }

    // You will need to implement the missing classes and methods (e.g., SplitPage, File, SearchInfo, OpenAIEmbeddings, BlobManager) according to your needs.


}
