using System.Windows;

namespace ComplianceAssistant.Services;

public interface IClipboardService
{
    void CopyText(string text, bool plainText = false);
}

public class ClipboardService : IClipboardService
{
    public void CopyText(string text, bool plainText = false)
    {
        if (plainText)
        {
            Clipboard.SetText(text, TextDataFormat.Text);
        }
        else
        {
            Clipboard.SetText(text, TextDataFormat.UnicodeText);
        }
    }
}
