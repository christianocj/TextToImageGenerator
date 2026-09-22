namespace TextToImageGenerator.Services
{
    public interface ICloudflareAiService
    {
        Task<byte[]> GenerateImageAsync(string prompt, int width, int height, CancellationToken cancellationToken = default);

        IAsyncEnumerable<string> StreamTextAsync(string prompt, CancellationToken cancellationToken = default);
    }
}
