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
    public async Task ShowToastAsync(string message, ToastDuration toastDuration = ToastDuration.Short)
    {
        try
        {
            var toast = Toast.Make(message, toastDuration, 14);
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
            TimeSpan duration = TimeSpan.FromSeconds(3);

            var snackbarOptions = new SnackbarOptions
            {
                CornerRadius = new CornerRadius(10),
                CharacterSpacing = 0.5
            };

            var snackbar = Snackbar.Make(message,null, "OK", duration, snackbarOptions);
            await snackbar.Show(new CancellationTokenSource().Token);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}
