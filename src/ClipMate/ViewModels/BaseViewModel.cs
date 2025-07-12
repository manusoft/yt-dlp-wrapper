using CommunityToolkit.Mvvm.ComponentModel;

namespace ClipMate.ViewModels;

public partial class BaseViewModel : ObservableRecipient
{
    [ObservableProperty]
    private bool isAnalizing;

    [ObservableProperty]
    private bool isAnalyzed;

    [ObservableProperty]
    private bool isAnalyzeError;

    [ObservableProperty]
    private string errorMessage = string.Empty;
}
