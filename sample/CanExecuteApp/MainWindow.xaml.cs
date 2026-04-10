using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace CanExecuteApp;

public partial class MainWindow : Window
{
    private readonly TodoStore _store;

    public MainWindow()
    {
        InitializeComponent();
       
        DataContext = _store = new TodoStore ();
    }

    private void InputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && _store.AddCommand.CanExecute(InputBox.Text))
        {
            _store.AddCommand.Execute(InputBox.Text);
            InputBox.Clear();
        }
    }
}

/// <summary>bool → 문자열 변환기. ConverterParameter="True일때|False일때" 형식으로 사용.</summary>
public class BoolToStringConverter : IValueConverter
{
    public static readonly BoolToStringConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var parts = (parameter as string)?.Split('|');
        if (parts?.Length == 2 && value is bool b)
            return b ? parts[0] : parts[1];
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
