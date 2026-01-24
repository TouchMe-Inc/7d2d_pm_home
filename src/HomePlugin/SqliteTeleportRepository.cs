using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace HomePlugin;

public class SqliteTeleportRepository : ITeleportRepository, IDisposable
{
    private readonly SQLiteConnection _connection;

    private const string CreateTableSql = """
                                          CREATE TABLE IF NOT EXISTS TeleportPoints (
                                              UserId TEXT    NOT NULL,
                                              Name   TEXT    NOT NULL,
                                              X      REAL    NOT NULL,
                                              Y      REAL    NOT NULL,
                                              Z      REAL    NOT NULL,
                                              PRIMARY KEY (UserId, Name)
                                          );
                                          """;

    private const string InsertOrReplaceSql = """
                                              INSERT OR REPLACE INTO TeleportPoints (UserId, Name, X, Y, Z)
                                              VALUES (@UserId, @Name, @X, @Y, @Z);
                                              """;

    private const string DeletePointSql = """
                                          DELETE FROM TeleportPoints 
                                          WHERE UserId = @UserId AND Name = @Name;
                                          """;

    private const string SelectPointsSql = """
                                           SELECT Name, X, Y, Z 
                                           FROM TeleportPoints 
                                           WHERE UserId = @UserId;
                                           """;

    private const string SelectPointSql = """
                                          SELECT X, Y, Z 
                                          FROM TeleportPoints 
                                          WHERE UserId = @UserId AND Name = @Name;
                                          """;

    private const string CountPointsSql = """
                                          SELECT COUNT(*) 
                                          FROM TeleportPoints 
                                          WHERE UserId = @UserId;
                                          """;
    
    public SqliteTeleportRepository(string connectionString)
    {
        _connection = new SQLiteConnection(connectionString);
        _connection.Open();
        EnsureTable();
    }

    private void EnsureTable()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = CreateTableSql;
        cmd.ExecuteNonQuery();
    }

    public void AddPoint(TeleportPoint point)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = InsertOrReplaceSql;
        cmd.Parameters.AddWithValue("@UserId", point.UserId);
        cmd.Parameters.AddWithValue("@Name", point.Name);
        cmd.Parameters.AddWithValue("@X", point.X);
        cmd.Parameters.AddWithValue("@Y", point.Y);
        cmd.Parameters.AddWithValue("@Z", point.Z);
        cmd.ExecuteNonQuery();
    }

    public int RemovePoint(string userId, string name)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = DeletePointSql;
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@Name", name);

        return cmd.ExecuteNonQuery();
    }

    public IEnumerable<TeleportPoint> GetPoints(string userId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = SelectPointsSql;
        cmd.Parameters.AddWithValue("@UserId", userId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            yield return new TeleportPoint
            {
                UserId = userId,
                Name = reader.GetString(0),
                X = reader.GetFloat(1),
                Y = reader.GetFloat(2),
                Z = reader.GetFloat(3)
            };
        }
    }

    public TeleportPoint GetPoint(string userId, string name)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = SelectPointSql;
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@Name", name);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new TeleportPoint
            {
                UserId = userId,
                Name = name,
                X = reader.GetFloat(0),
                Y = reader.GetFloat(1),
                Z = reader.GetFloat(2)
            };
        }

        return null;
    }

    public int GetPointsCount(string userId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = CountPointsSql;
        cmd.Parameters.AddWithValue("@UserId", userId);

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}