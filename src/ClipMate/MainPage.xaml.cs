using ClipMate.ViewModels;

namespace ClipMate;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        BindingContext = new MainViewModel();
    }   
}
