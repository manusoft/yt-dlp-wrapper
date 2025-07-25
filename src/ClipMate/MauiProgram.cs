﻿using ClipMate.Services;
using ClipMate.ViewModels;
using ClipMate.Views;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;


#if DEBUG
using Microsoft.Extensions.Logging;
#endif

namespace ClipMate
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .AddServices()
                .AddViewModels()
                .AddViews()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

        // More services registered here.
        public static MauiAppBuilder AddServices(this MauiAppBuilder builder)
        {
            builder.Services.AddSingleton<JsonService>();
            builder.Services.AddScoped<YtdlpService>();
            builder.Services.AddSingleton<IFolderPicker>(FolderPicker.Default);

            return builder;
        }

        // More view-models registered here.
        public static MauiAppBuilder AddViewModels(this MauiAppBuilder builder)
        {
            builder.Services.AddSingleton<MainViewModel>();
            return builder;
        }

        // More views registered here.
        public static MauiAppBuilder AddViews(this MauiAppBuilder builder)
        {
            builder.Services.AddSingleton<MainPage>();
            return builder;
        }
    }
}
