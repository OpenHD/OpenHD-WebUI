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
            .AddScoped<SystemFilesService>();
        builder.Services
            .AddSingleton<MediaService>()
            .AddSingleton<AirGroundService>();
        builder.Services
            .AddDirectoryBrowser()
            .AddControllers();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
        }

        app.UseDefaultFiles();
        app.MapStaticAssets();

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