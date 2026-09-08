# Contributing

Thanks for taking an interest. This is a small project, so the process is light.

## Getting set up

```bash
git clone https://github.com/brasmith9/Hangfire.Job.Api.git
cd Hangfire.Job.Api
createdb Hangfire
dotnet build
dotnet test
```

Copy your local configuration into `src/Hangfire.Job.Api/appsettings.Development.json`. That
file is gitignored — real connection strings and passwords must never be committed. If
you find one in a diff, say so on the pull request.

For local callback testing against `localhost`, set
`Hangfire:Callbacks:AllowPrivateNetworks` to `true` in that file. It disables the SSRF
guard, so keep it out of anything deployed.

## Pull requests

1. Branch off `main`.
2. Write a failing test first, then make it pass. Every behaviour change needs a test.
3. Keep the diff focused — one concern per pull request.
4. `dotnet build` must be warning-free and `dotnet test` must be green. CI checks both.
5. Describe *why* the change is needed, not only what it does.

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
  inside Hangfire.
- **Signed callbacks** — an HMAC header so receivers can verify a callback really came
  from this service.
- **API authentication** — the job endpoints are open to anyone who can reach them.
- **Structured logging** — log scheduling and callback outcomes with job ids.
