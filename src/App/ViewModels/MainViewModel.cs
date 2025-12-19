using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ComplianceAssistant.Data;
using ComplianceAssistant.Models;
using ComplianceAssistant.Plugins;
using ComplianceAssistant.Rules;
using ComplianceAssistant.Services;
using ComplianceAssistant.Views;
using Microsoft.EntityFrameworkCore;

namespace ComplianceAssistant.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly AppDbContext _dbContext;
    private readonly IRuleEngine _ruleEngine;
    private readonly IPluginRegistry _pluginRegistry;
    private readonly IClipboardService _clipboardService;
    private readonly IExportService _exportService;
    private readonly ILogService _logService;
    private readonly ITemplateService _templateService;
    private readonly IScheduleService _scheduleService;

    [ObservableProperty]
    private ObservableCollection<TaskViewModel> tasks = new();

    [ObservableProperty]
    private TaskViewModel? selectedTask;

    [ObservableProperty]
    private ObservableCollection<TabItemViewModel> tabs = new();

    [ObservableProperty]
    private TabItemViewModel? selectedTab;

    [ObservableProperty]
    private string? draftFeedback;

    [ObservableProperty]
    private string statusMessage = "已开启人工确认模式，禁止自动提交";

    public MainViewModel(AppDbContext dbContext, IRuleEngine ruleEngine, IPluginRegistry pluginRegistry,
        IClipboardService clipboardService, IExportService exportService, ILogService logService, ITemplateService templateService, IScheduleService scheduleService)
    {
        _dbContext = dbContext;
        _ruleEngine = ruleEngine;
        _pluginRegistry = pluginRegistry;
        _clipboardService = clipboardService;
        _exportService = exportService;
        _logService = logService;
        _templateService = templateService;
        _scheduleService = scheduleService;

        InitializeTabs();
        EnsureSeedData();
        LoadTasks();
    }

    private void InitializeTabs()
    {
        Tabs = new ObservableCollection<TabItemViewModel>
        {
            new("任务列表", new TaskListView { DataContext = this }),
            new("草稿与质检", new TaskDetailView { DataContext = this }),
            new("排期", new ScheduleView { DataContext = this }),
            new("设置", new SettingsView { DataContext = this })
        };
        SelectedTab = Tabs.FirstOrDefault();
    }

    private void EnsureSeedData()
    {
        _dbContext.Database.EnsureCreated();
        if (!_dbContext.Platforms.Any())
        {
            var zhihu = new Platform { Name = "知乎", PluginId = "zhihu", Notes = "仅人工发布" };
            var generic = new Platform { Name = "自定义网页", PluginId = "generic-web" };
            _dbContext.Platforms.AddRange(zhihu, generic);
            _dbContext.SaveChanges();
            _dbContext.Accounts.Add(new Account { DisplayName = "默认账号", PlatformId = zhihu.Id });
            _dbContext.SaveChanges();
        }
    }

    private void LoadTasks()
    {
        var list = _dbContext.ContentTasks
            .Include(t => t.Account)
            .Include(t => t.Platform)
            .Include(t => t.Drafts)
            .OrderByDescending(t => t.CreatedAt)
            .ToList();

        Tasks = new ObservableCollection<TaskViewModel>(list.Select(t =>
        {
            var vm = new TaskViewModel(t);
            vm.RefreshDrafts();
            return vm;
        }));

        SelectedTask = Tasks.FirstOrDefault();
    }

    [RelayCommand]
    private void CreateTask()
    {
        var platform = _dbContext.Platforms.First();
        var account = _dbContext.Accounts.First();
        var task = new ContentTask
        {
            PlatformId = platform.Id,
            AccountId = account.Id,
            TaskType = TaskType.Post,
            Title = "新任务",
            Keywords = "",
            Status = TaskStatus.Draft,
            TargetUrl = "https://www.zhihu.com/question"
        };
        _dbContext.ContentTasks.Add(task);
        _dbContext.SaveChanges();

        var vm = new TaskViewModel(task);
        Tasks.Insert(0, vm);
        SelectedTask = vm;
    }

    [RelayCommand]
    private void SaveDraft()
    {
        if (SelectedTask == null)
            return;

        var draft = new DraftVersion
        {
            ContentTaskId = SelectedTask.Task.Id,
            Markdown = SelectedTask.CurrentDraft,
            Html = null,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Environment.UserName
        };
        _dbContext.DraftVersions.Add(draft);
        _dbContext.SaveChanges();
        SelectedTask.Task.Drafts.Add(draft);
        SelectedTask.RefreshDrafts();
        _logService.Write("Draft", "保存草稿", draft.Id.ToString());
    }

    [RelayCommand]
    private void RunChecks()
    {
        if (SelectedTask == null)
            return;

        var ruleList = new List<RuleDescriptor>
        {
            new("safety_manual", "人工确认", "禁止自动提交，发布需人工操作", RuleSeverity.High, _ => false),
            new("medical_claims", "医疗风险示例", "避免夸大疗效、避免诊断性表述", RuleSeverity.Medium, content =>
                content.Contains("治愈") || content.Contains("百分之百"))
        };

        var plugin = _pluginRegistry.Get(SelectedTask.Task.Platform?.PluginId ?? string.Empty);
        if (plugin != null)
        {
            ruleList.AddRange(plugin.GetRules());
        }

        var hits = _ruleEngine.Evaluate(SelectedTask.CurrentDraft, ruleList);
        SelectedTask.RuleHits = new ObservableCollection<string>(hits.Select(h => $"[{h.Severity}] {h.Title}: {h.Message}"));
        DraftFeedback = SelectedTask.RuleHits.Any() ? string.Join("\n", SelectedTask.RuleHits) : "未命中风险";

        var similar = _ruleEngine.IsSimilarToRecent(SelectedTask.Task.AccountId, SelectedTask.CurrentDraft, 30, 0.6);
        if (similar)
        {
            DraftFeedback += "\n存在与近30天草稿高度相似内容，请复核。";
        }
    }

    [RelayCommand]
    private void CopyDraft()
    {
        if (SelectedTask?.Versions.FirstOrDefault() is { } latest)
        {
            _clipboardService.CopyText(latest.Markdown);
            _logService.Write("Copy", "复制草稿", latest.Id.ToString());
            StatusMessage = "草稿已复制，需人工在目标页面粘贴发布";
        }
    }

    [RelayCommand]
    private void ExportDraft()
    {
        if (SelectedTask?.Versions.FirstOrDefault() is { } latest)
        {
            var folder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ComplianceExports");
            var md = _exportService.ExportMarkdown(latest, folder);
            var html = _exportService.ExportHtml(latest, folder);
            StatusMessage = $"已导出: {md} / {html}";
            _logService.Write("Export", "导出草稿", md);
        }
    }

    [RelayCommand]
    private void ApplyTemplate()
    {
        if (SelectedTask == null)
            return;

        var variables = new Dictionary<string, string>
        {
            ["company"] = "示例公司",
            ["date"] = DateTime.Now.ToString("yyyy-MM-dd"),
            ["keyword"] = SelectedTask.Task.Keywords
        };
        var template = "# {{company}} 内容草稿\n关键词：{{keyword}}\n日期：{{date}}\n---\n请在此撰写草稿";
        SelectedTask.CurrentDraft = _templateService.ApplyTemplate(template, variables);
    }

    [RelayCommand]
    private void Schedule()
    {
        if (SelectedTask == null)
            return;

        var account = _dbContext.Accounts.First(a => a.Id == SelectedTask.Task.AccountId);
        var desiredTime = DateTime.Now.AddHours(1);
        if (_pluginRegistry.Get(account.Platform?.PluginId ?? SelectedTask.Task.Platform?.PluginId ?? string.Empty) == null)
        {
            StatusMessage = "未找到对应插件，无法排期";
            return;
        }

        if (!_scheduleService.CanSchedule(account, desiredTime, out var reason))
        {
            StatusMessage = $"排期被阻止：{reason}";
            return;
        }

        SelectedTask.Task.ScheduledAt = desiredTime;
        SelectedTask.Task.Status = TaskStatus.Scheduled;
        _dbContext.SaveChanges();
        StatusMessage = $"已排期 {desiredTime:yyyy-MM-dd HH:mm}，到点将弹窗提醒并打开页面供人工发布";
    }

    [RelayCommand]
    private void ExecutePluginAction(string actionName)
    {
        if (SelectedTask == null)
            return;

        var plugin = _pluginRegistry.Get(SelectedTask.Task.Platform?.PluginId ?? string.Empty);
        var latest = SelectedTask.Versions.FirstOrDefault();
        if (plugin == null)
            return;

        var ctx = new TaskContext(SelectedTask.Task, latest, _dbContext.Accounts.First(a => a.Id == SelectedTask.Task.AccountId));
        var actions = plugin.GetActions(ctx);
        PluginAction? action = actionName switch
        {
            "打开目标页" => actions.FirstOrDefault(a => a.Type == PluginActionType.OpenUrl),
            "复制草稿" => actions.FirstOrDefault(a => a.Type == PluginActionType.CopyText),
            "导出 Markdown" => actions.FirstOrDefault(a => a.Name.Contains("Markdown")),
            _ => actions.FirstOrDefault(a => a.Name == actionName)
        };
        action?.Execute();
    }
}
