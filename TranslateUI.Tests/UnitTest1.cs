using TranslateUI.Models;
using TranslateUI.Services;
using Xunit;

namespace TranslateUI.Tests;

public class PromptBuilderTests
{
    [Fact]
    public void Build_IncludesTwoBlankLinesBeforeText()
    {
        var builder = new PromptBuilder();
        var source = new LanguageInfo("en", "English");
        var target = new LanguageInfo("ru", "Russian");

        var prompt = builder.Build(source, target, "Hello");

        Assert.Contains("\n\n\nHello", prompt);
    }
}
