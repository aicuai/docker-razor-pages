# syntax=docker/dockerfile:1
# ビルドはリポジトリのルートを基点に行う:  docker build -f apps/web/Dockerfile .

# ---- 1. ビルド用 (SDK 入り。最終イメージには含めない) ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 先にプロジェクト定義だけコピーして restore する (ソースを変えても NuGet の取得をやり直さずに済む)
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY apps/web/WebApp.csproj apps/web/
RUN dotnet restore apps/web/WebApp.csproj

COPY apps/web/ apps/web/
RUN dotnet publish apps/web/WebApp.csproj -c Release -o /app/publish --no-restore

# ---- 2. 実行用 (ランタイムだけの小さなイメージ) ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    TZ=Asia/Tokyo

# Cookie 暗号鍵の保存先。root 以外のユーザーで書き込めるようにしておく
RUN mkdir -p /keys && chown "$APP_UID" /keys

COPY --from=build /app/publish .

# root ではなく、イメージに用意されている一般ユーザーで動かす
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "WebApp.dll"]
