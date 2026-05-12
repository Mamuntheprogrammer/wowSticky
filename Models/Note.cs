namespace WowSticky.Models;

public class Note
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FolderId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string Color { get; set; } = "#FFE066";
    public double Opacity { get; set; } = 1.0;
    public double Width { get; set; } = 340;
    public double Height { get; set; } = 380;
    public double XPosition { get; set; }
    public double YPosition { get; set; }
    public bool Pinned { get; set; }
    public bool Locked { get; set; }
    public bool Archived { get; set; }
    public bool Trashed { get; set; }
    public int FontSize { get; set; } = 14;
    public long ZIndex { get; set; }
    public bool ReminderEnabled { get; set; }
    public string? ReminderNextAt { get; set; }
    public string ReminderInterval { get; set; } = "once";
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");
    public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("o");
}
