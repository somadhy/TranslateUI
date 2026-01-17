using Microsoft.Extensions.Logging;

namespace TranslateUI.Models;

public sealed class LogLevelOption
{
    public LogLevelOption(LogLevel value, string display)
    {
        Value = value;
        Display = display;
    }

    public LogLevel Value { get; }
    public string Display { get; }
}
