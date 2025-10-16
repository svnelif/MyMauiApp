namespace MyMauiApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
    }

    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Settings", "Ayarlar henüz eklenmedi 😊", "Tamam");
    }
}
