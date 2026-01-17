namespace TranslateUI.Models;

public sealed class LanguageInfo
{
    public LanguageInfo(string code, string name)
    {
        Code = code;
        Name = name;
    }

    public string Code { get; }

    public string Name { get; }
}
