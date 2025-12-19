using System;
using System.IO;
using System.Reflection;
using System.Windows;
using ComplianceAssistant.Data;
using ComplianceAssistant.Services;
using ComplianceAssistant.Plugins;
using ComplianceAssistant.Rules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace ComplianceAssistant;

public partial class App : Application
{
    private IHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .WriteTo.Async(c => c.File(Path.Combine(AppContext.BaseDirectory, "logs", "app-.log"), rollingInterval: RollingInterval.Day))
            .CreateLogger();

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<IConfiguration>(configuration);
                services.AddDbContext<AppDbContext>(options =>
                {
                    var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ComplianceAssistant", "app.db");
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    options.UseSqlite($"Data Source={path}");
                });
                services.AddSingleton<IRuleEngine, RuleEngine>();
                services.AddSingleton<IScheduleService, ScheduleService>();
                services.AddSingleton<IClipboardService, ClipboardService>();
                services.AddSingleton<IExportService, ExportService>();
                services.AddSingleton<ILogService, LogService>();
                services.AddSingleton<ITemplateService, TemplateService>();
                services.AddSingleton<IPluginRegistry, PluginRegistry>();
                services.AddSingleton<MainWindow>();
                services.AddSingleton<ViewModels.MainViewModel>();
            })
            .UseSerilog()
            .Build();

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();

        var pluginRegistry = scope.ServiceProvider.GetRequiredService<IPluginRegistry>();
        var clipboard = scope.ServiceProvider.GetRequiredService<IClipboardService>();
        var export = scope.ServiceProvider.GetRequiredService<IExportService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogService>();

        pluginRegistry.Register(new ZhihuPlugin(clipboard, export, logger));
        pluginRegistry.Register(new GenericWebPlugin(clipboard, export, logger));

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.DataContext = _host.Services.GetRequiredService<ViewModels.MainViewModel>();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        _host?.Dispose();
        Log.CloseAndFlush();
    }
}
