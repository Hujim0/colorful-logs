// using System;
// using System.Globalization;
// using Avalonia.Data.Converters;

// namespace colorfulLogs.Desktop.Converters
// {
//     public class IntToBoolConverter : IValueConverter
//     {
//         public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture)
//         {
//             // Handle null value case
//             if (value is null) return false;

//             // Try to parse value to int
//             if (value is not int intValue)
//             {
//                 if (value is IConvertible convertible)
//                 {
//                     try { intValue = convertible.ToInt32(CultureInfo.InvariantCulture); }
//                     catch { return false; }
//                 }
//                 else
//                 {
//                     return false;
//                 }
//             }

//             // Handle parameter
//             if (parameter is string paramString &&
//                 int.TryParse(paramString, NumberStyles.Integer, CultureInfo.InvariantCulture, out int paramInt))
//             {
//                 return intValue > paramInt;
//             }

//             return intValue > 0;
//         }

//         public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo? culture)
//         {
//             throw new NotSupportedException("ConvertBack is not supported for IntToBoolConverter");
//         }
//     }
// }