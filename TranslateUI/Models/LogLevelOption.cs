using Microsoft.Extensions.Logging;

namespace TranslateUI.Models;

public sealed class LogLevelOption
{
    public LogLevelOption(LogLevel value, string resourceKey)
    {
        Value = value;
        ResourceKey = resourceKey;
    }

    public LogLevel Value { get; }
    public string ResourceKey { get; }
}
