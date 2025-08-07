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
            var version = AppInfo.Version;

            return new Window(new AppShell())
            {                 
                Title = $"ClipMate v{version.Major}.{version.Minor}.{version.Build}",
                MinimumHeight = 600,
                MinimumWidth = 900,
                Height = 700,
                Width = 1000,                
                X = 100,
                Y = 100
            };
        }
    }
}