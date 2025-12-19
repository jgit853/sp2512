using ComplianceAssistant.Models;
using Microsoft.EntityFrameworkCore;

namespace ComplianceAssistant.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Platform> Platforms => Set<Platform>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<ContentTask> ContentTasks => Set<ContentTask>();
    public DbSet<DraftVersion> DraftVersions => Set<DraftVersion>();
    public DbSet<RuleSet> RuleSets => Set<RuleSet>();
    public DbSet<Template> Templates => Set<Template>();
    public DbSet<LogEntry> Logs => Set<LogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Platform>()
            .HasMany(p => p.Accounts)
            .WithOne(a => a.Platform!)
            .HasForeignKey(a => a.PlatformId);

        modelBuilder.Entity<ContentTask>()
            .HasOne(t => t.Platform)
            .WithMany()
            .HasForeignKey(t => t.PlatformId);

        modelBuilder.Entity<ContentTask>()
            .HasOne(t => t.Account)
            .WithMany()
            .HasForeignKey(t => t.AccountId);

        modelBuilder.Entity<ContentTask>()
            .HasMany(t => t.Drafts)
            .WithOne(d => d.ContentTask!)
            .HasForeignKey(d => d.ContentTaskId);
    }
}
