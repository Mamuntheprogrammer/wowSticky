using Microsoft.Data.Sqlite;
using WowSticky.Models;

namespace WowSticky.Services;

public class NoteService
{
    private readonly DatabaseService _db;

    public NoteService(DatabaseService db) => _db = db;

    public List<Note> GetByFolder(string folderId)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM notes WHERE folderId = @fid AND trashed = 0 AND archived = 0 ORDER BY pinned DESC, updatedAt DESC";
        cmd.Parameters.AddWithValue("@fid", folderId);
        return ReadNotes(cmd);
    }

    public Note? GetById(string id)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM notes WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        return ReadNotes(cmd).FirstOrDefault();
    }

    public List<Note> GetPinned()
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM notes WHERE pinned = 1 AND trashed = 0 AND archived = 0 ORDER BY updatedAt DESC";
        return ReadNotes(cmd);
    }

    public Note Create(string folderId, double? x = null, double? y = null)
    {
        var note = new Note
        {
            FolderId = folderId,
            XPosition = x ?? 100 + Random.Shared.NextDouble() * 300,
            YPosition = y ?? 100 + Random.Shared.NextDouble() * 300,
            ZIndex = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO notes (id, folderId, title, content, color, opacity, width, height,
                xPosition, yPosition, pinned, locked, archived, trashed, fontSize, zIndex, createdAt, updatedAt)
            VALUES (@id, @fid, @title, @content, @color, @opacity, @width, @height,
                @x, @y, 0, 0, 0, 0, 14, @z, @createdAt, @updatedAt)
            """;
        cmd.Parameters.AddWithValue("@id", note.Id);
        cmd.Parameters.AddWithValue("@fid", note.FolderId);
        cmd.Parameters.AddWithValue("@title", note.Title);
        cmd.Parameters.AddWithValue("@content", note.Content);
        cmd.Parameters.AddWithValue("@color", note.Color);
        cmd.Parameters.AddWithValue("@opacity", note.Opacity);
        cmd.Parameters.AddWithValue("@width", note.Width);
        cmd.Parameters.AddWithValue("@height", note.Height);
        cmd.Parameters.AddWithValue("@x", note.XPosition);
        cmd.Parameters.AddWithValue("@y", note.YPosition);
        cmd.Parameters.AddWithValue("@z", note.ZIndex);
        cmd.Parameters.AddWithValue("@createdAt", note.CreatedAt);
        cmd.Parameters.AddWithValue("@updatedAt", note.UpdatedAt);
        cmd.ExecuteNonQuery();
        return note;
    }

    public void Update(string id, string? title = null, string? content = null, string? color = null, int? fontSize = null)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        var sets = new List<string> { "updatedAt = @updatedAt" };
        var now = DateTime.UtcNow.ToString("o");

        if (title != null) { sets.Add("title = @title"); }
        if (content != null) { sets.Add("content = @content"); }
        if (color != null) { sets.Add("color = @color"); }
        if (fontSize.HasValue) { sets.Add("fontSize = @fontSize"); }

        cmd.CommandText = $"UPDATE notes SET {string.Join(", ", sets)} WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@updatedAt", now);
        if (title != null) cmd.Parameters.AddWithValue("@title", title);
        if (content != null) cmd.Parameters.AddWithValue("@content", content);
        if (color != null) cmd.Parameters.AddWithValue("@color", color);
        if (fontSize.HasValue) cmd.Parameters.AddWithValue("@fontSize", fontSize.Value);
        cmd.ExecuteNonQuery();
    }

    public void UpdatePosition(string id, double x, double y)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE notes SET xPosition = @x, yPosition = @y, updatedAt = @updatedAt WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@x", x);
        cmd.Parameters.AddWithValue("@y", y);
        cmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    public void UpdateSize(string id, double w, double h)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE notes SET width = @w, height = @h, updatedAt = @updatedAt WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@w", w);
        cmd.Parameters.AddWithValue("@h", h);
        cmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    public void TogglePin(string id)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE notes SET pinned = CASE WHEN pinned = 1 THEN 0 ELSE 1 END, updatedAt = @updatedAt WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    public void ToggleLock(string id)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE notes SET locked = CASE WHEN locked = 1 THEN 0 ELSE 1 END, updatedAt = @updatedAt WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    public void Trash(string id)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE notes SET trashed = 1, updatedAt = @updatedAt WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    public Note Duplicate(string id)
    {
        var original = GetById(id);
        if (original == null) throw new InvalidOperationException("Note not found");

        var note = new Note
        {
            FolderId = original.FolderId,
            Title = string.IsNullOrEmpty(original.Title) ? "" : original.Title + " (Copy)",
            Content = original.Content,
            Color = original.Color,
            Width = original.Width,
            Height = original.Height,
            XPosition = original.XPosition + 20,
            YPosition = original.YPosition + 20,
            FontSize = original.FontSize,
            ZIndex = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO notes (id, folderId, title, content, color, opacity, width, height,
                xPosition, yPosition, pinned, locked, archived, trashed, fontSize, zIndex, createdAt, updatedAt)
            VALUES (@id, @fid, @title, @content, @color, @opacity, @width, @height,
                @x, @y, 0, 0, 0, 0, @fs, @z, @createdAt, @updatedAt)
            """;
        cmd.Parameters.AddWithValue("@id", note.Id);
        cmd.Parameters.AddWithValue("@fid", note.FolderId);
        cmd.Parameters.AddWithValue("@title", note.Title);
        cmd.Parameters.AddWithValue("@content", note.Content);
        cmd.Parameters.AddWithValue("@color", note.Color);
        cmd.Parameters.AddWithValue("@opacity", note.Opacity);
        cmd.Parameters.AddWithValue("@width", note.Width);
        cmd.Parameters.AddWithValue("@height", note.Height);
        cmd.Parameters.AddWithValue("@x", note.XPosition);
        cmd.Parameters.AddWithValue("@y", note.YPosition);
        cmd.Parameters.AddWithValue("@fs", note.FontSize);
        cmd.Parameters.AddWithValue("@z", note.ZIndex);
        cmd.Parameters.AddWithValue("@createdAt", note.CreatedAt);
        cmd.Parameters.AddWithValue("@updatedAt", note.UpdatedAt);
        cmd.ExecuteNonQuery();
        return note;
    }

    public void DeletePermanently(string id)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM notes WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    private static List<Note> ReadNotes(SqliteCommand cmd)
    {
        var list = new List<Note>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Note
            {
                Id = reader.GetString(0),
                FolderId = reader.GetString(1),
                Title = reader.GetString(2),
                Content = reader.GetString(3),
                Color = reader.GetString(4),
                Opacity = reader.GetDouble(5),
                Width = reader.GetDouble(6),
                Height = reader.GetDouble(7),
                XPosition = reader.GetDouble(8),
                YPosition = reader.GetDouble(9),
                Pinned = reader.GetInt32(10) == 1,
                Locked = reader.GetInt32(11) == 1,
                Archived = reader.GetInt32(12) == 1,
                Trashed = reader.GetInt32(13) == 1,
                FontSize = reader.GetInt32(14),
                ZIndex = reader.GetInt64(15),
                CreatedAt = reader.GetString(16),
                UpdatedAt = reader.GetString(17)
            });
        }
        return list;
    }
}
