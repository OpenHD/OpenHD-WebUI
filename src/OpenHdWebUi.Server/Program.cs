using System.Net;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using OpenHdWebUi.FileSystem;
using OpenHdWebUi.Server.Configuration;
using OpenHdWebUi.Server.Services.AirGround;
using OpenHdWebUi.Server.Services.Commands;
using OpenHdWebUi.Server.Services.Files;
using OpenHdWebUi.Server.Services.Media;
using OpenHdWebUi.Server.Services.Network;
using OpenHdWebUi.Server.Services.Settings;

namespace OpenHdWebUi.Server;

public class Program
{
    public static async Task Main(string[] args)
    {
        Prestart();

        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Host.UseSystemd();
        builder.Services
            .AddOptions<ServiceConfiguration>()
            .Bind(builder.Configuration);
        builder.Services
            .AddScoped<SystemCommandsService>()
            .AddScoped<SystemFilesService>()
            .AddScoped<NetworkInfoService>();
        builder.Services
            .AddHttpClient()
            .AddSingleton<MediaService>()
            .AddSingleton<AirGroundService>()
            .AddSingleton<SettingsService>();
        builder.Services
            .AddDirectoryBrowser()
            .AddControllers();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
        }

        app.UseDefaultFiles();
        app.UseStaticFiles();

        var mediaService = app.Services.GetRequiredService<MediaService>();
        var absoluteMediaPath = mediaService.MediaDirectoryFullPath;
        if (absoluteMediaPath != null)
        {
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(absoluteMediaPath),
                RequestPath = "/media",
                ServeUnknownFileTypes = true,
                ContentTypeProvider = new FileExtensionContentTypeProvider(new Dictionary<string, string> { { ".mkv", "video/x-matroska" } })
            });
        }

        //app.UseRouting();
        app.MapControllers();
        app.MapFallbackToFile("/index.html");

        app.Run();
    }

    private static void Prestart()
    {
        FileSystemHelpers.EnsureCurrentDirectoryIsBinaryDirectory();
    }
}