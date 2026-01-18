namespace TranslateUI.Models;

public enum CloseBehavior
{
    Exit,
    MinimizeToTray
}

public sealed class CloseBehaviorOption
{
    public CloseBehaviorOption(CloseBehavior value, string resourceKey)
    {
        Value = value;
        ResourceKey = resourceKey;
    }

    public CloseBehavior Value { get; }
    public string ResourceKey { get; }
}
