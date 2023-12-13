using System.Text.Json.Serialization;

namespace ScriptPrepdocs
{
    public class Document
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }


        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; }


        [JsonPropertyName("sourcepage")]
        public string Sourcepage { get; set; }

        [JsonPropertyName("sourcefile")]
        public string Sourcefile { get; set; }


        [JsonPropertyName("embedding")]
        public float[] Embedding { get; set; }

    }
}
