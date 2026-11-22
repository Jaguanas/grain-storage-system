using System.Net;
using StorageCore;
using NLog;

var loggingConfiguration = new NLog.Config.LoggingConfiguration();

var consoleTarget = new NLog.Targets.ConsoleTarget("console")
{
    Layout = @"${date:format=HH\:mm\:ss}|${level}| ${message} ${exception}"
};

loggingConfiguration.AddTarget(consoleTarget);
loggingConfiguration.AddRuleForAllLevels(consoleTarget);

LogManager.Configuration = loggingConfiguration;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(IPAddress.Loopback, 5002);
});

// Enable gRPC
builder.Services.AddGrpc();
builder.Services.AddSingleton<IStorageLogic, StorageLogic>();

var app = builder.Build();

app.MapGrpcService<StorageGrpc.StorageGrpcService>();

app.Run();
