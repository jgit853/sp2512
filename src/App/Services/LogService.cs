using System;
using ComplianceAssistant.Data;
using ComplianceAssistant.Models;

namespace ComplianceAssistant.Services;

public interface ILogService
{
    void Write(string category, string message, string? data = null);
}

public class LogService : ILogService
{
    private readonly AppDbContext _dbContext;

    public LogService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Write(string category, string message, string? data = null)
    {
        var entry = new LogEntry
        {
            Category = category,
            Message = message,
            Data = data,
            Timestamp = DateTime.UtcNow
        };
        _dbContext.Logs.Add(entry);
        _dbContext.SaveChanges();
    }
}
