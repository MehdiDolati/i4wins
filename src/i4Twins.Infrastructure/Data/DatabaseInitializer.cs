using Dapper;
using Microsoft.Data.Sqlite;

namespace i4Twins.Infrastructure.Data;

public class DatabaseInitializer
{
    private readonly SqliteConnection _connection;

    public DatabaseInitializer(SqliteConnection connection)
    {
        _connection = connection;
    }

    public void Initialize()
    {
        const string createTable = @"
            CREATE TABLE IF NOT EXISTS Readings (
                Id TEXT PRIMARY KEY,
                DeviceId TEXT NOT NULL,
                Metric TEXT NOT NULL,
                Timestamp TEXT NOT NULL,
                Value REAL NOT NULL,
                Sequence INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_readings_device_metric_time 
                ON Readings(DeviceId, Metric, Timestamp);
        ";

        _connection.Execute(createTable);
    }
}