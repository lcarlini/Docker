# Contributing

Thanks for helping improve Dockyard. Keep changes focused on container
reliability, security, observability, or the diagnostics API.

## Development workflow

1. Create a branch from `main`.
2. Restore and build the solution:
   ```bash
   dotnet restore
   dotnet build --configuration Release --no-restore
   ```
3. Add or update tests for behavior changes.
4. Run the test suite:
   ```bash
   dotnet test --configuration Release --no-build
   ```
5. If Docker files changed, build the image and validate Compose:
   ```bash
   docker build --tag dockyard-api:local .
   docker compose config --quiet
   ```
6. Open a pull request that explains the reason for the change and how it was
   verified.

## Guidelines

- Never expose credentials, authorization headers, or cookies from diagnostics.
- Keep the final runtime image non-root and avoid adding unnecessary packages.
- Prefer platform features over dependencies when the result stays clear.
- Treat compiler warnings as errors and keep tests deterministic.

By contributing, you agree that your work will be licensed under the MIT
License.
