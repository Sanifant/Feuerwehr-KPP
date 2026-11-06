using System;
using System.Globalization;
using Avalonia.Data.Converters;
using De.Openelp.Feuerwehr.Common.Models.Inventory;

namespace De.Openelp.Feuerwehr.App.Models
{
    /// <summary>
    /// Converts equipment condition values into the labels displayed by the Avalonia client.
    /// </summary>
    public sealed class EquipmentStatusConverter : IValueConverter
    {
        /// <summary>
        /// Converts an equipment status to its user-facing display label.
        /// </summary>
        /// <param name="value">The object? value to process.</param>
        /// <param name="targetType">The type expected by the binding target.</param>
        /// <param name="parameter">An optional converter parameter.</param>
        /// <param name="culture">The culture used to format the converted value.</param>
        /// <returns>The display label for the supplied equipment status.</returns>
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value switch
            {
                EquipmentStatus.Ready => "Einsatzklar",
                EquipmentStatus.Defective => "Defekt",
                EquipmentStatus.InRepair => "In Reparatur",
                _ => string.Empty,
            };
        }

        /// <summary>
        /// Rejects reverse conversion because status labels are intended for display only.
        /// </summary>
        /// <param name="value">The object? value to process.</param>
        /// <param name="targetType">The type expected by the binding target.</param>
        /// <param name="parameter">An optional converter parameter.</param>
        /// <param name="culture">The culture used to format the converted value.</param>
        /// <returns>No value; reverse conversion is unsupported and throws an exception.</returns>
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
