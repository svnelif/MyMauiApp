using System;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MyMauiApp.Views;

public partial class ScientificCalculatorPage : ContentPage
{
    private string _input = "";
    private readonly string _decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

    public ScientificCalculatorPage()
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

    // 🔹 Sayı tuşları
    private void OnNumberClicked(object sender, EventArgs e)
    {
        var num = ((Button)sender).Text;
        if (Display.Text is "0" or "Hata" or "Tanımsız" or "Belirsiz")
            Display.Text = num;
        else
            Display.Text += num;

        _input = Display.Text.Replace(",", ".");
    }

    // 🔹 Virgül
    private void OnDecimalClicked(object sender, EventArgs e)
    {
        if (!Display.Text.Contains(_decimalSeparator))
        {
            Display.Text += _decimalSeparator;
            _input = Display.Text.Replace(",", ".");
        }
    }

    // 🔹 Parantez
    private void OnParenClicked(object sender, EventArgs e)
    {
        var t = ((Button)sender).Text;
        if (Display.Text is "0" or "Hata" or "Tanımsız" or "Belirsiz")
            Display.Text = t;
        else
            Display.Text += t;

        _input = Display.Text.Replace(",", ".");
    }

    // 🔹 Operatörler
    private void OnOperatorClicked(object sender, EventArgs e)
    {
        var op = ((Button)sender).Text switch
        {
            "×" => "*",
            "÷" => "/",
            "–" => "-",
            "+" => "+",
            "mod" => "%",
            "xʸ" => "^",
            _ => ((Button)sender).Text
        };

        if (string.IsNullOrWhiteSpace(_input))
            return;

        if (_input.EndsWith(" + ") || _input.EndsWith(" - ") ||
            _input.EndsWith(" * ") || _input.EndsWith(" / ") ||
            _input.EndsWith(" % ") || _input.EndsWith(" ^ "))
        {
            _input = _input[..^3];
        }

        _input += $" {op} ";
        Display.Text = _input.Replace("*", "×").Replace("/", "÷").Replace("-", "–");
    }

    // 🔹 Token handler (XAML’de olabilir)
    private void OnFunctionTokenClicked(object sender, EventArgs e)
    {
        OnFunctionClicked(sender, e);
    }

    // 🔹 Fonksiyonlar
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
                case "²√x": result = Math.Sqrt(v); break;
                case "¹⁄ₓ":
                    if (v == 0)
                    {
                        await ShowError("Sıfıra bölme hatası", "Tanımsız");
                        return;
                    }
                    result = 1 / v;
                    break;

                case "n!":
                    if (v < 0)
                    {
                        await ShowError("Negatif sayının faktöriyeli tanımsızdır.", "Tanımsız");
                        return;
                    }
                    result = 1;
                    for (int i = 1; i <= (int)v; i++) result *= i;
                    break;

                case "exp": result = Math.Exp(v); break;
                case "10ˣ": result = Math.Pow(10, v); break;
                case "sin": result = Math.Sin(v * Math.PI / 180.0); break;
                case "cos": result = Math.Cos(v * Math.PI / 180.0); break;

                case "tan":
                    if (Math.Abs(v % 180) == 90)
                    {
                        await ShowError("tan(90° + 180n) tanımsızdır.", "Tanımsız");
                        return;
                    }
                    result = Math.Tan(v * Math.PI / 180.0);
                    break;

                case "ln":
                    if (v <= 0)
                    {
                        await ShowError("ln fonksiyonu için argüman (x) > 0 olmalıdır.", "Tanımsız");
                        return;
                    }
                    result = Math.Log(v);
                    break;

                case "log₁₀":
                    if (v <= 0)
                    {
                        await ShowError("log₁₀ fonksiyonu için argüman (x) > 0 olmalıdır.", "Tanımsız");
                        return;
                    }
                    result = Math.Log10(v);
                    break;

                default: return;
            }

            if (double.IsNaN(result))
                await ShowError("Sonuç tanımsızdır.", "Tanımsız");
            else if (double.IsInfinity(result))
                await ShowError("Sonuç belirsizdir.", "Belirsiz");
            else
            {
                Display.Text = result.ToString(CultureInfo.CurrentCulture);
                Display.TextColor = Colors.Black;
                _input = result.ToString(CultureInfo.InvariantCulture);
            }
        }
        catch (Exception ex)
        {
            await ShowError(ex.Message, "Tanımsız");
        }
    }

    // 🔹 "=" işlemi
    private async void OnEqualsClicked(object sender, EventArgs e)
    {
        try
        {
            string expr = _input
                .Replace("×", "*")
                .Replace("÷", "/")
                .Replace("–", "-")
                .Replace(",", ".");

            if (ContainsZeroOverZero(expr) || expr.Contains("0 % 0") || expr.Contains("0%0"))
            {
                await ShowError("0/0 veya 0%0 işlemi belirsizdir.", "Belirsiz");
                return;
            }

            if (DividesByZero(expr))
            {
                await ShowError("Sıfıra bölme işlemi tanımsızdır.", "Tanımsız");
                return;
            }

            expr = EvaluatePowers(expr);
            var table = new DataTable();
            var resultObj = table.Compute(expr, "");
            double result = Convert.ToDouble(resultObj, CultureInfo.InvariantCulture);

            if (double.IsNaN(result))
            {
                await ShowError("Sonuç tanımsızdır.", "Tanımsız");
                return;
            }
            if (double.IsInfinity(result))
            {
                await ShowError("Sonuç belirsizdir.", "Belirsiz");
                return;
            }

            Display.Text = result.ToString(CultureInfo.CurrentCulture);
            Display.TextColor = Colors.Black;
            _input = result.ToString(CultureInfo.InvariantCulture);
        }
        catch
        {
            await ShowError("Hesaplama sırasında bir hata oluştu.", "Tanımsız");
        }
    }

    // 🔹 Ortak hata gösterim metodu
    private async Task ShowError(string message, string type)
    {
        Display.Text = type;
        Display.TextColor = Colors.Red;
        _input = "";

        // kullanıcıya alert göster
        await DisplayAlert("Hata", message, "Tamam");

        // alert kapandıktan sonra ekranı sıfırla
        Display.Text = "0";
        Display.TextColor = Colors.Black;
    }

    private static bool ContainsZeroOverZero(string s)
        => s.Contains("0 / 0") || s.Contains("0/0");

    private static bool DividesByZero(string s)
    {
        var hasDivideZero = s.Contains("/ 0") || s.Contains("/0");
        return hasDivideZero && !ContainsZeroOverZero(s);
    }

    private static string EvaluatePowers(string expr)
    {
        var powPattern = new Regex(@"(?<a>-?\d+(\.\d+)?)\s*\^\s*(?<b>-?\d+(\.\d+)?)");
        while (true)
        {
            var m = powPattern.Match(expr);
            if (!m.Success) break;

            double a = double.Parse(m.Groups["a"].Value, CultureInfo.InvariantCulture);
            double b = double.Parse(m.Groups["b"].Value, CultureInfo.InvariantCulture);
            double val = Math.Pow(a, b);

            var rep = double.IsNaN(val) ? "NaN" : val.ToString(CultureInfo.InvariantCulture);
            expr = powPattern.Replace(expr, rep, 1);
        }
        return expr;
    }

    private void OnClearClicked(object sender, EventArgs e)
    {
        Display.Text = "0";
        Display.TextColor = Colors.Black;
        _input = "";
    }

    private void OnBackspaceClicked(object sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(Display.Text) && Display.Text != "0")
        {
            Display.Text = Display.Text.Length > 1 ? Display.Text[..^1] : "0";
            _input = Display.Text.Replace(",", ".");
        }
    }

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
