using System.Windows.Threading;
using WowSticky.Models;

namespace WowSticky.Services;

public class ReminderService
{
    private readonly NoteService _noteService;
    private readonly DispatcherTimer _timer;
    private Action<Note>? _onReminder;

    public ReminderService(NoteService noteService)
    {
        _noteService = noteService;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _timer.Tick += OnTick;
    }

    public void Start(Action<Note> onReminder)
    {
        _onReminder = onReminder;
        _timer.Start();
    }

    public void Stop() => _timer.Stop();

    private void OnTick(object? sender, EventArgs e)
    {
        try
        {
            var now = DateTime.Now;
            var notes = _noteService.GetReminders();

            foreach (var note in notes)
            {
                if (string.IsNullOrEmpty(note.ReminderNextAt)) continue;
                if (!DateTime.TryParse(note.ReminderNextAt, out var nextAt)) continue;

                if (now >= nextAt)
                {
                    _onReminder?.Invoke(note);
                    var next = ComputeNextReminder(note.ReminderInterval, nextAt);
                    _noteService.UpdateReminderNextAt(note.Id, next?.ToString("o"));
                }
            }
        }
        catch { }
    }

    public static DateTime? ComputeNextReminder(string interval, DateTime from)
    {
        var now = DateTime.Now;
        var next = from;

        if (interval == "once") return null;

        do
        {
            next = interval switch
            {
                "daily" => next.AddDays(1),
                "monthly" => next.AddMonths(1),
                "firstday" => new DateTime(next.Year, next.Month, 1).AddMonths(1).Date + from.TimeOfDay,
                "lastday" => new DateTime(next.Year, next.Month, 1).AddMonths(2).AddDays(-1).Date + from.TimeOfDay,
                _ => next.AddDays(1)
            };
        }
        while (next <= now);

        return next;
    }
}
