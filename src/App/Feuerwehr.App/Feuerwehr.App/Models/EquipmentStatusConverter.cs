using System;
using System.Globalization;
using Avalonia.Data.Converters;
using de.openelp.feuerwehr.Common.Models.Inventory;

namespace de.openelp.feuerwehr.App.Models;

public sealed class EquipmentStatusConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        EquipmentStatus.Ready => "Einsatzklar", EquipmentStatus.Defective => "Defekt", EquipmentStatus.InRepair => "In Reparatur", _ => "",
    };
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
