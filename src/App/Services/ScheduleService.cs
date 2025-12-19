using System;
using System.Collections.Generic;
using System.Linq;
using ComplianceAssistant.Data;
using ComplianceAssistant.Models;
using Microsoft.EntityFrameworkCore;

namespace ComplianceAssistant.Services;

public interface IScheduleService
{
    bool CanSchedule(Account account, DateTime desiredTime, out string reason);
}

public class ScheduleService : IScheduleService
{
    private readonly AppDbContext _dbContext;

    public ScheduleService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public bool CanSchedule(Account account, DateTime desiredTime, out string reason)
    {
        var startOfDay = desiredTime.Date;
        var endOfDay = startOfDay.AddDays(1);
        var tasksToday = _dbContext.ContentTasks
            .Where(t => t.AccountId == account.Id && t.ScheduledAt >= startOfDay && t.ScheduledAt < endOfDay)
            .Count();

        if (tasksToday >= account.DailyLimit)
        {
            reason = $"已达到账号每日上限 {account.DailyLimit}";
            return false;
        }

        var lastScheduled = _dbContext.ContentTasks
            .Where(t => t.AccountId == account.Id && t.ScheduledAt != null)
            .OrderByDescending(t => t.ScheduledAt)
            .Select(t => t.ScheduledAt)
            .FirstOrDefault();

        if (lastScheduled.HasValue && desiredTime - lastScheduled.Value < TimeSpan.FromMinutes(account.MinimumIntervalMinutes))
        {
            reason = $"未满足最短间隔 {account.MinimumIntervalMinutes} 分钟";
            return false;
        }

        reason = string.Empty;
        return true;
    }
}
