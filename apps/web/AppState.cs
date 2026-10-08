namespace WebApp;

/// <summary>
/// アクセスカウンター。Classic ASP の Application("Counter") の置き換え。
/// シングルトンとして 1 個だけ作られ、全ユーザーで共有される。
/// Application.Lock / Unlock の代わりに Interlocked で排他する。
/// (メモリ上にあるだけなので、アプリを再起動すると 0 に戻るのも ASP と同じ)
/// </summary>
public sealed class AccessCounter
{
    private long _count;

    public long Increment() => Interlocked.Increment(ref _count);

    public long Current => Interlocked.Read(ref _count);
}

/// <summary>Session("...") のキー名を打ち間違えないよう定数にしておく。</summary>
public static class SessionKeys
{
    public const string OperatorName = "OperatorName";
}
