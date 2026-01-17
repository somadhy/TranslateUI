using TranslateUI.Models;

namespace TranslateUI.Services;

public interface IPromptBuilder
{
    string Build(LanguageInfo source, LanguageInfo target, string text);
}

public sealed class PromptBuilder : IPromptBuilder
{
    public string Build(LanguageInfo source, LanguageInfo target, string text)
    {
        return
            $"You are a professional {source.Name} ({source.Code}) to {target.Name} ({target.Code}) translator. " +
            $"Your goal is to accurately convey the meaning and nuances of the original {source.Name} text while " +
            $"adhering to {target.Name} grammar, vocabulary, and cultural sensitivities.\n" +
            $"Produce only the {target.Name} translation, without any additional explanations or commentary. " +
            $"Please translate the following {source.Name} text into {target.Name}:\n\n\n" +
            text;
    }
}
