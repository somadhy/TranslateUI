using System;
using System.IO;
using System.Threading.Tasks;
using TranslateUI.Services;
using Xunit;

namespace TranslateUI.Tests;

public class TxtMdFileHandlerTests
{
    [Fact]
    public async Task ExtractAndBuildOutput_WritesText()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"translateui-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var inputPath = Path.Combine(tempDir, "input.txt");
        var outputPath = Path.Combine(tempDir, "output.txt");

        await File.WriteAllTextAsync(inputPath, "hello");

        try
        {
            var handler = new TxtMdFileHandler();
            var extracted = await handler.ExtractTextAsync(inputPath);
            await handler.BuildOutputAsync(inputPath, "hola", outputPath);

            var saved = await File.ReadAllTextAsync(outputPath);

            Assert.Equal("hello", extracted);
            Assert.Equal("hola", saved);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
