using CommunityToolkit.Mvvm.ComponentModel;

namespace TranslateUI.ViewModels;

public partial class CloseBehaviorDialogViewModel : ViewModelBase
{
    public CloseBehaviorDialogViewModel()
    {
    }

    [ObservableProperty]
    private bool dontShowAgain;
}
