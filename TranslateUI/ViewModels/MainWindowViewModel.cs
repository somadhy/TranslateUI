namespace TranslateUI.ViewModels;

using Microsoft.Extensions.Logging;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ILogger<MainWindowViewModel> _logger;

    public MainWindowViewModel(ILogger<MainWindowViewModel> logger)
    {
        _logger = logger;
        _logger.LogDebug("MainWindowViewModel initialized");
    }
}
