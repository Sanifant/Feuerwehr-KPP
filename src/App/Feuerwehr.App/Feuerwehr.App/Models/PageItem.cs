using System;

namespace De.Openelp.Feuerwehr.App.Models
{
    /// <summary>
    /// Represents a page entry with a header, content factory, and optional icon data.
    /// </summary>
    /// <param name="header">The label displayed for this navigation page.</param>
    /// <param name="factory">The factory used to construct the navigation page content.</param>
    /// <param name="iconData">The vector path data used to render the page icon.</param>
    /// <param name="isVisible">An optional predicate determining whether the navigation page is visible.</param>
    public class PageItem(string header, Func<object> factory, string? iconData = null, Func<bool>? isVisible = null)
    {
        private Func<bool> _isVisible = isVisible ?? (() => true);

        /// <summary>
        /// Gets the text shown for the navigation page.
        /// </summary>
        /// <value> The header. </value>
        public string Header { get; } = header;

        /// <summary>
        /// Gets the factory that creates the navigation page content.
        /// </summary>
        /// <value> The factory. </value>
        public Func<object> Factory { get; } = factory;

        /// <summary>
        /// Gets the vector path data used to render the page icon.
        /// </summary>
        /// <value> The icon data. </value>
        public string? IconData { get; } = iconData;

        /// <summary>
        /// Gets a value indicating whether the navigation item is visible to the current user.
        /// </summary>
        /// <value><c>true</c> if this <see cref="PageItem" /> is visible; otherwise, <c>false</c>. </value>
        public bool IsVisible { get => this._isVisible(); }
    }
}
