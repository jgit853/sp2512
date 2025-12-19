namespace ComplianceAssistant.ViewModels;

public class TabItemViewModel
{
    public string Header { get; }
    public object Content { get; }

    public TabItemViewModel(string header, object content)
    {
        Header = header;
        Content = content;
    }
}
