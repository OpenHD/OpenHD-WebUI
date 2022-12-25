using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

using OpenHdWebUi.Server.Configuration;
using OpenHdWebUi.Server.FFmpeg;
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
            .AddScoped<SystemFilesService>()
            .AddScoped<MediaService>();
        builder.Services
            .AddDirectoryBrowser();
        builder.Services.AddControllersWithViews();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
        }

        app.UseStaticFiles();

        var config = app.Services.GetRequiredService<IOptions<ServiceConfiguration>>().Value;
        var absoluteMediaPath = Path.GetFullPath(config.FilesFolder);
        EnsureFolderCreated(absoluteMediaPath);

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(absoluteMediaPath),
            RequestPath = "/media",
            ServeUnknownFileTypes = true
        });

        EnsureFolderCreated(MediaConsts.PreviewsFsPath);

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(MediaConsts.PreviewsFsPath),
            RequestPath = MediaConsts.PreviewsWebPath,
            ServeUnknownFileTypes = true
        });

        app.UseRouting();

        app.MapControllerRoute(
            name: "default",
            pattern: "api/{controller}/{action=Index}/{id?}");

        app.MapFallbackToFile("index.html");

        app.Run();
    }

    private static async Task PrestartAsync()
    {
        var currentDir = GetExeDirectory();
        Directory.SetCurrentDirectory(currentDir);

        await FFmpegHelpers.EnsureFFmpegAvailableAsync();
    }

    private static void EnsureFolderCreated(string fullPath)
    {
        if (Directory.Exists(fullPath))
        {
            if(Environment.OSVersion.Platform == PlatformID.Unix)
            {
                var dirInfo = new DirectoryInfo(fullPath);
                dirInfo.UnixFileMode = Consts.Mode0777;
            }

            return;
        }

        if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            Directory.CreateDirectory(fullPath, Consts.Mode0777);
        }
        else
        {
            Directory.CreateDirectory(fullPath);
        }
    }

    private static string GetExeDirectory()
    {
        var exeFileName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
        return Path.GetDirectoryName(exeFileName);
    }
}