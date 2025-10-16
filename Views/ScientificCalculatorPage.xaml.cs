using System;
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

    // ☰ Menü (sidebar)
    private void OnMenuClicked(object sender, EventArgs e)
    {
        if (Shell.Current is not null)
        {
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Flyout;
            Shell.Current.FlyoutIsPresented = true;
        }
    }

    // 🔹 Sayı butonları
    private void OnNumberClicked(object sender, EventArgs e)
    {
        var num = ((Button)sender).Text;
        if (Display.Text == "0" || Display.Text == "Hata" || Display.Text == "Tanımsız" || Display.Text == "Belirsiz")
            Display.Text = num;
        else
            Display.Text += num;

        _input = Display.Text.Replace(",", ".");
    }

    // 🔹 Virgül / Nokta
    private void OnDecimalClicked(object sender, EventArgs e)
    {
        if (!Display.Text.Contains(_decimalSeparator))
        {
            Display.Text += _decimalSeparator;
            _input = Display.Text.Replace(",", ".");
        }
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

        _input += $" {op} ";
        Display.Text = _input.Replace("*", "×").Replace("/", "÷").Replace("-", "–");
    }

    // 🔹 Fonksiyonlar (bilimsel)
    private void OnFunctionClicked(object sender, EventArgs e)
{
    var func = ((Button)sender).Text;

    // Eğer ekranda hata varsa sıfırla
    if (Display.Text == "Hata" || Display.Text == "Tanımsız" || Display.Text == "Belirsiz")
    {
        Display.Text = "0";
        _input = "";
    }

    // Eğer sin, cos, tan, ln, log₁₀ gibi bir fonksiyonsa, mevcut metne ekle
    if (func is "sin" or "cos" or "tan" or "log₁₀" or "ln")
    {
        // Eğer ekran 0 veya boşsa direkt yaz
        if (Display.Text == "0" || string.IsNullOrWhiteSpace(Display.Text))
            Display.Text = $"{func}(";
        else
            Display.Text += $"{func}(";

        _input = Display.Text.Replace(",", ".");
        return;
    }

    // Diğer fonksiyonlar (tek parametreli hesaplamalar)
    if (!double.TryParse(Display.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double v))
        return;

    double result = v;
    try
    {
        switch (func)
        {
            case "x²": result = Math.Pow(v, 2); break;
            case "²√x": result = Math.Sqrt(v); break;
            case "¹⁄ₓ":
                if (v == 0) throw new DivideByZeroException();
                result = 1 / v;
                break;
            case "10ˣ": result = Math.Pow(10, v); break;
            case "n!":
                if (v < 0) throw new Exception("Negatif sayının faktöriyeli yok.");
                result = 1;
                for (int i = 1; i <= (int)v; i++) result *= i;
                break;
        }

        Display.Text = result.ToString(CultureInfo.CurrentCulture);
        Display.TextColor = Colors.Black;
        _input = result.ToString(CultureInfo.InvariantCulture);
    }
    catch
    {
        Display.Text = "Tanımsız";
        Display.TextColor = Colors.Red;
    }
}

    // 🔹 Eşittir
    private void OnEqualsClicked(object sender, EventArgs e)
    {
        try
        {
            string expr = Display.Text.Replace(",", ".").Trim();

            // 🔍 sin(...), cos(...), tan(...), ln(...), log₁₀(...)
            var match = Regex.Match(expr, @"(sin|cos|tan|log₁₀|ln)\(([^()]+)\)");
            if (match.Success)
            {
                string func = match.Groups[1].Value;
                string innerExpr = match.Groups[2].Value;

                // parantez içindeki ifadeyi hesapla
                var table = new System.Data.DataTable();
                double innerValue = Convert.ToDouble(table.Compute(innerExpr, ""), CultureInfo.InvariantCulture);

                double result = func switch
                {
                    "sin" => Math.Sin(innerValue * Math.PI / 180),
                    "cos" => Math.Cos(innerValue * Math.PI / 180),
                    "tan" => Math.Tan(innerValue * Math.PI / 180),
                    "ln" => Math.Log(innerValue),
                    "log₁₀" => Math.Log10(innerValue),
                    _ => double.NaN
                };

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

                return;
            }

            // 🔹 Normal ifadeleri hesapla
            expr = expr.Replace("×", "*").Replace("÷", "/").Replace("–", "-");

            var dt = new System.Data.DataTable();
            var val = Convert.ToDouble(dt.Compute(expr, ""), CultureInfo.InvariantCulture);

            if (double.IsNaN(val))
            {
                Display.Text = "Tanımsız";
                Display.TextColor = Colors.Red;
            }
            else if (double.IsInfinity(val))
            {
                Display.Text = "Belirsiz";
                Display.TextColor = Colors.Red;
            }
            else
            {
                Display.Text = val.ToString(CultureInfo.CurrentCulture);
                Display.TextColor = Colors.Black;
                _input = val.ToString(CultureInfo.InvariantCulture);
            }
        }
        catch
        {
            Display.Text = "Tanımsız";
            Display.TextColor = Colors.Red;
            _input = "";
        }
    }

    // 🔹 Temizle (⟳)
    private void OnClearClicked(object sender, EventArgs e)
    {
        Display.Text = "0";
        Display.TextColor = Colors.Black;
        _input = "";
    }

    // 🔹 Silme
    private void OnBackspaceClicked(object sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(Display.Text) && Display.Text != "0")
        {
            Display.Text = Display.Text.Length > 1 ? Display.Text[..^1] : "0";
            _input = Display.Text.Replace(",", ".");
        }
    }

    // 🔹 Yüzde
    private void OnPercentClicked(object sender, EventArgs e)
    {
        if (double.TryParse(Display.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
        {
            double result = val / 100.0;
            Display.Text = result.ToString(CultureInfo.CurrentCulture);
            Display.TextColor = Colors.Black;
            _input = result.ToString(CultureInfo.InvariantCulture);
        }
    }

    // 🔹 İşaret değiştir
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
