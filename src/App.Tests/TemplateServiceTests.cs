using System.Collections.Generic;
using ComplianceAssistant.Services;
using Xunit;

namespace ComplianceAssistant.App.Tests;

public class TemplateServiceTests
{
    [Fact]
    public void ApplyTemplateReplacesVariables()
    {
        var svc = new TemplateService();
        var output = svc.ApplyTemplate("Hello {{name}}", new Dictionary<string, string> { { "name", "World" } });
        Assert.Equal("Hello World", output);
    }
}
