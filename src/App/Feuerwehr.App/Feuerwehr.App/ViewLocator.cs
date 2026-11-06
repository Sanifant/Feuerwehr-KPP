using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using De.Openelp.Feuerwehr.App.ViewModels;

namespace De.Openelp.Feuerwehr.App
{
    /// <summary>
    /// Given a view model, returns the corresponding view if possible.
    /// </summary>
    [RequiresUnreferencedCode(
        "Default implementation of ViewLocator involves reflection which may be trimmed away.",
        Url = "https://docs.avaloniaui.net/docs/concepts/view-locator")]
    public class ViewLocator : IDataTemplate
    {
        /// <summary>
        /// Builds the operation.
        /// </summary>
        /// <param name="param">The view model for which to create a view.</param>
        /// <returns>The resolved view, or a diagnostic control if the matching view type cannot be found.</returns>
        public Control? Build(object? param)
        {
            if (param is null)
            {
                return null;
            }

            string name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
            Type? type = Type.GetType(name);

            if (type != null)
            {
                return (Control)Activator.CreateInstance(type)!;
            }

            return new TextBlock { Text = "Not Found: " + name };
        }

        /// <summary>
        /// Determines whether the supplied object is a view model supported by the view locator.
        /// </summary>
        /// <param name="data">The data object to test against the supported view-model types.</param>
        /// <returns>True if the data is a supported view model; otherwise, false.</returns>
        public bool Match(object? data)
        {
            return data is ViewModelBase;
        }
    }
}
