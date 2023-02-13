using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using OpenHdWebUi.FFmpeg;
using OpenHdWebUi.FileSystem;
using OpenHdWebUi.Server.Configuration;
using OpenHdWebUi.Server.Hubs;
using OpenHdWebUi.Server.Services.AirGround;
using OpenHdWebUi.Server.Services.Commands;
using OpenHdWebUi.Server.Services.Files;
using OpenHdWebUi.Server.Services.Media;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

namespace OpenHdWebUi.Server;

public class Program
{
    public static async Task Main(string[] args)
    {
        await PrestartAsync();

        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Host.UseSystemd();
        builder.Services
            .AddOptions<ServiceConfiguration>()
            .Bind(builder.Configuration);
        builder.Services
            .AddScoped<SystemCommandsService>()
            .AddScoped<SystemFilesService>();
        builder.Services
            .AddSingleton<MediaService>()
            .AddSingleton<AirGroundService>();
        builder.Services
            .AddDirectoryBrowser();
        builder.Services.AddControllersWithViews();
        builder.Services.AddSignalR();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
        }

        app.UseStaticFiles();

        var config = app.Services.GetRequiredService<IOptions<ServiceConfiguration>>().Value;
        var absoluteMediaPath = Path.GetFullPath(config.FilesFolder);
        FileSystemHelpers.EnsureFolderCreated(absoluteMediaPath);

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(absoluteMediaPath),
            RequestPath = "/media",
            ServeUnknownFileTypes = true,
            ContentTypeProvider = new FileExtensionContentTypeProvider(new Dictionary<string, string>{{".mkv", "video/x-matroska" } })
        });

        FileSystemHelpers.EnsureFolderCreated(MediaConsts.PreviewsFsPath);

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(MediaConsts.PreviewsFsPath),
            RequestPath = $"/{MediaConsts.PreviewsWebPath}",
            ServeUnknownFileTypes = true
        });

        app.UseRouting();

        app.MapControllerRoute(
            name: "default",
            pattern: "api/{controller}/{action=Index}/{id?}");

        app.MapHub<VideoHub>("/videoHub");

        app.MapFallbackToFile("index.html");

        app.Run();
    }

    private static async Task PrestartAsync()
    {
        FileSystemHelpers.EnsureCurrentDirectoryIsBinaryDirectory();
        await FFmpegHelpers.EnsureFFmpegAvailableAsync();
    }
}