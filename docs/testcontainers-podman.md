# Testcontainers on Docker and podman

Status: **implemented** in `src/BookingApi.Tests/Fixtures/ContainerRuntime.cs`, ported from
NodeBookingApi (`test/support/container-runtime.ts`). Windows (podman's named pipe) is out of scope.

## Problem

Testcontainers finds the Docker API socket itself: `DOCKER_HOST`, `~/.testcontainers.properties`,
`/var/run/docker.sock`, then a few rootless *Docker* paths. Podman serves the same API, but the Node
library never probes `podman.sock`. A podman-only machine then needs `DOCKER_HOST` exported by hand,
and on macOS even that isn't stable: the socket lives under `$TMPDIR` and its path changes.

## Resolution order

Run once, before the first container is built:

1. `DOCKER_HOST` set, or `~/.testcontainers.properties` exists → do nothing. The properties file
   can override `DOCKER_HOST`, so setting the variable could contradict it.
2. `/var/run/docker.sock` exists (Linux, CI) → do nothing.
3. Rootless podman on Linux → `$XDG_RUNTIME_DIR/podman/podman.sock`, falling back to
   `/run/user/<uid>/podman/podman.sock`.
4. Podman machine on macOS → take the socket's **file name** from
   `podman machine inspect --format '{{.ConnectionInfo.PodmanSocket.Path}}'` and look for it under
   `$TMPDIR/podman/`, `$(getconf DARWIN_USER_TEMP_DIR)/podman/` and `/tmp/podman/`. Don't trust the
   directory it reports: it comes from the current shell's `$TMPDIR`, not the one the machine
   started under, and inside `nix develop` it never exists. `DARWIN_USER_TEMP_DIR` is the same in
   every shell.
5. Podman installed, socket not found → warn "is the machine running? `podman machine start`", then
   let Testcontainers report the failure.
6. Socket found → set `DOCKER_HOST=unix://<socket>` and `TESTCONTAINERS_RYUK_DISABLED=true`.

A missing or failing `podman`/`getconf` counts as "not available", not an error. Give the calls a
timeout.

## Ryuk

Disable it under podman only. Ryuk bind-mounts the socket into its own container, and rootless
podman refuses that ("operation not supported"). Teardown must then remove the container itself.
After a hard kill of the test run, clean up with `podman ps -a` / `podman rm`.

## Porting to BookingApi.Tests

- **Hook.** Call the resolver first in `PostgresAssemblyFixture.InitializeAsync`, before
  `new PostgreSqlBuilder(...)`. Testcontainers for .NET reads its settings the first time they're
  used, so `Environment.SetEnvironmentVariable` after that is too late.
- **Alternatives to environment variables.** `TestcontainersSettings.ResourceReaperEnabled = false`
  for Ryuk, `.WithDockerEndpoint(...)` per builder. Verify both against Testcontainers 4.12.
- **Teardown.** `CleanupAsync` calls `StopAsync()`, which leaves the container in place. Use
  `DisposeAsync()` so it's removed when Ryuk is off.
- **Podman probing.** Testcontainers for .NET 4.12 does not probe podman sockets, so the whole
  resolution order is needed. In .NET, step 1 also skips when `TESTCONTAINERS_HOST_OVERRIDE` or
  `TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE` is set, and only a properties file with a `docker.host`
  entry counts.
- **Out of scope.** `postgres-in-podman.sh` (the dev database) doesn't go through Testcontainers.
