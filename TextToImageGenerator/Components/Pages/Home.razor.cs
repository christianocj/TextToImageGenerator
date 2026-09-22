using TextToImageGenerator.Models;

namespace TextToImageGenerator.Components.Pages
{
    public partial class Home
    {
        private GenerationMode _mode = GenerationMode.Texto;
        private string _prompt = string.Empty;
        private int _imageCount = 1;
        private bool _isGenerating;
        private string? _errorMessage;
        private string _generatedText = string.Empty;
        private readonly List<string> _images = new();

        private async Task GerarAsync()
        {
            if (string.IsNullOrWhiteSpace(_prompt))
            {
                _errorMessage = "Digite um prompt antes de gerar.";
                return;
            }

            _errorMessage = null;
            _isGenerating = true;
            _generatedText = string.Empty;
            _images.Clear();
            StateHasChanged();

            try
            {
                if (_mode == GenerationMode.Texto)
                {
                    await foreach (var chunk in AiService.StreamTextAsync(_prompt))
                    {
                        _generatedText += chunk;
                        StateHasChanged();
                    }
                }
                else
                {
                    var count = Math.Clamp(_imageCount, 1, 10);
                    for (var i = 0; i < count; i++)
                    {
                        var bytes = await AiService.GenerateImageAsync(_prompt, 1024, 1024);
                        var base64 = Convert.ToBase64String(bytes);
                        _images.Add($"data:image/png;base64,{base64}");
                        StateHasChanged();
                    }
                }
            }
            catch (Exception ex)
            {
                _errorMessage = ex.Message;
            }
            finally
            {
                _isGenerating = false;
                StateHasChanged();
            }
        }
    }
}
