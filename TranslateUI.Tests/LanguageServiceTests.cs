using Microsoft.Extensions.Logging.Abstractions;
using TranslateUI.Services;
using Xunit;

namespace TranslateUI.Tests;

public class LanguageServiceTests
{
    [Fact]
    public void TryGetByCode_FindsKnownLanguage()
    {
        var service = new LanguageService(NullLogger<LanguageService>.Instance);

        var found = service.TryGetByCode("en", out var language);

        Assert.True(found);
        Assert.Equal("en", language.Code);
    }
}
