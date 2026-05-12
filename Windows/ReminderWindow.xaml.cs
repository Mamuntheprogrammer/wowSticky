using System.Media;
using System.Windows;
using System.Windows.Threading;
using WowSticky.Models;
using WMedia = System.Windows.Media;

namespace WowSticky.Windows;

public partial class ReminderWindow : Window
{
    private readonly Note _note;
    private readonly string _folderName;
    private readonly DispatcherTimer _ringTimer;
    private int _ringCount;

    public event Action<string>? Dismissed;

    public string NoteId => _note.Id;

    public ReminderWindow(Note note, string folderName)
    {
        InitializeComponent();
        _note = note;
        _folderName = folderName;

        _ringTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.5) };
        _ringTimer.Tick += (_, _) => PlayRing();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        TitleText.Text = string.IsNullOrEmpty(_note.Title) ? "Untitled" : _note.Title;

        var intervalLabel = _note.ReminderInterval switch
        {
            "daily" => "Daily",
            "monthly" => "Same day monthly",
            "firstday" => "First day of month",
            "lastday" => "Last day of month",
            _ => "Once"
        };
        MetaText.Text = $"From: {_folderName}  ·  {intervalLabel}";

        ContentText.Text = string.IsNullOrEmpty(_note.Content) ? "(empty)" : _note.Content;

        ApplyNoteColor();

        PlayRing();
        _ringTimer.Start();
    }

    private void ApplyNoteColor()
    {
        try
        {
            if (WMedia.ColorConverter.ConvertFromString(_note.Color) is WMedia.Color bg)
            {
                HeaderBorder.Background = new WMedia.SolidColorBrush(bg);
                HeaderBorder.BorderBrush = new WMedia.SolidColorBrush(WMedia.Color.FromArgb(60, 255, 255, 255));

                var fg = GetContrastBrush(bg);
                TitleText.Foreground = fg;
                MetaText.Foreground = fg;
            }
        }
        catch { }
    }

    private static WMedia.Brush GetContrastBrush(WMedia.Color bg)
    {
        var brightness = (0.299 * bg.R + 0.587 * bg.G + 0.114 * bg.B) / 255.0;
        return brightness > 0.6
            ? new WMedia.SolidColorBrush(WMedia.Colors.Black)
            : new WMedia.SolidColorBrush(WMedia.Colors.White);
    }

    private void PlayRing()
    {
        try
        {
            if (_ringCount % 2 == 0)
                SystemSounds.Asterisk.Play();
            else
                SystemSounds.Hand.Play();
            _ringCount++;
        }
        catch { }
    }

    private void Dismiss_Click(object sender, RoutedEventArgs e)
    {
        _ringTimer.Stop();
        Dismissed?.Invoke(_note.Id);
        Close();
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _ringTimer.Stop();
    }
}
