using Azure.AI.OpenAI;

using Polly;

namespace ScriptPrepdocs
{
    public class OpenAIEmbeddings
    {

        private readonly OpenAIClient _openAIClient;

        public OpenAIEmbeddings(OpenAIClient openAIClient)
        {
            _openAIClient = openAIClient;
        }

        public async Task<IReadOnlyList<EmbeddingItem>> CreateEmbeddings(List<string> inputs)
        {
            const int MaxBatchSize = 16;
            var inputsBatches = new List<List<string>>();
            for (var i = 0; i < inputs.Count; i += MaxBatchSize)
            {
                inputsBatches.Add(inputs.GetRange(i, Math.Min(MaxBatchSize, inputs.Count - i)));
            }




            List<EmbeddingItem> embeddingItems = new List<EmbeddingItem>();

            foreach (var batch in inputsBatches)
            {
                await Policy
                .Handle<Azure.RequestFailedException>()
                .WaitAndRetryAsync(
                    retryCount: 15,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(new Random().Next(15, 60)),
                    onRetry: (exception, _, _) => BeforeRetrySleep())
                .ExecuteAsync(async () =>
                {
                    var responseEmbedding = await _openAIClient.GetEmbeddingsAsync(new EmbeddingsOptions("embedding", batch));
                    embeddingItems.AddRange(responseEmbedding.Value.Data);


                    Console.WriteLine($"Batch Completed. Batch size {batch.Count}.");

                });




            }



            return embeddingItems;
        }

        private void BeforeRetrySleep()
        {
            Console.WriteLine("Rate limited on the OpenAI embeddings API, sleeping before retrying...");
        }
    }
}
