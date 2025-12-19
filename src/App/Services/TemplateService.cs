using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ComplianceAssistant.Models;

namespace ComplianceAssistant.Services;

public interface ITemplateService
{
    string ApplyTemplate(string template, IDictionary<string, string> variables);
}

public class TemplateService : ITemplateService
{
    public string ApplyTemplate(string template, IDictionary<string, string> variables)
    {
        return Regex.Replace(template, "\\{\\{(.*?)\\}\\}", match =>
        {
            var key = match.Groups[1].Value.Trim();
            return variables.TryGetValue(key, out var value) ? value : match.Value;
        });
    }
}
