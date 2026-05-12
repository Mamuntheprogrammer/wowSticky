using System.Diagnostics;
using System.Windows;

namespace WowSticky.Windows;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }

    private void Link_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    private void CopyEmail_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Windows.Forms.Clipboard.SetText("a.a.mamunbu@gmail.com");
        }
        catch { }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}