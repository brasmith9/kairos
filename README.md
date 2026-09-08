# Hangfire Job API

[![CI](https://github.com/brasmith9/Hangfire.Job.Api/actions/workflows/ci.yml/badge.svg)](https://github.com/brasmith9/Hangfire.Job.Api/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A small ASP.NET Core service that turns [Hangfire](https://www.hangfire.io/) into a
language-agnostic **webhook scheduler**.

Instead of writing C# jobs, a client POSTs a cron expression and a callback URL. When the
cron fires, this service POSTs back to that URL. Any service that can receive an HTTP
request can now schedule work on Hangfire, whatever it's written in.

```
POST /api/jobs  ──►  Hangfire (PostgreSQL)  ──cron fires──►  POST https://your-app/webhook
```

## Features

- Schedule and cancel recurring webhooks over a JSON API
- Durable job storage in PostgreSQL, surviving restarts
- Hangfire's automatic retries when your webhook returns a non-success status
- Password-protected dashboard, closed to the internet by default
- Callback URLs validated against SSRF (loopback, private and cloud-metadata ranges)

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- PostgreSQL 12 or later

## Quick start

```bash
git clone https://github.com/brasmith9/Hangfire.Job.Api.git
cd Hangfire.Job.Api

# Create the database Hangfire will install its schema into
createdb Hangfire

# Point the app at your database (see Configuration below)
dotnet user-secrets --project src/Hangfire.Job.Api \
  set "ConnectionStrings:DefaultConnection" \
  "Server=127.0.0.1;Port=5432;User Id=postgres;Password=yourpassword;Database=Hangfire"

dotnet run --project src/Hangfire.Job.Api
```

The service starts on `http://localhost:5222`. Hangfire creates its own tables on first run.

| URL | What |
| --- | --- |
| `http://localhost:5222/swagger` | Swagger UI (Development only) |
| `http://localhost:5222/dashboard` | Hangfire dashboard |

## Configuration

`appsettings.json` ships with placeholders. Real values belong in
`appsettings.Development.json` (gitignored), [user secrets][secrets], or environment
variables — never in a committed file.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=127.0.0.1;Port=5432;User Id=postgres;Password=CHANGE_ME;Database=Hangfire"
  },
  "Hangfire": {
    "Dashboard": {
      "Path": "/dashboard",
      "Username": "",
      "Password": ""
    },
    "Callbacks": {
      "AllowPrivateNetworks": false
    }
  }
}
```

| Setting | Default | Meaning |
| --- | --- | --- |
| `Hangfire:Dashboard:Path` | `/dashboard` | Where the dashboard is mounted |
| `Hangfire:Dashboard:Username` | *(empty)* | Empty means **local requests only**. Set it to enable Basic auth from anywhere |
| `Hangfire:Dashboard:Password` | *(empty)* | Password for Basic auth |
| `Hangfire:Callbacks:AllowPrivateNetworks` | `false` | `true` permits callbacks to localhost and private IPs. **Development only** |

Every setting can be supplied as an environment variable using `__` as the separator:

```bash
export Hangfire__Dashboard__Username=admin
export Hangfire__Dashboard__Password=a-long-random-password
```

[secrets]: https://learn.microsoft.com/aspnet/core/security/app-secrets

## API

### `POST /api/jobs` — schedule a recurring callback

```http
POST /api/jobs
Content-Type: application/json

{
  "uniqueId": "order-42",
  "cron": "*/5 * * * *",
  "callbackUrl": "https://example.com/webhooks/hangfire",
  "metaData": { "orderId": "42" }
}
```

| Field | Required | Notes |
| --- | --- | --- |
| `uniqueId` | yes | Identifies the job. Also echoed back in the callback payload |
| `cron` | yes | Standard cron expression |
| `callbackUrl` | yes | Absolute `http`/`https` URL, must not resolve to a private address |
| `metaData` | no | String map echoed back in the callback payload |

`201 Created` on success, `400 Bad Request` if the callback URL is rejected.

Scheduling is idempotent: posting the same `uniqueId` again updates that job in place
rather than creating a second one.

### `DELETE /api/jobs/{uniqueId}` — cancel a job

Pass the same `uniqueId` you scheduled with. Returns `200 OK`; removing a job that does
not exist is not an error.

### The callback

When the cron fires, the service sends:

```http
POST https://example.com/webhooks/hangfire
Content-Type: application/json

{
  "uniqueId": "order-42",
  "metadata": { "orderId": "42" }
}
```

Return any 2xx to acknowledge. Anything else throws, and Hangfire retries on its
default schedule. After a successful callback the recurring job removes itself.

## Dashboard authorization

The dashboard exposes every job, its arguments and its history, so it is never open by
default.

**Local development** — leave `Username` empty. Requests from `localhost` are allowed;
everything else is refused.

**Deployed** — set a username and password. The dashboard then answers with
`401` and a `WWW-Authenticate: Basic` challenge until valid credentials arrive:

```bash
export Hangfire__Dashboard__Username=admin
export Hangfire__Dashboard__Password=$(openssl rand -base64 24)
```

Basic auth sends credentials base64-encoded, not encrypted — **serve the dashboard over
HTTPS**. If you already have an identity provider, replace
`src/Hangfire.Job.Api/Security/DashboardBasicAuthFilter.cs` with a filter that inspects `HttpContext.User`;
it is the only place authorization is decided.

## Security notes

- **SSRF.** `POST /api/jobs` makes the server fetch a URL the caller chose. Every URL is
  checked before scheduling *and* again before each callback, since DNS can be re-pointed
  in between. Rejected: non-http(s) schemes, loopback, RFC 1918 private ranges,
  carrier-grade NAT, link-local `169.254.0.0/16` (cloud metadata), multicast, and the
  IPv6 equivalents.
- **The API itself is unauthenticated.** Anyone who can reach it can schedule jobs. Put
  it behind a gateway, mTLS or a network boundary before exposing it. Adding API
  authentication is [open work](CONTRIBUTING.md).
- **Callback payloads are not signed.** A receiver cannot yet verify a callback came from
  this service. Also open work.

## Project layout

```
src/Hangfire.Job.Api/
├── Controllers/JobsController.cs             # the JSON API
├── Dtos/                                     # request and callback shapes
├── Security/DashboardBasicAuthFilter.cs      # dashboard authorization
├── Services/CallbackUrlValidator.cs          # SSRF guard
└── Services/Providers/DynamicCallbackJob.cs  # what Hangfire executes
tests/Hangfire.Job.Api.Tests/                 # xUnit tests
```

## Tests

```bash
dotnet test
```

## Contributing

Issues and pull requests are welcome — see [CONTRIBUTING.md](CONTRIBUTING.md).

## License

[MIT](LICENSE) © Isaac Amankwaah Anane
