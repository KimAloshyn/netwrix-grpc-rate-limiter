# Rate Limiter gRPC Service

A standalone C# gRPC service implementing **Fixed Window rate limiting** in memory. Other services can configure limits for named resources and check whether a client is allowed to make another request.

The rate limiting algorithm is implemented from scratch using .NET collections and synchronization primitives. **No existing rate limiting library is used.** ASP.NET Core gRPC is used only for RPC hosting and communication.

## Requirements covered

- `ConfigureResource` creates or updates a resource-specific request limit and window duration.
- `CheckRateLimit` returns whether a request is allowed and counts accepted requests.
- Counters reset when their fixed-duration window expires.
- Requests are handled safely under concurrency using a per-client/resource lock.
- The service listens on **port 5000** using **HTTP/2**.
- Resource configurations and counters are held in process memory; persistence is not reqiured.

## Tech

- C# / .NET 10
- ASP.NET Core gRPC (`Grpc.AspNetCore`)
- Protocol Buffers (`ratelimiter.proto`)
- `ConcurrentDictionary` and `lock` for thread safety
- xUnit for automated testing
- `TimeProvider` and `FakeTimeProvider` for deterministic window-expiration tests

## Release v1.0.0

The first release "Rate Limiter gRPC Service v1.0.0" is available to download in the git repo Releases. ZIP should be downloaded and unpacked, where the executable file will be ready to run.

## Build and run

### Prerequisites

- .NET 10 SDK (verify with `dotnet --version`)
- Windows, Linux, or macOS for development; publish for Windows to create the requested `.exe`

Run the following commands from the solution root:

```powershell
dotnet restore RateLimiterSolution.sln
dotnet build RateLimiterSolution.sln -c Release
dotnet test RateLimiterSolution.sln -c Release
dotnet run --project src/YourRateLimiterService
```

The server listens at **`http://localhost:5000`** (plaintext HTTP/2, also called *h2c*). Stop the server with `Ctrl+C`.


## Publish the Windows executable

From the solution root:

```powershell
dotnet publish src/YourRateLimiterService/YourRateLimiterService.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:PublishTrimmed=false `
  -o publish
```

The output includes `publish/YourRateLimiterService.exe`. Run it on Windows:

```powershell
.\publish\YourRateLimiterService.exe
```


## How the algorithm works

A resource configuration contains `MaxRequests`, `WindowSeconds`, and a `Version` used to recognize updates. A separate state is maintained for each `(client_id, resource)` pair, containing the accepted-request counter and window start time.

For each `CheckRateLimit` call:

1. Reject an empty/whitespace client ID or an unconfigured/invalid resource.
2. Locate or create the state for `(client_id, resource)`.
3. Lock that state and read the current resource configuration.
4. Reset its counter when the time window has expired or the configuration version has changed.
5. If the count has reached `MaxRequests`, return `allowed = false`; otherwise increment the count and return `allowed = true`.

**Only accepted requests increase the counter.** A denied request does not move the window boundary.

### Concurrency design

`ConcurrentDictionary` provides safe concurrent access to the configuration and state maps. A lock on each `WindowState` protects the *check->reset->increment* sequence as a single critical section. Requests for different client/resource pairs can proceed independently; requests targeting the same pair are serialized briefly. Resource configurations are immutable records that are replaced through `ConcurrentDictionary.AddOrUpdate`.

## Assumptions and decisions

### 1. Limit scope: per client **and** resource

`ConfigureResource` defines the **policy per resource**, but request counters are tracked per **`(client_id, resource)` pair**. For example, if `api/users` allows 100 requests per 60 seconds, `user1` and `user2` each receive a separate 100-request allowance.

The Activity requirements do not explicitly say whether counters are per-client or shared globally. Per-client/resource enforcement was chosen because `CheckRateLimit` includes both `client_id` and `resource`.

### 2. Window semantics

Windows are **first-request-anchored fixed windows**: a client's first request starts a window of the configured duration. The window does not slide with later requests. Once it expires, the next request starts a new window. Windows are reset lazily on access rather than by background timers.

### 3. Configuration changes reset counters

When a resource's limits or window duration change, its configuration version increases. On the **next check** for each affected client/resource pair, the engine detects the new version and resets that counter, starting a new window with the updated settings. Reapplying an identical configuration does **not** reset counters.

### 4. In-memory state and synchronous logic

Both configurations and counters are kept **in memory**. They are lost when the process exits or restarts; different server instances do not share their counters.

The engine performs short, synchronous, in-memory operations; there is no external I/O to await. If storage moves to **Redis or a database**, the storage calls should use asynchronous APIs and the gRPC handlers should be updated to use `async`/`await`.

## Testing

Run:

```powershell
dotnet test RateLimiterSolution.sln -c Release
```

Tests are grouped by behavior: Request tests, Configuration tests, Window tests and Concurrency tests.

Manual gRPC checks can be performed with Postman and the provided `.proto` file.

## Performance and trade-offs

- Average-case configuration and state lookups are approximately **O(1)** using hash-based dictionaries.
- The hot path uses a small per-key lock; unrelated clients/resources do not contend on the same lock.
- Memory usage scales with the number of distinct **client/resource pairs** ever seen, plus configured resources.
- **Known limitation:** unused `WindowState` entries are not currently evicted. Memory may grow without bound when many new client IDs appear.
- Fixed Window rate limiting allows **boundary bursts**: traffic near the end of one window and the start of the next can exceed a nominal per-window rate over a short interval.
- Because counters are in process memory, this implementation is designed for a **single service instance**, not distributed enforcement across multiple replicas.

## Improvements with more time

1. **Stale-state cleanup:** expire inactive counters, memory clean-up mechanism.
2. **Observability:** add structured logs.

## Production-wise improvements

1. **Security:** Enable TLS on the production environemnt.
2. **Distributed enforcement:** use Redis or another coordinated store for counters when running multiple instances;

