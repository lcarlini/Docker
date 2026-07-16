# Dockyard

[![CI](https://github.com/lcarlini/Docker/actions/workflows/ci.yml/badge.svg)](https://github.com/lcarlini/Docker/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Docker](https://img.shields.io/badge/Docker-ready-2496ED?logo=docker&logoColor=white)](https://www.docker.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A compact, production-minded reference for containerizing ASP.NET Core services.
Dockyard exposes runtime and request diagnostics that make it useful for testing
container networking, reverse proxies, health checks, and deployment platforms.

**Pages:** [Docker for .NET, explained](https://lcarlini.github.io/Docker/) — a short visual walkthrough of how this project is containerized.

## Why this project

Docker examples often stop at `docker build`. Dockyard demonstrates the details
that matter after an image reaches an environment:

- multi-stage builds with a small ASP.NET runtime image;
- a non-root process with all Linux capabilities dropped;
- a read-only filesystem and an explicit temporary filesystem;
- liveness and readiness endpoints plus an image-level health check;
- graceful init and restart behavior through Docker Compose;
- deterministic .NET builds, automated tests, and CI image validation;
- correlation IDs, structured responses, rate limiting, and safe diagnostics.

## Run it

### Docker Compose

```bash
docker compose up --build
```

The API is available at `http://localhost:8080`. Set `DOCKYARD_PORT` to publish
another host port:

```bash
DOCKYARD_PORT=8090 docker compose up --build
```

### Local .NET SDK

Requires the .NET 10 SDK:

```bash
dotnet restore
dotnet run --project src/Dockyard.Api
```

Use the HTTP URL printed by ASP.NET Core (port `5138` with the checked-in launch
profile).

## Try the API

```bash
curl http://localhost:8080/api/v1/runtime
curl -H "X-Correlation-ID: demo-123" \
  "http://localhost:8080/api/v1/inspect?source=readme"
curl http://localhost:8080/health/ready
```

| Endpoint | Purpose |
| --- | --- |
| `GET /api/v1/runtime` | Reports framework, OS, architecture, and process uptime |
| `GET /api/v1/inspect` | Shows request, client, and proxy metadata |
| `GET /health/live` | Confirms that the process can serve requests |
| `GET /health/ready` | Runs registered readiness checks |

Only a small allowlist of request headers is returned. Authorization, cookies,
and arbitrary headers are intentionally excluded from diagnostics.

## Container design

```text
source ──► restore layer ──► release publish ──► non-root runtime image
                                                    │
                                                    └── built-in health probe
```

The application doubles as the container health-check executable. Running
`dotnet Dockyard.Api.dll --health-check` probes the local liveness endpoint, so
the runtime image does not need `curl`, `wget`, or an extra package layer.

The Compose service applies additional runtime controls:

```yaml
read_only: true
security_opt:
  - no-new-privileges:true
cap_drop:
  - ALL
```

## Quality checks

```bash
dotnet build --configuration Release
dotnet test --configuration Release --no-build
docker build --tag dockyard-api:local .
docker compose config --quiet
```

CI runs the .NET build and test suite, publishes coverage as an artifact, and
verifies that the production container image builds successfully.

## Repository layout

```text
.
├── .github/workflows/ci.yml
├── docs/                      # GitHub Pages site
├── src/Dockyard.Api/          # diagnostics API
├── tests/Dockyard.Api.Tests/  # integration tests
├── compose.yaml               # hardened local runtime
├── Dockerfile                 # multi-stage production image
└── Directory.Build.props      # shared quality settings
```

Publish the explainer from **Settings → Pages**: source branch `main`, folder `/docs`.
The site is then available at [lcarlini.github.io/Docker](https://lcarlini.github.io/Docker/).

## Contributing

Issues and focused pull requests are welcome. See [CONTRIBUTING.md](CONTRIBUTING.md)
for the development workflow and [SECURITY.md](SECURITY.md) for responsible
vulnerability reporting.

## License

Licensed under the [MIT License](LICENSE).
