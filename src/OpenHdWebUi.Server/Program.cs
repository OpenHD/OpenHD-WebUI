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
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(config.FilesFolder),
                RequestPath = "/Media"
            });

            app.UseRouting();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller}/{action=Index}/{id?}");

            app.MapFallbackToFile("index.html");

            app.Run();
        }
    }
}