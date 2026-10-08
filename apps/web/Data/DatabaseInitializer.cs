using System.Reflection;
using Dapper;
using Npgsql;

namespace WebApp.Data;

/// <summary>
/// 起動時に schema.sql を流してテーブルと初期データを用意する。
/// コンテナ起動直後は DB がまだ接続を受け付けないことがあるので、少し待って再試行する。
/// </summary>
public static class DatabaseInitializer
{
    // 複数プロセスが同時に起動しても CREATE TABLE が衝突しないようにするためのロック番号
    private const long SchemaLockKey = 20261008;

    public static async Task InitializeAsync(NpgsqlDataSource db, ILogger logger, CancellationToken ct = default)
    {
        var sql = ReadSchemaSql();

        const int maxAttempts = 30;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var conn = await db.OpenConnectionAsync(ct);
                await using var tx = await conn.BeginTransactionAsync(ct);
                await conn.ExecuteAsync("SELECT pg_advisory_xact_lock(@Key)", new { Key = SchemaLockKey }, tx);
                await conn.ExecuteAsync(sql, transaction: tx);
                await tx.CommitAsync(ct);
                logger.LogInformation("データベースの初期化が完了しました。");
                return;
            }
            catch (Exception ex) when (IsNotReadyYet(ex) && attempt < maxAttempts)
            {
                logger.LogWarning("データベースに接続できません ({Attempt}/{Max}): {Message}", attempt, maxAttempts, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(1), ct);
            }
        }
    }

    /// <summary>「DB がまだ起動中」を表すエラーだけ再試行する。SQL の書き間違いなどはすぐ失敗させる。</summary>
    private static bool IsNotReadyYet(Exception ex) => ex switch
    {
        PostgresException pg => pg.SqlState == PostgresErrorCodes.CannotConnectNow,
        NpgsqlException or System.Net.Sockets.SocketException => true,
        _ => false,
    };

    private static string ReadSchemaSql()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("schema.sql")
            ?? throw new InvalidOperationException("埋め込みリソース schema.sql が見つかりません。");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
