using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
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


    // Toast settings
    public async Task ShowToastAsync(string message)
    {
        try
        {
            var toast = Toast.Make(message, ToastDuration.Long, 14);
            await toast.Show(new CancellationTokenSource().Token);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    // Snackbar settings
    public async Task ShowSnackbarAsync(string message)
    {
        try
        {
            var snackbar = Snackbar.Make(message,null, "OK");
            await snackbar.Show(new CancellationTokenSource().Token);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}
