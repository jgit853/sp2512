using System;
using System.Collections.Generic;
using ComplianceAssistant.Models;

namespace ComplianceAssistant.Plugins;

public enum PluginActionType
{
    OpenUrl,
    CopyText,
    Export,
    ApiPublish
}

public record PluginAction(string Name, PluginActionType Type, Action Execute);

public record PlatformFieldSchema(string TaskUrlLabel, bool RequiresTargetUrl, string HelpText);

public record TaskContext(ContentTask Task, DraftVersion? LatestDraft, Account Account);

public interface IPlatformPlugin
{
    string Id { get; }
    string DisplayName { get; }
    IEnumerable<PluginAction> GetActions(TaskContext ctx);
    IEnumerable<RuleDescriptor> GetRules();
    PlatformFieldSchema GetSchema();
}

public interface IPluginRegistry
{
    void Register(IPlatformPlugin plugin);
    IEnumerable<IPlatformPlugin> All { get; }
    IPlatformPlugin? Get(string id);
}

public class PluginRegistry : IPluginRegistry
{
    private readonly Dictionary<string, IPlatformPlugin> _plugins = new();

    public IEnumerable<IPlatformPlugin> All => _plugins.Values;

    public void Register(IPlatformPlugin plugin)
    {
        if (!_plugins.ContainsKey(plugin.Id))
        {
            _plugins.Add(plugin.Id, plugin);
        }
    }

    public IPlatformPlugin? Get(string id) => _plugins.TryGetValue(id, out var plugin) ? plugin : null;
}
