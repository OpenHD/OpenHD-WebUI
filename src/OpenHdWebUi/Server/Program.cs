using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using OpenHdWebUi.Server.Configuration;
using OpenHdWebUi.Server.Services;

namespace OpenHdWebUi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Host.UseSystemd();
            // Add services to the container.
            builder.Services
                .AddOptions<ServiceConfiguration>()
                .Bind(builder.Configuration);
            builder.Services
                .AddScoped<SystemControlService>();

            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseCors(cors => cors
                .AllowAnyMethod()
                .AllowAnyHeader()
                .SetIsOriginAllowed(origin => true)
                .AllowCredentials()
            );

            app.MapRazorPages();
            app.MapControllers();
            app.MapFallbackToFile("index.html");

            var config = app.Services.GetRequiredService<IOptions<ServiceConfiguration>>().Value;
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(config.FilesFolder),
                RequestPath = "/Media"
            });

            app.Run();
        }
    }
}