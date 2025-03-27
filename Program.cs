using HydroNode;
using HydroNode.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// 설정파일 로딩
builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();


builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<DatabaseService>();
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

var host = builder.Build();
host.Run();
