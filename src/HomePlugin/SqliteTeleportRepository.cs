using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace HomePlugin;

public class SqliteTeleportRepository : ITeleportRepository, IDisposable
{
    private readonly SQLiteConnection _connection;

    public SqliteTeleportRepository(string connectionString)
    {
        _connection = new SQLiteConnection(connectionString);
        _connection.Open();
        EnsureTable();
    }

    private void EnsureTable()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS TeleportPoints (
                UserId INTEGER NOT NULL,
                Name   TEXT    NOT NULL,
                X      REAL    NOT NULL,
                Y      REAL    NOT NULL,
                Z      REAL    NOT NULL,
                PRIMARY KEY (UserId, Name)
            );";
        cmd.ExecuteNonQuery();
    }

    public void AddPoint(TeleportPoint point)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO TeleportPoints (UserId, Name, X, Y, Z)
            VALUES ($userId, $name, $x, $y, $z);";
        cmd.Parameters.AddWithValue("$userId", point.UserId);
        cmd.Parameters.AddWithValue("$name", point.Name);
        cmd.Parameters.AddWithValue("$x", point.X);
        cmd.Parameters.AddWithValue("$y", point.Y);
        cmd.Parameters.AddWithValue("$z", point.Z);
        cmd.ExecuteNonQuery();
    }

    public void RemovePoint(string userId, string name)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM TeleportPoints WHERE UserId = $userId AND Name = $name;";
        cmd.Parameters.AddWithValue("$userId", userId);
        cmd.Parameters.AddWithValue("$name", name);
        cmd.ExecuteNonQuery();
    }

    public IEnumerable<TeleportPoint> GetPoints(string userId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT Name, X, Y, Z FROM TeleportPoints WHERE UserId = $userId;";
        cmd.Parameters.AddWithValue("$userId", userId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            yield return new TeleportPoint
            {
                UserId = userId,
                Name   = reader.GetString(0),
                X      = reader.GetFloat(1),
                Y      = reader.GetFloat(2),
                Z      = reader.GetFloat(3)
            };
        }
    }

    public TeleportPoint GetPoint(string userId, string name)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT X, Y, Z FROM TeleportPoints WHERE UserId = $userId AND Name = $name;";
        cmd.Parameters.AddWithValue("$userId", userId);
        cmd.Parameters.AddWithValue("$name", name);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new TeleportPoint
            {
                UserId = userId,
                Name   = name,
                X      = reader.GetFloat(0),
                Y      = reader.GetFloat(1),
                Z      = reader.GetFloat(2)
            };
        }
        return null;
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
