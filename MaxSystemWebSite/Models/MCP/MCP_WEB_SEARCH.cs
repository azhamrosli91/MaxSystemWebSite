namespace MaxSystemWebSite.Models.MCP
{
    public class ApiResponse
    {
        public string Id { get; set; }
        public string Object { get; set; }
        public long CreatedAt { get; set; }
        public string Status { get; set; }
        public object Error { get; set; }
        public object IncompleteDetails { get; set; }
        public object Instructions { get; set; }
        public object MaxOutputTokens { get; set; }
        public string Model { get; set; }
        public List<OutputItem> Output { get; set; }
        public bool ParallelToolCalls { get; set; }
        public object PreviousResponseId { get; set; }
        public Reasoning Reasoning { get; set; }
        public bool Store { get; set; }
        public double Temperature { get; set; }
        public TextFormat Text { get; set; }
        public string ToolChoice { get; set; }
        public List<Tool> Tools { get; set; }
        public double TopP { get; set; }
        public string Truncation { get; set; }
        public Usage Usage { get; set; }
        public object User { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    public class OutputItem
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public string Role { get; set; }
        public List<ContentItem> Content { get; set; }
    }

    public class ContentItem
    {
        public string Type { get; set; }
        public string Text { get; set; }
        public List<Annotation> Annotations { get; set; }
    }

    public class Annotation
    {
        public string Type { get; set; }
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
        public string Url { get; set; }
        public string Title { get; set; }
    }

    public class Reasoning
    {
        public object Effort { get; set; }
        public object Summary { get; set; }
    }

    public class TextFormat
    {
        public Format Format { get; set; }
    }

    public class Format
    {
        public string Type { get; set; }
    }

    public class Tool
    {
        public string Type { get; set; }
        public string SearchContextSize { get; set; }
        public UserLocation UserLocation { get; set; }
        // Removed Domains since it's not present in the sample JSON
    }

    public class UserLocation
    {
        public string Type { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string Region { get; set; }
        public string Timezone { get; set; }
    }

    public class Usage
    {
        public int InputTokens { get; set; }
        public InputTokensDetails InputTokensDetails { get; set; }
        public int OutputTokens { get; set; }
        public OutputTokensDetails OutputTokensDetails { get; set; }
        public int TotalTokens { get; set; }
    }

    public class InputTokensDetails
    {
        public int CachedTokens { get; set; }
    }

    public class OutputTokensDetails
    {
        public int ReasoningTokens { get; set; }
    }
}
