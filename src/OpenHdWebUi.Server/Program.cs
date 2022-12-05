using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

using OpenHdWebUi.Server.Configuration;
using OpenHdWebUi.Server.Services;

namespace OpenHdWebUi.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Host.UseSystemd();
            builder.Services
                .AddOptions<ServiceConfiguration>()
                .Bind(builder.Configuration);
            builder.Services
                .AddScoped<SystemControlService>();

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
                RequestPath = "/media"
            });

            app.UseRouting();

            app.MapControllerRoute(
                name: "default",
                pattern: "api/{controller}/{action=Index}/{id?}");

            app.MapFallbackToFile("index.html");

            app.Run();
        }

        private static void EnsureFolderCreated(string fullPath)
        {
            if (Directory.Exists(fullPath))
            {
                return;
            }

            Directory.CreateDirectory(
                fullPath,
                UnixFileMode.GroupRead |
                UnixFileMode.GroupWrite |
                UnixFileMode.OtherRead |
                UnixFileMode.OtherWrite |
                UnixFileMode.UserRead |
                UnixFileMode.UserWrite);
        }
    }
}