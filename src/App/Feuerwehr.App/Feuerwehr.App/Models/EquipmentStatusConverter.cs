using System;
using System.Globalization;
using Avalonia.Data.Converters;
using De.Openelp.Feuerwehr.Common.Models.Inventory;

namespace De.Openelp.Feuerwehr.App.Models
{
    public sealed class EquipmentStatusConverter : IValueConverter
    {
        /// <summary>
        /// Executes convert.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="targetType">The target Type.</param>
        /// <param name="parameter">The parameter.</param>
        /// <param name="culture">The culture.</param>
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
        {
            EquipmentStatus.Ready => "Einsatzklar", EquipmentStatus.Defective => "Defekt", EquipmentStatus.InRepair => "In Reparatur", _ => "",
        };
        /// <summary>
        /// Executes convert Back.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="targetType">The target Type.</param>
        /// <param name="parameter">The parameter.</param>
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }

}
