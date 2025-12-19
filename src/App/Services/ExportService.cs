using System;
using System.IO;
using System.Text;
using ComplianceAssistant.Models;

namespace ComplianceAssistant.Services;

public interface IExportService
{
    string ExportMarkdown(DraftVersion draft, string directory);
    string ExportHtml(DraftVersion draft, string directory);
}

public class ExportService : IExportService
{
    public string ExportMarkdown(DraftVersion draft, string directory)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"{SanitizeFileName(draft.Id.ToString())}.md");
        File.WriteAllText(path, draft.Markdown, Encoding.UTF8);
        return path;
    }

    public string ExportHtml(DraftVersion draft, string directory)
    {
        Directory.CreateDirectory(directory);
        var htmlContent = draft.Html ?? Markdown.ToHtml(draft.Markdown);
        var path = Path.Combine(directory, $"{SanitizeFileName(draft.Id.ToString())}.html");
        File.WriteAllText(path, htmlContent, Encoding.UTF8);
        return path;
    }

    private static string SanitizeFileName(string value)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(c, '_');
        }
        return value;
    }
}

internal static class Markdown
{
    // Minimal markdown to HTML converter placeholder for MVP
    public static string ToHtml(string markdown)
    {
        var builder = new StringBuilder();
        builder.Append("<html><body>");
        foreach (var line in markdown.Split('\n'))
        {
            if (line.StartsWith("# "))
            {
                builder.Append("<h1>").Append(System.Net.WebUtility.HtmlEncode(line[2..])).Append("</h1>");
            }
            else if (line.StartsWith("## "))
            {
                builder.Append("<h2>").Append(System.Net.WebUtility.HtmlEncode(line[3..])).Append("</h2>");
            }
            else
            {
                builder.Append("<p>").Append(System.Net.WebUtility.HtmlEncode(line)).Append("</p>");
            }
        }
        builder.Append("</body></html>");
        return builder.ToString();
    }
}
