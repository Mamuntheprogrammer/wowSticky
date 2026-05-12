using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WowSticky.Windows;

public partial class TutorialWindow : Window
{
    private readonly Step[] _steps =
    [
        new("📁", "Folder-Aware Notes",
            "Your notes are linked to folders. Switch to any folder in Explorer and your notes for that folder appear automatically. Switch away and they hide."),
        new("💻", "System Tray — The Control Center",
            "WowSticky runs in your system tray (notification area near the clock). You control everything from here. Look for the tray icon and right-click it to create notes, show/hide all notes, open the data folder, or quit the app."),
        new("📌", "Pin & Lock",
            "Pin keeps a note at a fixed screen position. Lock makes it read-only — perfect for temporary reference notes you don't want to accidentally edit."),
        new("🎨", "Colors & Font Size",
            "Click the palette button to cycle through note colors. Use the + and − buttons to adjust the font size to your liking."),
        new("↔️", "Drag & Resize",
            "Drag the title bar to move a note anywhere. Grab the ↘ handle in the bottom-right corner to resize."),
        new("💾", "Auto-Save",
            "Everything saves automatically. Close a note and it will be right back when you revisit the folder."),
        new("⌨️", "Keyboard Shortcut",
            "Press Ctrl+Shift+W anywhere to show or hide all notes instantly. The tray icon also responds to double-click."),
        new("🗑", "Trash",
            "Click the trash icon to permanently delete a note."),
    ];

    private int _current;

    public TutorialWindow()
    {
        InitializeComponent();
        ShowStep(0);
    }

    private void ShowStep(int i)
    {
        _current = i;
        var s = _steps[i];
        TitleText.Text = $"✨  {s.Title}";
        IconText.Text = s.Icon;
        DescText.Text = s.Desc;
        CounterText.Text = $"{i + 1} / {_steps.Length}";
        PrevBtn.Visibility = i == 0 ? Visibility.Hidden : Visibility.Visible;
        NextBtn.Content = i == _steps.Length - 1 ? "Got it!  🚀" : "Next  →";
        UpdateDots(i);
    }

    private void UpdateDots(int active)
    {
        DotsBar.ItemsSource = null;
        var dots = new List<SolidColorBrush>();
        for (int i = 0; i < _steps.Length; i++)
            dots.Add(i == active
                ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(99, 102, 241))
                : new SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 80)));
        DotsBar.ItemsSource = dots;
    }

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        if (_current == _steps.Length - 1)
            Close();
        else
            ShowStep(_current + 1);
    }

    private void Prev_Click(object sender, RoutedEventArgs e)
    {
        if (_current > 0)
            ShowStep(_current - 1);
    }

    private void Skip_Click(object sender, RoutedEventArgs e) => Close();

    private record Step(string Icon, string Title, string Desc);
}
