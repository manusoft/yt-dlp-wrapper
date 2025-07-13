using CommunityToolkit.Mvvm.ComponentModel;

namespace ClipMate.ViewModels;

public partial class BaseViewModel : ObservableRecipient
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool isBusy;

    public bool IsNotBusy => !IsBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotAnalizing))]
    private bool isAnalizing;

    public bool IsNotAnalizing => !IsAnalizing;

    [ObservableProperty]
    private bool isAnalyzed;

    [ObservableProperty]
    private bool isAnalyzeError;

    [ObservableProperty]
    private string errorMessage = string.Empty;
}
