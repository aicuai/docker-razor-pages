// ============================================================================
// Program.cs ― アプリの起動処理。Classic ASP でいう global.asa + IIS の設定画面。
// ============================================================================
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Dapper;
using Microsoft.AspNetCore.DataProtection;
using Npgsql;
using WebApp;
using WebApp.Data;

// Shift_JIS (CP932) を使えるようにする。Linux の .NET は既定では UTF-8 系しか持っていない。
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

// Dapper の設定: DB の snake_case 列 (employee_no) を C# の EmployeeNo に自動で対応させる。
DefaultTypeMap.MatchNamesWithUnderscores = true;
SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

var builder = WebApplication.CreateBuilder(args);

// ---- サービスの登録 (ASP: Server.CreateObject で都度作っていた部品を、ここで一度だけ登録する) ----

// Razor Pages を有効にする。Pages フォルダーの構成がそのまま URL になる。
builder.Services.AddRazorPages().AddMvcOptions(options =>
{
    // 入力値の型変換エラーを日本語にする
    var messages = options.ModelBindingMessageProvider;
    messages.SetAttemptedValueIsInvalidAccessor((value, field) => $"{field}の値「{value}」は正しくありません。");
    messages.SetNonPropertyAttemptedValueIsInvalidAccessor(value => $"値「{value}」は正しくありません。");
    messages.SetValueMustNotBeNullAccessor(field => $"{field}は必須です。");
    messages.SetValueIsInvalidAccessor(value => $"値「{value}」は正しくありません。");
});

// 日本語を &#x3042; のような数値文字参照にせず、そのまま出力する。
// (既定では英数字以外をすべて参照に変換するため、HTML ソースが読めなくなりサイズも増える。
//  < > & " などの危険な文字は、この設定でも引き続きエスケープされる)
builder.Services.AddWebEncoders(options => options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

// DB 接続 (ASP: ADODB.Connection)。接続文字列は appsettings.json か環境変数 ConnectionStrings__Default で指定。
builder.Services.AddSingleton(sp =>
{
    var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("Default")
        ?? throw new InvalidOperationException("接続文字列 ConnectionStrings:Default が設定されていません。");
    return NpgsqlDataSource.Create(connectionString);
});
builder.Services.AddScoped<EmployeeRepository>();

// Session (ASP: Session("...") )。タイムアウトは ASP の既定と同じ 20 分にしてある。
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.Name = ".AspDemo.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Application 変数の置き換え (全ユーザー共有のアクセスカウンター)
builder.Services.AddSingleton<AccessCounter>();

// Cookie の暗号鍵をコンテナ再起動後も使い回せるよう、指定フォルダーに保存する (未指定なら既定の場所)
var keysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrEmpty(keysPath))
{
    builder.Services.AddDataProtection()
        .SetApplicationName("AspDemo")
        .PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}

var app = builder.Build();

// ---- 起動時に 1 回だけ行う処理 (ASP: Application_OnStart) ----
await DatabaseInitializer.InitializeAsync(
    app.Services.GetRequiredService<NpgsqlDataSource>(),
    app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Database"));

// ---- リクエストごとの処理の流れ (ミドルウェア)。上から順に通る ----
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error"); // 本番ではエラー詳細を見せず、エラー画面へ
}

app.UseRouting();
app.UseSession();        // これより後ろで HttpContext.Session が使える
app.UseAuthorization();

app.MapStaticAssets();   // wwwroot 配下の CSS など
app.MapRazorPages().WithStaticAssets();

app.Run();
