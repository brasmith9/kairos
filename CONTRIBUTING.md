# Contributing

Thanks for taking an interest. This is a small project, so the process is light.

## Getting set up

The fastest path is Docker:

```bash
git clone https://github.com/brasmith9/kairos.git
cd kairos
docker compose up
```

To work on the code directly you need the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
and a PostgreSQL you can reach:

```bash
createdb Kairos
dotnet build
dotnet test
```

Put your local configuration in `src/Kairos.Api/appsettings.Development.json`. That file
is gitignored — real connection strings, API keys and signing secrets must never be
committed. If you spot one in a diff, say so on the pull request.

For callbacks pointing at `localhost`, set `Kairos:Callbacks:AllowPrivateNetworks` to
`true` in that file. It disables the SSRF guard, so keep it out of anything deployed.

## Pull requests

1. Branch off `main`.
2. Write a failing test first, then make it pass. Every behaviour change needs a test.
3. Keep the diff focused — one concern per pull request.
4. `dotnet build` must be warning-free and `dotnet test` must be green. CI checks both.
5. Describe *why* the change is needed, not only what it does.

## Tests

Anything touching `JobStorage.Current` must join the `Hangfire storage` collection — it's
a process-wide static, and xUnit runs test classes in parallel by default. Two classes
swapping storage at once will see each other's jobs.

## Style

Match the surrounding code. It is stock .NET conventions: four spaces, file-scoped
namespaces, primary constructors where they fit, `var` when the type is obvious.

Comments should explain reasoning that is not visible in the code. Don't add comments
that restate the line below them.

## Reporting a security issue

Do not open a public issue for a vulnerability. Email the maintainer instead and allow
reasonable time for a fix before disclosing.

## Open work

Good places to start, roughly easiest first:

- **Cron validation** — an invalid cron expression is currently accepted and fails later,
  inside Hangfire. Validate it at the API boundary and return 400.
- **Structured logging** — log scheduling and callback outcomes with job ids.
- **Callback delivery history** — expose attempts and failures through the API, not only
  the dashboard.
- **Per-key scoping** — every API key currently has full access to every job.
- **Pluggable authentication** — JWT bearer as an alternative to the shared API key, for
  teams with an existing identity provider.
