using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using ZgrzytDesktop.ViewModels;

namespace ZgrzytDesktop;

/// <summary>
/// Given a view model, returns the corresponding view if possible.
/// </summary>
[RequiresUnreferencedCode(
    "Default implementation of ViewLocator involves reflection which may be trimmed away.",
    Url = "https://docs.avaloniaui.net/docs/concepts/view-locator")]
public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        var viewName = param.GetType().FullName!
            .Replace(".ViewModels.", ".Views.", StringComparison.Ordinal)
            .Replace("ViewModel", "View", StringComparison.Ordinal);

        var type = typeof(ViewLocator).Assembly.GetType(viewName);

        if (type is not null)
        {
            var control = (Control)Activator.CreateInstance(type)!;
            control.DataContext = param;
            return control;
        }

        return new TextBlock { Text = "Not Found: " + viewName };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}