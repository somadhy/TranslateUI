using System.Threading;
using System.Threading.Tasks;

namespace TranslateUI.Services;

public interface IFileHandler
{
    bool CanHandle(string extension);
    Task<string> ExtractTextAsync(string path, CancellationToken cancellationToken = default);
    Task<string> BuildOutputAsync(string path, string translatedText, string outputPath, CancellationToken cancellationToken = default);
}
