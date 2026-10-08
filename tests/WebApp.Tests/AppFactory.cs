using System.Net;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;

namespace WebApp.Tests;

/// <summary>
/// テスト用にアプリをメモリ上で起動する。DB は本物の PostgreSQL (テスト専用データベース) を使う。
/// 接続先は環境変数 TEST_CONNECTION_STRING で変更できる。
/// </summary>
public sealed class AppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public static string ConnectionString =>
        Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING")
        ?? "Host=localhost;Port=5432;Database=aspdemo_test;Username=app;Password=app";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", ConnectionString);
    }

    public async Task InitializeAsync()
    {
        await EnsureDatabaseExistsAsync();

        // アプリを起動する (ここで schema.sql が流れる)
        using var client = CreateClient();
        (await client.GetAsync("/")).EnsureSuccessStatusCode();

        // 前回のテストの残りを消して、初期データだけの状態にそろえる
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.ExecuteAsync("DELETE FROM todos; DELETE FROM employees WHERE employee_no LIKE 'T%';");
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;

    /// <summary>Cookie を保持し、リダイレクトを自動でたどらないクライアント。</summary>
    public HttpClient CreateBrowser() =>
        CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    private static async Task EnsureDatabaseExistsAsync()
    {
        var target = new NpgsqlConnectionStringBuilder(ConnectionString);
        var admin = new NpgsqlConnectionStringBuilder(ConnectionString) { Database = "postgres" };

        await using var conn = new NpgsqlConnection(admin.ConnectionString);
        var exists = await conn.ExecuteScalarAsync<bool>(
            "SELECT EXISTS (SELECT 1 FROM pg_database WHERE datname = @name)", new { name = target.Database });
        if (!exists)
        {
            // データベース名は識別子なのでパラメータにできない。二重引用符で囲んで埋め込む。
            await conn.ExecuteAsync($"CREATE DATABASE \"{target.Database!.Replace("\"", "\"\"")}\"");
        }
    }
}

[CollectionDefinition(Name)]
public sealed class AppCollection : ICollectionFixture<AppFactory>
{
    public const string Name = "app";
}

public static partial class HtmlExtensions
{
    /// <summary>フォームに埋め込まれた偽造防止トークンを取り出す。</summary>
    public static string AntiforgeryToken(this string html)
    {
        var match = TokenRegex().Match(html);
        Assert.True(match.Success, "偽造防止トークンが HTML に見つかりません。");
        return match.Groups[1].Value;
    }

    /// <summary>画面を GET してトークンを取り、同じ Cookie でフォームを POST する。</summary>
    public static async Task<HttpResponseMessage> PostFormAsync(
        this HttpClient client, string getUrl, string postUrl, Dictionary<string, string> fields)
    {
        var html = await client.GetStringAsync(getUrl);
        var form = new Dictionary<string, string>(fields) { ["__RequestVerificationToken"] = html.AntiforgeryToken() };
        return await client.PostAsync(postUrl, new FormUrlEncodedContent(form));
    }

    public static void AssertRedirect(this HttpResponseMessage response) =>
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex TokenRegex();
}
