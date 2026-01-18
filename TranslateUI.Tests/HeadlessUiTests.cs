using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using TranslateUI.Views;
using Xunit;

namespace TranslateUI.Tests;

public class HeadlessUiTests
{
    [AvaloniaFact]
    public void MainWindow_CanShow()
    {
        var window = new MainWindow();
        window.Show();

        Assert.True(window.IsVisible);
    }

    [AvaloniaFact]
    public void MainWindow_HasExpectedTabs()
    {
        var window = new MainWindow();
        window.Show();

        var tabControl = window.GetVisualDescendants().OfType<TabControl>().FirstOrDefault();

        Assert.NotNull(tabControl);
        Assert.Equal(3, tabControl!.Items.Cast<object>().Count());
    }

    [AvaloniaFact]
    public void MainWindow_TabsHaveExpectedHeaders()
    {
        var window = new MainWindow();
        window.Show();

        var tabControl = window.GetVisualDescendants().OfType<TabControl>().FirstOrDefault();
        Assert.NotNull(tabControl);

        var headers = tabControl!.Items.Cast<TabItem>()
            .Select(item => item.Header as string)
            .ToList();

        Assert.Contains("Text", headers);
        Assert.Contains("File", headers);
        Assert.Contains("Image", headers);
    }

    [AvaloniaFact]
    public void MainWindow_HasLanguageSelectorsAndModelSlider()
    {
        var window = new MainWindow();
        window.Show();

        var autoCompleteBoxes = window.GetVisualDescendants().OfType<AutoCompleteBox>().ToList();
        var slider = window.GetVisualDescendants().OfType<Slider>().FirstOrDefault();

        Assert.Equal(2, autoCompleteBoxes.Count);
        Assert.NotNull(slider);
        Assert.Equal(0, slider!.Minimum);
        Assert.Equal(2, slider.Maximum);
        Assert.True(slider.IsSnapToTickEnabled);
        Assert.Equal(1, slider.TickFrequency);
    }

    [AvaloniaFact]
    public void MainWindow_FileTab_HasOutputAndActionButtons()
    {
        var window = new MainWindow();
        window.Show();

        var tabControl = window.GetVisualDescendants().OfType<TabControl>().FirstOrDefault();
        Assert.NotNull(tabControl);

        tabControl!.SelectedIndex = 1;

        var selectedTab = tabControl.SelectedItem as TabItem;
        Assert.NotNull(selectedTab);

        var textBoxes = GetLogicalDescendants(selectedTab!).OfType<TextBox>().ToList();
        var buttons = GetLogicalDescendants(selectedTab!).OfType<Button>().ToList();

        Assert.Contains(textBoxes, box => box.IsReadOnly);
        Assert.Contains(buttons, button => IsButtonContent(button, GetResourceString("TranslateFileButton")));
        Assert.Contains(buttons, button => IsButtonContent(button, GetResourceString("BrowseButton")));
    }

    [AvaloniaFact]
    public void MainWindow_HasSettingsAndTranslateButtons()
    {
        var window = new MainWindow();
        window.Show();

        var buttons = window.GetVisualDescendants().OfType<Button>().ToList();

        Assert.Contains(buttons, button => IsButtonContent(button, "⚙"));
        Assert.Contains(buttons, button => IsButtonContent(button, "▶"));
    }

    private static bool IsButtonContent(Button button, string expected)
    {
        return button.Content is string content && string.Equals(content, expected, System.StringComparison.Ordinal);
    }

    private static string GetResourceString(string key)
    {
        var resources = Avalonia.Application.Current?.Resources as ResourceDictionary;
        if (resources is null)
        {
            return key;
        }

        if (resources.TryGetResource(key, theme: null, out var value) && value is string text)
        {
            return text;
        }

        foreach (var dictionary in resources.MergedDictionaries)
        {
            if (dictionary.TryGetResource(key, theme: null, out value) && value is string mergedText)
            {
                return mergedText;
            }
        }

        return key;
    }

    private static IEnumerable<object> GetLogicalDescendants(object root)
    {
        if (root is not ILogical logical)
        {
            yield break;
        }

        foreach (var child in logical.LogicalChildren)
        {
            yield return child;
            foreach (var descendant in GetLogicalDescendants(child))
            {
                yield return descendant;
            }
        }
    }
}
