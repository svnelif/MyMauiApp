using System;
using System.Globalization;

namespace MyMauiApp.Views;

public partial class StandardCalculatorPage : ContentPage
{
    // Kullanıcının girdiği ifadeyi tutan değişken
    private string _input = "";

    // Kullanıcının sistem diline göre ondalık ayırıcıyı belirler (örneğin Türkçe için ",")
    private readonly string _decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

    public StandardCalculatorPage()
    {
        InitializeComponent();
        Display.Text = "0"; // Başlangıçta ekran 0 olarak ayarlanır
    }

    // Menü tuşuna basıldığında Shell menüsünü açar
    private void OnMenuClicked(object sender, EventArgs e)
    {
        if (Shell.Current is not null)
        {
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Flyout;
            Shell.Current.FlyoutIsPresented = true;
        }
    }

    // Sayı tuşlarına basıldığında ekrana sayıyı yazar
    private void OnNumberClicked(object sender, EventArgs e)
    {
        var num = ((Button)sender).Text;

        // Eğer ekranda "0" veya hata mesajı varsa, yeni sayı yazmaya başlanır
        if (Display.Text == "0" || Display.Text == "Tanımsız" || Display.Text == "Belirsiz")
            Display.Text = num;
        else
            Display.Text += num;

        // Hesaplama için giriş "." formatına çevrilir
        _input = Display.Text.Replace(",", ".");
    }

    // Virgül veya nokta tuşuna basıldığında ondalık sayı oluşturur
    private void OnDecimalClicked(object sender, EventArgs e)
    {
        if (!Display.Text.Contains(_decimalSeparator))
        {
            Display.Text += _decimalSeparator;
            _input = Display.Text.Replace(",", ".");
        }
    }

    // Operatör tuşları (+, -, ×, ÷) tıklandığında çağrılır
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

        // Eğer giriş boşsa işlem yapılmaz
        if (string.IsNullOrWhiteSpace(_input))
            return;

        // Son karakter zaten bir operatörse, yenisiyle değiştirilir
        if (_input.EndsWith(" + ") || _input.EndsWith(" - ") || _input.EndsWith(" * ") || _input.EndsWith(" / "))
            _input = _input[..^3];

        // Yeni operatör eklenir
        _input += $" {op} ";
        Display.Text = _input.Replace("*", "×").Replace("/", "÷").Replace("-", "–");
    }

    // Fonksiyon tuşları (x², ¹/x, √) tıklandığında çalışır
    private async void OnFunctionClicked(object sender, EventArgs e)
    {
        // Ekrandaki metni double tipine dönüştür
        if (!double.TryParse(Display.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double v))
            return;

        var func = ((Button)sender).Text;
        double result = v;

        try
        {
            switch (func)
            {
                case "x²": 
                    result = Math.Pow(v, 2); 
                    break;
                case "¹⁄ₓ":
                    // Sıfıra bölünme kontrolü
                    if (v == 0)
                    {
                        await ShowError("Sıfıra bölme hatası!", "Tanımsız");
                        return;
                    }
                    result = 1 / v; 
                    break;
                case "²√x": 
                    result = Math.Sqrt(v); 
                    break;
            }

            // NaN veya sonsuz sonuç durumlarını kontrol et
            if (double.IsNaN(result))
                await ShowError("Sonuç tanımsızdır.", "Tanımsız");
            else if (double.IsInfinity(result))
                await ShowError("Sonuç belirsizdir.", "Belirsiz");
            else
            {
                // Hesaplama sonucu ekrana yazdırılır
                Display.Text = result.ToString(CultureInfo.CurrentCulture);
                Display.TextColor = Colors.Black;
                _input = result.ToString(CultureInfo.InvariantCulture);
            }
        }
        catch (Exception ex)
        {
            // Beklenmeyen bir hata durumunda uyarı göster
            await ShowError(ex.Message, "Tanımsız");
        }
    }

    // Eşittir (=) tuşuna basıldığında çalışır
    private async void OnEqualsClicked(object sender, EventArgs e)
    {
        try
        {
            // Görsel sembolleri hesaplamaya uygun formatta değiştir
            string expr = _input.Replace("×", "*").Replace("÷", "/").Replace("–", "-").Replace(",", ".");

            // 0 / 0 veya 0 % 0 durumları belirsiz kabul edilir
            if (expr.Contains("0 / 0") || expr.Contains("0%0") || expr.Contains("0 % 0"))
            {
                await ShowError("0/0 işlemi belirsizdir.", "Belirsiz");
                return;
            }

            // 0’a bölme durumu tanımsız kabul edilir
            if ((expr.Contains("/ 0") || expr.EndsWith("/0")) &&
                !(expr.Contains("0 / 0") || expr.Contains("0%0")))
            {
                await ShowError("Sıfıra bölme işlemi tanımsızdır.", "Tanımsız");
                return;
            }

            // İşlem DataTable ile değerlendirilir
            var table = new System.Data.DataTable();
            var result = Convert.ToDouble(table.Compute(expr, ""), CultureInfo.InvariantCulture);

            // Sonuç kontrolleri
            if (double.IsNaN(result))
                await ShowError("Sonuç tanımsızdır.", "Tanımsız");
            else if (double.IsInfinity(result))
                await ShowError("Sonuç belirsizdir.", "Belirsiz");
            else
            {
                // Geçerli sonuç ekrana yazılır
                Display.Text = result.ToString(CultureInfo.CurrentCulture);
                Display.TextColor = Colors.Black;
                _input = result.ToString(CultureInfo.InvariantCulture);
            }
        }
        catch
        {
            await ShowError("Hesaplama sırasında bir hata oluştu.", "Tanımsız");
        }
    }

    // Ortak hata gösterim metodu: ekranda hata mesajı gösterir, uyarı verir ve ardından ekranı sıfırlar
    private async Task ShowError(string message, string type)
    {
        // Önce ekrana hata türünü (Tanımsız / Belirsiz) kırmızı olarak yaz
        Display.Text = type;
        Display.TextColor = Colors.Red;
        _input = "";

        // Alert’i ana thread üzerinde güvenli şekilde göster
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await DisplayAlert("Hata", message, "Tamam");
        });

        // Kullanıcı "Tamam" butonuna bastıktan sonra ekranı sıfırla
        Display.Text = "0";
        Display.TextColor = Colors.Black;
    }

    // Temizle (C) tuşu ekrandaki değeri sıfırlar
    private void OnClearClicked(object sender, EventArgs e)
    {
        Display.Text = "0";
        Display.TextColor = Colors.Black;
        _input = "";
    }

    // Silme (⌫) tuşu son karakteri siler
    private void OnBackspaceClicked(object sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(Display.Text) && Display.Text != "0")
        {
            Display.Text = Display.Text.Length > 1 ? Display.Text[..^1] : "0";
            _input = Display.Text.Replace(",", ".");
        }
    }

    // Yüzde (%) tuşu mevcut sayıyı 100'e böler
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

    // (+/–) tuşu sayının işaretini değiştirir
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