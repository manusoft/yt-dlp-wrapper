using Microsoft.Data.Sqlite;
using VideoDownloader.Models;

namespace VideoDownloader.Data;

public sealed class HistoryDb
{
    private const string Conn = "Data Source=history.db";

    public void Insert(DownloadTask task)
    {
        using var con = new SqliteConnection(Conn);
        con.Open();

        var cmd = con.CreateCommand();
        cmd.CommandText =
        """
        CREATE TABLE IF NOT EXISTS history(
            id TEXT,
            url TEXT,
            date TEXT
        );
        """;

        cmd.ExecuteNonQuery();

        cmd.CommandText =
        """
        INSERT INTO history VALUES($id,$url,$date)
        """;

        cmd.Parameters.AddWithValue("$id", task.Id.ToString());
        cmd.Parameters.AddWithValue("$url", task.Url);
        cmd.Parameters.AddWithValue("$date", DateTime.Now);

        cmd.ExecuteNonQuery();
    }
}