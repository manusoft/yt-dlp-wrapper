using ClipMate.ViewModels;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using Microsoft.Maui.Controls.Shapes;

namespace ClipMate.Views;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel viewModel;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
    }

    private async void OnAboutClicked(object sender, EventArgs e)
    {
        var popup = new AboutPopup();

        await this.ShowPopupAsync(popup, new PopupOptions
        {
            Shape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(8),
                StrokeThickness = 0
            }
        });
    }

    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        var popup = new SettingsPopup();

        await this.ShowPopupAsync(popup, new PopupOptions
        {
            Shape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(8),
                StrokeThickness = 0
            }
        });
    }
}
