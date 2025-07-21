namespace ClipMate
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            UserAppTheme = AppTheme.Dark;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell())
            {                 
                Title = "ClipMate v1.0",
                Width = 1000,
                Height = 700,
                X = 100,
                Y = 100
            };
        }
    }
}