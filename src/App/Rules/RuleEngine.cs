using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComplianceAssistant.Data;
using ComplianceAssistant.Models;
using Microsoft.EntityFrameworkCore;

namespace ComplianceAssistant.Rules;

public enum RuleSeverity
{
    Low,
    Medium,
    High
}

public record RuleResult(string RuleId, string Title, RuleSeverity Severity, string Message);

public record RuleDescriptor(string RuleId, string Title, string Message, RuleSeverity Severity, Func<string, bool> Evaluate);

public interface IRuleEngine
{
    IEnumerable<RuleResult> Evaluate(string content, IEnumerable<RuleDescriptor> rules);
    double CalculateJaccard(string a, string b, int shingleSize = 4);
    bool IsSimilarToRecent(Guid accountId, string content, int days, double threshold);
}

public class RuleEngine : IRuleEngine
{
    private readonly AppDbContext _dbContext;

    public RuleEngine(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IEnumerable<RuleResult> Evaluate(string content, IEnumerable<RuleDescriptor> rules)
    {
        var hits = new List<RuleResult>();
        foreach (var rule in rules)
        {
            if (rule.Evaluate(content))
            {
                hits.Add(new RuleResult(rule.RuleId, rule.Title, rule.Severity, rule.Message));
            }
        }
        return hits;
    }

    public double CalculateJaccard(string a, string b, int shingleSize = 4)
    {
        var setA = Shingles(a, shingleSize);
        var setB = Shingles(b, shingleSize);
        var intersection = setA.Intersect(setB).Count();
        var union = setA.Union(setB).Count();
        return union == 0 ? 0 : (double)intersection / union;
    }

    public bool IsSimilarToRecent(Guid accountId, string content, int days, double threshold)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        var drafts = _dbContext.DraftVersions
            .Include(d => d.ContentTask)
            .Where(d => d.ContentTask != null && d.ContentTask.AccountId == accountId && d.CreatedAt >= since)
            .ToList();

        foreach (var draft in drafts)
        {
            var score = CalculateJaccard(content, draft.Markdown);
            if (score >= threshold)
            {
                return true;
            }
        }
        return false;
    }

    private static HashSet<string> Shingles(string text, int size)
    {
        text = text.Replace("\r", "");
        var tokens = text.Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var shingles = new HashSet<string>();
        for (var i = 0; i <= tokens.Length - size; i++)
        {
            shingles.Add(string.Join(' ', tokens.Skip(i).Take(size)));
        }
        return shingles;
    }
}
