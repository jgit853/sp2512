using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ComplianceAssistant.Models;

public enum TaskStatus
{
    Draft,
    ReadyForReview,
    Scheduled,
    AwaitingManualPublish,
    Published,
    Archived
}

public enum TaskType
{
    Post,
    Reply
}

public class Platform
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(128)]
    public string PluginId { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public ICollection<Account> Accounts { get; set; } = new List<Account>();
}

public class Account
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(128)]
    public string DisplayName { get; set; } = string.Empty;

    public Guid PlatformId { get; set; }
    public Platform? Platform { get; set; }

    [MaxLength(256)]
    public string? EncryptedToken { get; set; }

    public int DailyLimit { get; set; } = 10;
    public int MinimumIntervalMinutes { get; set; } = 30;
}

public class ContentTask
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PlatformId { get; set; }
    public Platform? Platform { get; set; }

    public Guid AccountId { get; set; }
    public Account? Account { get; set; }

    public TaskType TaskType { get; set; }

    [MaxLength(512)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? TargetUrl { get; set; }

    public string Keywords { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DueAt { get; set; }
    public DateTime? ScheduledAt { get; set; }

    public TaskStatus Status { get; set; } = TaskStatus.Draft;

    public ICollection<DraftVersion> Drafts { get; set; } = new List<DraftVersion>();
}

public class DraftVersion
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ContentTaskId { get; set; }
    public ContentTask? ContentTask { get; set; }

    public string Markdown { get; set; } = string.Empty;
    public string? Html { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
}

public class RuleSet
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    public string Scope { get; set; } = "General";

    public string RulesJson { get; set; } = string.Empty;
}

public class Template
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string Variables { get; set; } = string.Empty;
}

public class LogEntry
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [MaxLength(64)]
    public string Category { get; set; } = string.Empty;

    [MaxLength(256)]
    public string Message { get; set; } = string.Empty;

    public string? Data { get; set; }
}
