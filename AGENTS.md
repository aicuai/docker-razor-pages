# AGENTS.md

Working rules for AI coding agents (and human contributors).
Read this to the end before working in this repository.

## Purpose of this repository

This is a **teaching sample** of ASP.NET Core Razor Pages that people with Classic ASP experience can look at and think "I can read this".
It is referenced from technical blog articles. The priorities are therefore:

1. **Readability** ― someone who knows ASP can read it top to bottom and understand it
2. **Correctness and safety** ― people who copy the sample must not end up writing vulnerable code
3. Rich features, abstraction and performance are **not** priorities

A change that looks like "better design" is not adopted if it adds files or concepts the reader has to follow.

## Layout

| Path | Contents | May be changed? |
|---|---|---|
| `apps/web/` | The Razor Pages app | Yes |
| `tests/WebApp.Tests/` | Automated tests (xUnit) | Yes |
| `docs/` | Guide (`razor-pages-primer.md`) and cheat sheet (`asp-to-razor-cheatsheet.md`) | Yes |
| `README.md`, `README.ja.md` | English README (primary) and its Japanese version | Yes (always both; see "Languages") |
| `legacy/asp/` | Period-style Classic ASP version. **For side-by-side reading only; never run or built** | Limited (see "Handling legacy") |
| `docker-compose.yml`, `apps/web/Dockerfile` | Runtime environment | Yes |
| `Directory.Build.props`, `Directory.Packages.props`, `global.json` | Settings shared by all projects | Only when needed |

## Commands

Run everything from the repository root. .NET SDK 10.0 and Docker are required.

```bash
docker compose up -d db          # Start PostgreSQL (needed for tests and dotnet run)
dotnet build                     # Build. Warnings are treated as errors
dotnet test                      # Tests. They connect to a real PostgreSQL
dotnet watch --project apps/web  # Run for development (http://localhost:5121)
docker compose up --build        # Run everything in containers (http://localhost:8080)
```

- The tests create a test-only database, `aspdemo_test`, on their own. The connection can be changed with the `TEST_CONNECTION_STRING` environment variable.
- In an environment that cannot reach the database, the tests cannot run. Report them honestly as "not run"; do not report success.
- If the .NET SDK is not installed on the host, build and test inside the SDK container instead:

  ```bash
  docker compose up -d db
  docker run --rm --network docker-razor-pages_default -v "$PWD":/src -w /src \
    -e TEST_CONNECTION_STRING="Host=db;Port=5432;Database=aspdemo_test;Username=app;Password=app" \
    mcr.microsoft.com/dotnet/sdk:10.0 sh -c 'dotnet build && dotnet test --no-build'
  ```

  The network name follows the folder name (`<folder>_default`).

## Definition of done

Before reporting a change as "done", confirm all of the following.

- [ ] `dotnet build` passes with 0 warnings and 0 errors
- [ ] `dotnet test` passes in full
- [ ] If a screen changed, you actually opened it in a browser (or over HTTP)
- [ ] If behaviour changed, you added or updated tests
- [ ] If "Keeping documents in sync" below applies, you fixed the documents too

## Languages

The readers of the sample are Japanese developers; the repository front page is in English.

- **Japanese**: screen text, code comments, everything under `docs/`, `legacy/asp/README.md`, `README.ja.md`, and commit messages.
- **English**: `README.md` (primary), `AGENTS.md`, and identifiers (class and variable names).
- `README.md` and `README.ja.md` must say the same thing. Change both in the same commit. If they disagree, `README.md` is the one to trust, and the disagreement is a bug to fix.

## Coding conventions

### General

- Comments explain "why it is done this way" and "how it was written in ASP". Where a matching ASP idiom exists, add it in the form `// ASP: Request.Form("x")`.
- Assume the reader is a C# beginner. Do not use clever language features, custom base classes, reflection or source generators.
- Do not add NuGet packages lightly. When you add one, put the version in `Directory.Packages.props` and add the reason to "Tech stack" in the READMEs.
- Do not bring in JavaScript or CSS frameworks. The only stylesheet is `apps/web/wwwroot/css/site.css`.

### Razor Pages

- Pages are deliberately written in two styles. Do not unify them.
  - **Single-file style** (`@functions`): `Pages/Index.cshtml`, `Pages/Todo.cshtml`. Mostly plain HTML (`name="..."`).
  - **Separate PageModel style** (`.cshtml` + `.cshtml.cs`): `Pages/Employees/`. Uses tag helpers (`asp-for` and so on).
- Operations that change state (create, update, delete) must be **POST**. Never delete from a GET link.
- After a successful POST, always `RedirectToPage(...)` (to prevent double submission). Pass the result message through `TempData["Message"]`.
- On an input error, redisplay the same screen with `return Page();`. Before that, reload any display data such as lists and options.
- Properties received from a form or URL that may be omitted must be nullable (`string?`, `int?`). A non-nullable `string` is automatically treated as required.
- Do not combine `asp-all-route-data` and `asp-route-xxx` on one tag (the `asp-route-xxx` value is not written to the URL). Pass extra values as hidden fields, or add them to the dictionary.
- Do not disable antiforgery token validation (do not add `[IgnoreAntiforgeryToken]`).
- Do not use `@Html.Raw`. If it is truly needed, explain in a comment that the value does not come from user input.
- Do not embed `@value` directly inside JavaScript. Put it in a `data-*` attribute and read it with `this.dataset` (see the delete confirmation in `Employees/Index.cshtml`).

### Database

- Write SQL by hand and run it with Dapper. Do not introduce an O/R mapper such as Entity Framework Core (the point is to keep the feel of ADO).
- **Always pass values as `@parameters`. Never build SQL with values embedded by string concatenation or `$"..."`.**
  Concatenating constant SQL fragments is fine (for example `SelectColumns` in `EmployeeRepository`).
- Pass `LIKE` search input through `EmployeeRepository.ToLikePattern`, which escapes `%` and `_`.
- Table definitions live only in `apps/web/Data/schema.sql`. It runs on every startup, so it must be written to **give the same result no matter how many times it runs** (`IF NOT EXISTS` and the like).
- Column names are `snake_case`; C# properties are `PascalCase`. Dapper maps them automatically (configured in `Program.cs`).
- Date-only values use the `date` type + `DateOnly`. Date-times use `timestamptz` + `DateTime` (UTC), converted with `ToLocalTime()` for display.

### Character encoding

- Source, HTML and the database are all UTF-8.
- Shift_JIS is used only for the CSV export for Excel (`Csv.cs`). Do not bring Shift_JIS anywhere else.

## Keeping documents in sync

In this repository, the worst defect is code and explanation disagreeing. When you make one of the changes below, fix the documents in the same commit.

| What changed | Also fix |
|---|---|
| A page added or removed, or a URL changed | The tables in `README.md` and `README.ja.md`; section 5.1 of `docs/razor-pages-primer.md` |
| Settings in `Program.cs` | Section 9 (pitfalls) of `docs/razor-pages-primer.md`; "Settings" in both READMEs |
| A rewrite that changes the correspondence with ASP | The "example" column of `docs/asp-to-razor-cheatsheet.md`; the mapping table in `legacy/asp/README.md` |
| `Pages/Index.cshtml` or `legacy/asp/default.asp` | The excerpts under "Start with `default.asp`" in both READMEs |
| Package or runtime versions | "Tech stack" in both READMEs, `Dockerfile`, `global.json`, and `.github/workflows/ci.yml` (not added yet) |
| The look of a screen | The screenshots in `docs/images/` |
| Either README | The other README |

When you write a fact in a document (a product's support deadline, a version, a date), check a primary source and add a link to it. Do not write what you cannot confirm.

## Handling legacy

- `legacy/asp/` deliberately preserves "the way it was commonly written back then". The SQL string concatenation and the GET delete are **intentional**. Do not fix them.
- You may touch it only to keep the side-by-side reading valid: when a feature in `apps/web` changes, update the matching `.asp`.
- There is no environment to run it (Windows + IIS), so it cannot be verified. Do not remove the statement in `legacy/asp/README.md` that it has not been run.
- Do not bring code from `legacy/asp/` into `apps/web`. Do not add dependencies on COM components such as BASP21.

## Do not

- Put realistic-looking personal data in the seed data (names are fictional, e-mail addresses use `example.co.jp`, no phone numbers).
- Commit passwords, API keys or production connection details. Write no credentials other than the sample `app` / `app`.
- Weaken validation, delete tests or skip tests in order to make tests pass.
- Do large refactorings, folder restructurings or framework swaps that were not asked for.

## Commits and pull requests

- Write commit messages in Japanese, with "what was done" on the first line (example: `従業員一覧に部署での絞り込みを追加`).
- One pull request does one thing.
- In the pull request description, write the reason for the change, how you verified it (the commands you ran and their results), and screenshots if a screen changed.
