using Microsoft.Data.Sqlite;
using WowSticky.Models;

namespace WowSticky.Services;

public class FolderService
{
    private readonly DatabaseService _db;

    public FolderService(DatabaseService db) => _db = db;

    public List<Folder> GetAll()
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM folders WHERE archived = 0 ORDER BY updatedAt DESC";
        return ReadFolders(cmd);
    }

    public Folder? GetById(string id)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM folders WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        return ReadFolders(cmd).FirstOrDefault();
    }

    public Folder? GetByPath(string path)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM folders WHERE name = @name AND archived = 0";
        cmd.Parameters.AddWithValue("@name", path);
        return ReadFolders(cmd).FirstOrDefault();
    }

    public Folder Create(string name, string? color = null, string? icon = null)
    {
        var folder = new Folder
        {
            Name = name,
            Color = color ?? "#6366f1",
            Icon = icon ?? "folder"
        };

        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO folders (id, name, color, icon, archived, createdAt, updatedAt)
            VALUES (@id, @name, @color, @icon, 0, @createdAt, @updatedAt)
            """;
        cmd.Parameters.AddWithValue("@id", folder.Id);
        cmd.Parameters.AddWithValue("@name", folder.Name);
        cmd.Parameters.AddWithValue("@color", folder.Color);
        cmd.Parameters.AddWithValue("@icon", folder.Icon);
        cmd.Parameters.AddWithValue("@createdAt", folder.CreatedAt);
        cmd.Parameters.AddWithValue("@updatedAt", folder.UpdatedAt);
        cmd.ExecuteNonQuery();
        return folder;
    }

    public void Delete(string id)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM folders WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    private static List<Folder> ReadFolders(SqliteCommand cmd)
    {
        var list = new List<Folder>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Folder
            {
                Id = reader.GetString(0),
                Name = reader.GetString(1),
                Color = reader.GetString(2),
                Icon = reader.GetString(3),
                Archived = reader.GetInt32(4) == 1,
                CreatedAt = reader.GetString(5),
                UpdatedAt = reader.GetString(6)
            });
        }
        return list;
    }
}
