# Kairos

**Scheduled webhooks, self-hosted.** Cron in, HTTP callback out.

[![CI](https://github.com/brasmith9/kairos/actions/workflows/ci.yml/badge.svg)](https://github.com/brasmith9/kairos/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Kairos turns [Hangfire](https://www.hangfire.io/) into a language-agnostic scheduler.
Instead of writing C# jobs, a client POSTs a cron expression (or a timestamp) and a
callback URL. When it fires, Kairos POSTs back — signed, so you can verify it.

```
POST /api/jobs  ──►  Kairos (Hangfire + PostgreSQL)  ──fires──►  POST https://your-app/webhook
```

Any service that speaks HTTP can now schedule work, whatever it's written in.

## Why this exists

If you already run Hangfire, your .NET services have durable scheduling and your Node,
Python and Go services have nothing. Kairos exposes the infrastructure you already
operate as an HTTP API the whole organisation can call — no new datastore, no new thing
to run.

If you don't run Hangfire, it's a self-hosted alternative to Cloud Scheduler and
EventBridge Scheduler that keeps your callback URLs and job history on your own hardware.

| | Kairos | Cloud Scheduler / EventBridge | QStash |
| --- | --- | --- | --- |
| Self-hosted | yes | no | no |
| Storage | your PostgreSQL | vendor | vendor |
| Signed callbacks | yes | no (IAM instead) | yes |
| Retries | Hangfire's policy | yes | yes |
| Built-in UI | Hangfire dashboard | console | console |
| Cost | your server | per job | per message |

## Quick start

```bash
git clone https://github.com/brasmith9/kairos.git
cd kairos
docker compose up
```

That's PostgreSQL plus Kairos on <http://localhost:8080>, with development credentials
baked into `docker-compose.yml`. Schedule something:

```bash
curl -X POST http://localhost:8080/api/jobs \
  -H 'Content-Type: application/json' \
  -H 'X-Api-Key: dev-api-key' \
  -d '{
        "uniqueId": "order-42",
        "cron": "*/5 * * * *",
        "callbackUrl": "https://example.com/webhooks/kairos",
        "metaData": { "orderId": "42" }
      }'
```

The dashboard is at <http://localhost:8080/dashboard> (`admin` / `admin`).

> The compose file sets an API key, dashboard credentials and
> `AllowPrivateNetworks=true` because container traffic isn't loopback and callbacks to
> sibling services resolve to private addresses. **They are development values.** See
> [Configuration](#configuration) before deploying.

### Without cloning

Releases publish a multi-arch image to GHCR, so a `compose.yml` is the whole install:

```yaml
services:
  db:
    image: postgres:16-alpine
    environment:
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: Kairos
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres -d Kairos"]
      interval: 5s
      retries: 10

  api:
    image: ghcr.io/brasmith9/kairos:latest
    depends_on:
      db:
        condition: service_healthy
    ports:
      - "8080:8080"
    environment:
      ConnectionStrings__DefaultConnection: "Server=db;Port=5432;User Id=postgres;Password=postgres;Database=Kairos"
      Kairos__Api__Keys__0: dev-api-key
      Kairos__Dashboard__Username: admin
      Kairos__Dashboard__Password: admin
      Kairos__Callbacks__SigningSecret: dev-signing-secret
```

Same development values as above, and the same warning applies. Pin a real tag rather
than `latest` for anything you intend to keep running.

### Running from source

```bash
dotnet run --project src/Kairos.Api      # needs PostgreSQL on 127.0.0.1:5432
dotnet test
```

## Configuration

`appsettings.json` ships with placeholders. Real values belong in
`appsettings.Development.json` (gitignored), [user secrets][secrets], or environment
variables — never in a committed file. Every setting maps to an environment variable
using `__` as the separator, e.g. `Kairos__Api__Keys__0`.

| Setting | Default | Meaning |
| --- | --- | --- |
| `ConnectionStrings:DefaultConnection` | — | PostgreSQL connection string |
| `Kairos:Api:Keys` | *(empty)* | Accepted API keys. Empty means **local requests only** |
| `Kairos:Dashboard:Path` | `/dashboard` | Where the dashboard is mounted |
| `Kairos:Dashboard:Username` | *(empty)* | Empty means **local requests only**. Set it to enable Basic auth |
| `Kairos:Dashboard:Password` | *(empty)* | Password for dashboard Basic auth |
| `Kairos:Callbacks:SigningSecret` | *(empty)* | HMAC secret. Empty means callbacks are unsigned |
| `Kairos:Callbacks:AllowPrivateNetworks` | `false` | `true` permits callbacks to localhost and private IPs. **Development only** |

Both auth mechanisms fail closed the same way: configure nothing and only local callers
get through, so an unconfigured deployment is never open to the internet.

[secrets]: https://learn.microsoft.com/aspnet/core/security/app-secrets

## API

Every `/api` route requires `X-Api-Key` unless the caller is local and no keys are set.

### `POST /api/jobs` — schedule a callback

```jsonc
{
  "uniqueId": "order-42",
  "cron": "*/5 * * * *",              // recurring…
  // "runAt": "2026-01-01T09:00:00Z", // …or one-shot. Exactly one of the two.
  "callbackUrl": "https://example.com/webhooks/kairos",
  "metaData": { "orderId": "42" }
}
```

| Field | Required | Notes |
| --- | --- | --- |
| `uniqueId` | yes | Identifies the job, echoed back in the callback |
| `callbackUrl` | yes | Absolute `http`/`https` URL, must not resolve to a private address |
| `cron` | one of | Cron expression for a recurring job |
| `runAt` | one of | ISO-8601 timestamp for a one-shot job |
| `metaData` | no | String map echoed back in the callback |

Returns `201` with the job's `id`. For a cron job that's your `uniqueId`, and posting it
again updates the job in place. For a one-shot job it's a generated Hangfire id — keep it
if you intend to cancel.

`400` if the callback URL is rejected, or if neither/both of `cron` and `runAt` are given.

### `GET /api/jobs` — list scheduled jobs

```json
[
  {
    "id": "order-42",
    "type": "recurring",
    "cron": "*/5 * * * *",
    "callbackUrl": "https://example.com/webhooks/kairos",
    "nextExecution": "2026-01-01T09:05:00Z",
    "lastExecution": "2026-01-01T09:00:00Z"
  }
]
```

`type` is `recurring` or `scheduled` (one-shot).

### `DELETE /api/jobs/{id}` — cancel a job

Takes either kind of id. Returns `200` when the job was cancelled, and also when the id is
well-formed but unknown — storage can't distinguish "just deleted" from "never existed".
An id that no storage could have issued returns `404`.

### `GET /health` — readiness

Returns `200` when Kairos can reach its database, `503` when it can't. No API key: an
orchestrator has no way to send one. Note that Kairos won't finish starting at all if the
database is unreachable, so this reports a database lost *after* startup.

### OpenAPI

The generated document is served in every environment at `/swagger/v1/swagger.json`, with
the browsable UI at `/swagger`. Point a client generator at it:

```bash
npx @openapitools/openapi-generator-cli generate \
  -i http://localhost:8080/swagger/v1/swagger.json -g typescript-fetch -o ./kairos-client
```

It documents the same three routes as this README, so it is not privileged information —
but it is unauthenticated, so put it behind your proxy if your deployment differs.

### The callback

```http
POST https://example.com/webhooks/kairos
Content-Type: application/json
X-Kairos-Signature: sha256=ad71a7eb…

{ "uniqueId": "order-42", "metadata": { "orderId": "42" } }
```

Return any 2xx to acknowledge. Anything else throws, and Hangfire retries on its default
schedule. Recurring jobs keep running until you delete them; one-shot jobs are done after
a successful callback.

**Verifying the signature.** The HMAC-SHA256 is computed over the raw request body with
`Kairos:Callbacks:SigningSecret`. Compare in constant time:

```python
import hmac, hashlib
expected = "sha256=" + hmac.new(secret.encode(), raw_body, hashlib.sha256).hexdigest()
if not hmac.compare_digest(expected, request.headers["X-Kairos-Signature"]):
    abort(401)
```

```js
const expected = "sha256=" + crypto.createHmac("sha256", secret).update(rawBody).digest("hex");
if (!crypto.timingSafeEqual(Buffer.from(expected), Buffer.from(req.get("X-Kairos-Signature")))) return res.sendStatus(401);
```

Sign over the **raw** bytes, before any JSON parsing and re-serialisation.

## Security notes

- **SSRF.** `POST /api/jobs` makes the server fetch a URL the caller chose. Every URL is
  checked before scheduling *and* again before each callback, since DNS can be re-pointed
  in between. Rejected: non-http(s) schemes, loopback, RFC 1918 private ranges,
  carrier-grade NAT, link-local `169.254.0.0/16` (cloud metadata), multicast, and the
  IPv6 equivalents.
- **Transport.** API keys and Basic credentials are sent in clear. Terminate TLS in front
  of Kairos; the container speaks plain HTTP on 8080 by design.
- **Callback delivery isn't audited via the API yet.** Use the dashboard for history.

## Licensing

Kairos itself is [MIT](LICENSE). It depends on Hangfire, which is **LGPL v3** — fine to
use and to run, but if you redistribute Kairos (a modified fork, or your own container
image) the LGPL obligations for that dependency travel with it. Check the terms for your
situation; a commercial Hangfire licence is available if LGPL doesn't suit you.

Kairos is not affiliated with or endorsed by HangfireIO.

## Project layout

```
src/Kairos.Api/
├── Controllers/JobsController.cs             # the JSON API
├── Dtos/                                     # request, callback and summary shapes
├── Security/ApiKeyAuthenticator.cs           # API key guard
├── Security/DashboardBasicAuthFilter.cs      # dashboard authorization
├── Services/CallbackSigner.cs                # HMAC callback signatures
├── Services/CallbackUrlValidator.cs          # SSRF guard
└── Services/Providers/DynamicCallbackJob.cs  # what Hangfire executes
tests/Kairos.Api.Tests/                       # xUnit tests
```

## Contributing

Issues and pull requests are welcome — see [CONTRIBUTING.md](CONTRIBUTING.md).

## License

[MIT](LICENSE) © Isaac Amankwaah Anane
