using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using ComplianceAssistant.Models;

namespace ComplianceAssistant.ViewModels;

public partial class TaskViewModel : ObservableObject
{
    [ObservableProperty]
    private ContentTask task;

    [ObservableProperty]
    private string currentDraft = string.Empty;

    [ObservableProperty]
    private ObservableCollection<DraftVersion> versions = new();

    [ObservableProperty]
    private ObservableCollection<string> ruleHits = new();

    public TaskViewModel(ContentTask task)
    {
        this.task = task;
    }

    public void RefreshDrafts()
    {
        Versions = new ObservableCollection<DraftVersion>(Task.Drafts.OrderByDescending(d => d.CreatedAt));
        CurrentDraft = Versions.FirstOrDefault()?.Markdown ?? string.Empty;
    }
}
