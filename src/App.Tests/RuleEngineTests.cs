using System;
using ComplianceAssistant.Data;
using ComplianceAssistant.Models;
using ComplianceAssistant.Rules;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ComplianceAssistant.App.Tests;

public class RuleEngineTests
{
    private AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public void JaccardSimilarityDetectsOverlap()
    {
        using var db = CreateDb();
        var engine = new RuleEngine(db);
        var score = engine.CalculateJaccard("hello world example", "hello world sample");
        Assert.True(score > 0);
    }

    [Fact]
    public void RuleEvaluationReturnsHits()
    {
        using var db = CreateDb();
        var engine = new RuleEngine(db);
        var rules = new[] { new RuleDescriptor("a", "test", "hit", RuleSeverity.High, c => c.Contains("alert")) };
        var hits = engine.Evaluate("alert message", rules);
        Assert.Single(hits);
    }
}
