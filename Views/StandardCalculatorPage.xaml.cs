using System;
using System.Globalization;

namespace MyMauiApp.Views;

public partial class StandardCalculatorPage : ContentPage
{
    private string _input = "";
    private readonly string _decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

    public StandardCalculatorPage()
    {
        InitializeComponent();
        Display.Text = "0";
    }

    // ☰ Menü
    private void OnMenuClicked(object sender, EventArgs e)
    {
        if (Shell.Current is not null)
        {
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Flyout;
            Shell.Current.FlyoutIsPresented = true;
        }
    }

    // 🔹 Sayı
    private void OnNumberClicked(object sender, EventArgs e)
    {
        var num = ((Button)sender).Text;
        if (Display.Text == "0" || Display.Text == "Tanımsız" || Display.Text == "Belirsiz")
            Display.Text = num;
        else
            Display.Text += num;

        _input = Display.Text.Replace(",", ".");
    }

    // 🔹 Nokta / Virgül
    private void OnDecimalClicked(object sender, EventArgs e)
    {
        if (!Display.Text.Contains(_decimalSeparator))
        {
            Display.Text += _decimalSeparator;
            _input = Display.Text.Replace(",", ".");
        }
    }

    // 🔹 Operatör (+ - × ÷)
    private void OnOperatorClicked(object sender, EventArgs e)
    {
        var op = ((Button)sender).Text switch
        {
            "×" => "*",
            "÷" => "/",
            "–" => "-",
            "+" => "+",
            _ => ((Button)sender).Text
        };

        if (string.IsNullOrWhiteSpace(_input))
            return;

        if (_input.EndsWith(" + ") || _input.EndsWith(" - ") || _input.EndsWith(" * ") || _input.EndsWith(" / "))
            _input = _input[..^3];

        _input += $" {op} ";
        Display.Text = _input.Replace("*", "×").Replace("/", "÷").Replace("-", "–");
    }

    // 🔹 Fonksiyonlar (x², ¹/x, √)
    private async void OnFunctionClicked(object sender, EventArgs e)
    {
        if (!double.TryParse(Display.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double v))
            return;

        var func = ((Button)sender).Text;
        double result = v;

        try
        {
            switch (func)
            {
                case "x²": result = Math.Pow(v, 2); break;
                case "¹⁄ₓ": if (v == 0) throw new DivideByZeroException(); result = 1 / v; break;
                case "²√x": result = Math.Sqrt(v); break;
            }

            if (double.IsNaN(result))
            {
                Display.Text = "Tanımsız";
                Display.TextColor = Colors.Red;
            }
            else if (double.IsInfinity(result))
            {
                Display.Text = "Belirsiz";
                Display.TextColor = Colors.Red;
            }
            else
            {
                Display.Text = result.ToString(CultureInfo.CurrentCulture);
                Display.TextColor = Colors.Black;
                _input = result.ToString(CultureInfo.InvariantCulture);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", ex.Message, "Tamam");
            Display.Text = "Tanımsız";
            Display.TextColor = Colors.Red;
            _input = "";
        }
    }

    // 🔹 Eşittir
    private void OnEqualsClicked(object sender, EventArgs e)
    {
        try
        {
            string expr = _input.Replace("×", "*").Replace("÷", "/").Replace("–", "-").Replace(",", ".");

            // 0 / 0 veya 0 % 0 -> Belirsiz
            if (expr.Contains("0 / 0") || expr.Contains("0 % 0"))
            {
                Display.Text = "Belirsiz";
                Display.TextColor = Colors.Red;
                _input = "";
                return;
            }

            // ÷0 ama 0÷0 değilse -> Tanımsız
            if ((expr.Contains("/ 0") || expr.EndsWith("/0")) &&
                !(expr.Contains("0 / 0") || expr.Contains("0%0")))
            {
                Display.Text = "Tanımsız";
                Display.TextColor = Colors.Red;
                _input = "";
                return;
            }

            var table = new System.Data.DataTable();
            var result = Convert.ToDouble(table.Compute(expr, ""), CultureInfo.InvariantCulture);

            if (double.IsNaN(result))
            {
                Display.Text = "Tanımsız";
                Display.TextColor = Colors.Red;
            }
            else if (double.IsInfinity(result))
            {
                Display.Text = "Belirsiz";
                Display.TextColor = Colors.Red;
            }
            else
            {
                Display.Text = result.ToString(CultureInfo.CurrentCulture);
                Display.TextColor = Colors.Black;
                _input = result.ToString(CultureInfo.InvariantCulture);
            }
        }
        catch
        {
            Display.Text = "Tanımsız";
            Display.TextColor = Colors.Red;
            _input = "";
        }
    }

    // 🔹 Temizle (C, CE, ⟳)
    private void OnClearClicked(object sender, EventArgs e)
    {
        Display.Text = "0";
        Display.TextColor = Colors.Black;
        _input = "";
    }

    // 🔹 Silme (⌫)
    private void OnBackspaceClicked(object sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(Display.Text) && Display.Text != "0")
        {
            Display.Text = Display.Text.Length > 1 ? Display.Text[..^1] : "0";
            _input = Display.Text.Replace(",", ".");
        }
    }

    // 🔹 Yüzde (%)
    private void OnPercentClicked(object sender, EventArgs e)
    {
        if (double.TryParse(Display.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
        {
            val /= 100.0;
            Display.Text = val.ToString(CultureInfo.CurrentCulture);
            Display.TextColor = Colors.Black;
            _input = Display.Text.Replace(",", ".");
        }
    }

    // 🔹 Negatif / Pozitif (+/–)
    private void OnNegateClicked(object sender, EventArgs e)
    {
        if (double.TryParse(Display.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
        {
            val = -val;
            Display.Text = val.ToString(CultureInfo.CurrentCulture);
            Display.TextColor = Colors.Black;
            _input = Display.Text.Replace(",", ".");
        }
    }
}
