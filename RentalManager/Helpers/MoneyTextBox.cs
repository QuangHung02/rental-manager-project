using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RentalManager.Helpers;

public static class MoneyTextBox
{
    public static readonly DependencyProperty EnabledProperty =
        DependencyProperty.RegisterAttached("Enabled", typeof(bool), typeof(MoneyTextBox), new PropertyMetadata(false, OnEnabledChanged));

    public static bool GetEnabled(DependencyObject obj) => (bool)obj.GetValue(EnabledProperty);

    public static void SetEnabled(DependencyObject obj, bool value) => obj.SetValue(EnabledProperty, value);

    private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox textBox)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            textBox.PreviewTextInput += OnPreviewTextInput;
            textBox.PreviewKeyDown += OnPreviewKeyDown;
            textBox.GotFocus += OnGotFocus;
            textBox.LostFocus += OnLostFocus;
            DataObject.AddPastingHandler(textBox, OnPaste);
        }
        else
        {
            textBox.PreviewTextInput -= OnPreviewTextInput;
            textBox.PreviewKeyDown -= OnPreviewKeyDown;
            textBox.GotFocus -= OnGotFocus;
            textBox.LostFocus -= OnLostFocus;
            DataObject.RemovePastingHandler(textBox, OnPaste);
        }
    }

    private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !Regex.IsMatch(e.Text, "^[0-9,]*$");
    }

    private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            e.Handled = true;
        }
    }

    private static void OnPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.DataObject.GetDataPresent(DataFormats.Text))
        {
            e.CancelCommand();
            return;
        }

        var text = e.DataObject.GetData(DataFormats.Text) as string ?? string.Empty;
        if (!Regex.IsMatch(text, "^[0-9,]*$"))
        {
            e.CancelCommand();
        }
    }

    private static void OnGotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.Text = textBox.Text.Replace(",", string.Empty, StringComparison.Ordinal);
            textBox.SelectAll();
        }
    }

    private static void OnLostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox textBox || string.IsNullOrWhiteSpace(textBox.Text))
        {
            return;
        }

        var normalized = textBox.Text.Replace(",", string.Empty, StringComparison.Ordinal);
        if (!decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
        {
            MessageBox.Show("Vui lòng nhập số tiền hợp lệ.", "Quản lý nhà trọ", MessageBoxButton.OK, MessageBoxImage.Warning);
            textBox.Focus();
            textBox.SelectAll();
            return;
        }

        textBox.Text = value.ToString("N0", CultureInfo.InvariantCulture);
        textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
    }
}
