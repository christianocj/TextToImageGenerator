namespace TextToImageGenerator.Services
{
    public sealed class CloudflareAiOptions
    {
        public string AccountId { get; set; } = string.Empty;
        public string ApiToken { get; set; } = string.Empty;
        public string ImageModel { get; set; } = "@cf/black-forest-labs/flux-2-klein-4b";
        public string TextModel { get; set; } = "@cf/meta/llama-3.1-8b-instruct";
    }
}
