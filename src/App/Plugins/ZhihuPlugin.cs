using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using ComplianceAssistant.Models;
using ComplianceAssistant.Services;
using ComplianceAssistant.Rules;

namespace ComplianceAssistant.Plugins;

public class ZhihuPlugin : IPlatformPlugin
{
    private readonly IClipboardService _clipboardService;
    private readonly IExportService _exportService;
    private readonly ILogService _logService;

    public ZhihuPlugin(IClipboardService clipboardService, IExportService exportService, ILogService logService)
    {
        _clipboardService = clipboardService;
        _exportService = exportService;
        _logService = logService;
    }

    public string Id => "zhihu";
    public string DisplayName => "知乎";

    public IEnumerable<PluginAction> GetActions(TaskContext ctx)
    {
        var actions = new List<PluginAction>();
        if (!string.IsNullOrWhiteSpace(ctx.Task.TargetUrl))
        {
            actions.Add(new PluginAction("打开目标页", PluginActionType.OpenUrl, () =>
            {
                Process.Start(new ProcessStartInfo(ctx.Task.TargetUrl!) { UseShellExecute = true });
                _logService.Write("Plugin", $"Open Zhihu target {ctx.Task.TargetUrl}");
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
            new RuleDescriptor("zhihu_links", "外链提示", "外链需标注来源并确保可信", RuleSeverity.Medium, content =>
                content.Contains("http://") || content.Contains("https://")),
            new RuleDescriptor("zhihu_marketing", "营销话术风险", "避免夸大性宣传，保持中立表述", RuleSeverity.High, content =>
                content.Contains("最好的") || content.Contains("唯一"))
        };
    }

    public PlatformFieldSchema GetSchema() => new("目标链接", true, "请粘贴回答/文章/评论的链接或入口");

    private static string GetExportFolder()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ComplianceExports");
        Directory.CreateDirectory(path);
        return path;
    }
}
