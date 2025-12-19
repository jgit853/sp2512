using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using ComplianceAssistant.Models;
using ComplianceAssistant.Services;
using ComplianceAssistant.Rules;

namespace ComplianceAssistant.Plugins;

public class GenericWebPlugin : IPlatformPlugin
{
    private readonly IClipboardService _clipboardService;
    private readonly IExportService _exportService;
    private readonly ILogService _logService;

    public GenericWebPlugin(IClipboardService clipboardService, IExportService exportService, ILogService logService)
    {
        _clipboardService = clipboardService;
        _exportService = exportService;
        _logService = logService;
    }

    public string Id => "generic-web";
    public string DisplayName => "通用网页平台";

    public IEnumerable<PluginAction> GetActions(TaskContext ctx)
    {
        var actions = new List<PluginAction>();
        if (!string.IsNullOrWhiteSpace(ctx.Task.TargetUrl))
        {
            actions.Add(new PluginAction("打开入口", PluginActionType.OpenUrl, () =>
            {
                Process.Start(new ProcessStartInfo(ctx.Task.TargetUrl!) { UseShellExecute = true });
                _logService.Write("Plugin", $"Open generic url {ctx.Task.TargetUrl}");
            }));
        }

        if (ctx.LatestDraft != null)
        {
            actions.Add(new PluginAction("复制草稿", PluginActionType.CopyText, () =>
            {
                _clipboardService.CopyText(ctx.LatestDraft.Markdown);
                _logService.Write("Plugin", "Copy draft to clipboard");
            }));
            actions.Add(new PluginAction("导出 Markdown", PluginActionType.Export, () =>
            {
                var path = _exportService.ExportMarkdown(ctx.LatestDraft, GetExportFolder());
                _logService.Write("Plugin", $"Export markdown {path}");
            }));
            actions.Add(new PluginAction("导出 HTML", PluginActionType.Export, () =>
            {
                var path = _exportService.ExportHtml(ctx.LatestDraft, GetExportFolder());
                _logService.Write("Plugin", $"Export html {path}");
            }));
        }

        return actions;
    }

    public IEnumerable<RuleDescriptor> GetRules()
    {
        return new[]
        {
            new RuleDescriptor("generic_copyright", "版权提示", "请确认素材来源合法且可用", RuleSeverity.Low, _ => false),
            new RuleDescriptor("generic_safety", "合规提示", "禁止自动提交和刷屏，所有发布需人工操作", RuleSeverity.High, _ => false)
        };
    }

    public PlatformFieldSchema GetSchema() => new("目标链接", false, "在排期到点后会打开该链接以便人工粘贴发布");

    private static string GetExportFolder()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ComplianceExports");
        Directory.CreateDirectory(path);
        return path;
    }
}
