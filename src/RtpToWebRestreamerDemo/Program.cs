// See https://aka.ms/new-console-template for more information

using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenHdWebUi.RtpToWebRestreamer;

Console.WriteLine("Hello, World!");
IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(builder => builder
        .AddConsole()
        .SetMinimumLevel(LogLevel.Trace))
    .Build();

var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();

var handler = new RtpRestreamer(
    new IPEndPoint(IPAddress.Any, 8081),
    new IPEndPoint(IPAddress.Any, 5600),
    loggerFactory);
handler.Start();

host.Run();