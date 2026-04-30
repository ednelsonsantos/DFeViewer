using DFeViewer.Models;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace DFeViewer.Converters;

/// <summary>Retorna Visible se o valor for zero (para mostrar placeholder de lista vazia)</summary>
public class ZeroToVisibleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => (value is int n && n == 0) ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

/// <summary>Retorna Visible se o valor for diferente de zero</summary>
public class NonZeroToVisibleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => (value is int n && n > 0) ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

/// <summary>bool → Visibility (true = Visible)</summary>
public class BoolToVisibleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => (value is bool b && b) ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

/// <summary>bool → Visibility (true = Collapsed — inverso)</summary>
public class BoolToInvisibleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => (value is bool b && b) ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

/// <summary>TipoDFe → cor de fundo do badge</summary>
public class TipoToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is TipoDFe tipo
            ? tipo switch
            {
                TipoDFe.NFe  => new SolidColorBrush(Color.FromRgb(21, 101, 192)),  // azul
                TipoDFe.NFCe => new SolidColorBrush(Color.FromRgb(0, 137, 123)),   // teal
                TipoDFe.CTe  => new SolidColorBrush(Color.FromRgb(230, 81, 0)),    // laranja
                TipoDFe.MDFe => new SolidColorBrush(Color.FromRgb(123, 31, 162)),  // roxo
                _            => new SolidColorBrush(Colors.Gray)
            }
            : new SolidColorBrush(Colors.Gray);
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

/// <summary>Situação string → cor (verde = autorizado, laranja = outros)</summary>
public class SituacaoToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var situacao = value?.ToString() ?? string.Empty;
        return situacao.Contains("Autoriza", StringComparison.OrdinalIgnoreCase)
            ? new SolidColorBrush(Color.FromRgb(46, 125, 50))   // verde
            : situacao.Contains("Erro", StringComparison.OrdinalIgnoreCase)
                ? new SolidColorBrush(Color.FromRgb(183, 28, 28)) // vermelho
                : new SolidColorBrush(Color.FromRgb(230, 81, 0)); // laranja
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

/// <summary>Caminho completo → apenas nome do arquivo</summary>
public class NomeArquivoConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is string path ? System.IO.Path.GetFileName(path) : value;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

/// <summary>IEnumerable de ItemNFe → soma do ValorTotal</summary>
public class SomaTotalConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is IEnumerable<ItemNFe> itens)
            return itens.Sum(i => i.ValorTotal);
        return 0m;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
