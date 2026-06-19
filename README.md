# AxpoGroup Challenge

**Yansel Martínez**
[GitHub](https://github.com/ymartinez7) · 
[LinkedIn](https://www.linkedin.com/in/ymartinez7/) · ymartinez7@gmail.com

---

## Table of Contents

1. [How It Solves the Challenge](#1-how-it-solves-the-challenge)
2. [Architecture](#2-architecture)
   - [Layers](#layers)
   - [Extraction Flow](#extraction-flow)
   - [Concurrency](#concurrency)
   - [Scope per Extraction](#scope-per-extraction)
3. [Design Patterns](#3-design-patterns)
4. [Configuration](#4-configuration)
   - [Reference appsettings.json](#reference-appsettingsjson)
   - [All Configuration Keys](#all-configuration-keys)
   - [Polly Retry](#polly-retry)
5. [Execution Model](#5-execution-model)
6. [Running](#6-running)
   - [CLI](#cli)
   - [Visual Studio 2022 / 2026](#visual-studio-2022--2026)
   - [Environments](#environments)
7. [CSV Format](#7-csv-format)
8. [Tests](#8-tests)

---

## 1. How It Solves the Challenge

Worker service (.NET 10) that extracts intraday power trading positions from `PowerService.dll` and exports them as CSV files on a configurable schedule.

| Requirement | Solution |
|---|---|
| .NET Core worker | `BackgroundService` (.NET 10) |
| Aggregate per local hour (Europe/London) | `PowerTradeAgregatorService` — Period 1 → 23:00 previous day |
| CSV: header + 24 rows, `HH:MM` format | `CsvFileExportService` — `Local Time,Volume` |
| Filename `PowerPosition_YYYYMMDD_HHMM.csv` | Extraction timestamp in configurable local timezone |
| Configurable output folder (CLI or config file) | `ReportFileOptions:OutputDirectoryPath` |
| Extraction every X minutes | `PeriodicTimer` — `WorkerExecutionOptions:ExtractionIntervalMinutes` |
| Never miss a scheduled extraction | `SemaphoreSlim(1,1)` — overlapping extraction is skipped, not cancelled |
| First extraction on startup | `ExecuteAsync` calls `TryRunExtractionAsync` before the first tick |
| Production-grade logging | Serilog — structured logging with environment-configurable level |
| Resilience against PowerService failures | Polly retry with exponential backoff — configurable via `RetryOptions` |

---

## 2. Architecture

### Layers

Clean Architecture in three layers — no Domain layer:

```
       Host              ← BackgroundService worker, DI wiring, Serilog, appsettings
        ↓
    Application          ← interfaces, use cases, aggregation service, DTOs, configuration records
        ↓
  Infrastructure         ← implementations: CSV export, system clock, PowerService, retry decorator
        ↓
PowerService.dll (external — libs/)
```

**Dependency rule:** `Application` has no reference to `Infrastructure`. The `Host` references both and performs DI wiring.

### Extraction Flow

```
PowerPositionReportWorker
  └── creates IServiceScope per extraction
        └── IGeneratePowerPositionReportUseCase
              ├── IClockService               → local date (configurable timezone)
              ├── IPowerService               → PowerService.dll (with Polly retry)
              ├── IPowerTradeAgregatorService  → sum volumes per period (1–24)
              └── IFileExportService          → write CSV to disk
```

### Concurrency

`PeriodicTimer` + `SemaphoreSlim(1,1)`: if an extraction takes longer than the interval, the next tick is skipped with a warning log. `StopAsync` waits up to 60 seconds for any active extraction to finish before shutting down.

### Scope per Extraction

The worker is registered as `Singleton` (`IHostedService`). Infrastructure services are `Scoped`. The worker resolves the use case through `IServiceScopeFactory` — creating and disposing a DI scope on every extraction tick.

---

## 3. Design Patterns

| Pattern | Where | Purpose |
|---|---|---|
| **Decorator** | `ResilientPowerServiceDecorator` wraps `IPowerService` | Adds Polly retry to `GetTrades()` without modifying the use case or the original service |
| **Interface Abstraction** | `IPowerService` defined in `Application` | Decouples the application from the external DLL; enables mocking in tests without touching `PowerService.dll` |
| **Options** | `IOptions<T>` for `ReportFileOptions`, `WorkerExecutionOptions`, `RetryOptions` | Strongly-typed configuration bound from `appsettings.json` or CLI |
| **Strategy** | `IFileExportService`, `IClockService` | Swappable implementations — CSV format or clock source can be replaced without touching the use case |
| **Template Method** | `BackgroundService` (`ExecuteAsync`) | Framework-provided base class; the worker implements only the scheduling loop |
| **Dependency Injection** | `IServiceCollection` wiring in `Program.cs` and `DependencyInjection.cs` | Decouples construction from usage; all dependencies are resolved by the container — enables Scrutor decorator registration and scope-per-extraction via `IServiceScopeFactory` |
| **Retry** | `ResilientPowerServiceDecorator` via Polly `ResiliencePipeline` | Automatically retries `GetTrades()` up to `MaxRetryAttempts` times with exponential backoff on any exception — transparent to the use case |
| **Builder** | `ResiliencePipelineBuilder<T>` in `ResilientPowerServiceDecorator.BuildPipeline()` | Constructs the Polly resilience pipeline step by step via a fluent API — separates pipeline configuration from its execution |
| **Factory Method** | `IServiceScopeFactory.CreateAsyncScope()` in `PowerPositionReportWorker` | Delegates scope creation to the container — the worker never instantiates dependencies directly, only requests them from the factory |

---

## 4. Configuration

All values can be overridden at runtime via CLI using `--Section:Key=value`.

### Reference `appsettings.json`

```json
{
  "WorkerExecutionOptions": {
    "ExtractionIntervalMinutes": 10
  },
  "RetryOptions": {
    "MaxRetryAttempts": 3,
    "BaseDelay": "00:00:01"
  },
  "ReportFileOptions": {
    "OutputDirectoryPath": "C:\\PowerPositionReports",
    "TimeZone": "GMT Standard Time",
    "FileNameFormat": "PowerPosition_{0:yyyyMMdd}_{1:HHmm}.csv"
  },
  "Serilog": {
    "MinimumLevel": "Error"
  }
}
```

### All Configuration Keys

| Key | Description | Default |
|---|---|---|
| `WorkerExecutionOptions:ExtractionIntervalMinutes` | Minutes between extractions | `10` |
| `RetryOptions:MaxRetryAttempts` | Retries on PowerService failure (`0` = no retry) | `3` |
| `RetryOptions:BaseDelay` | Exponential backoff base delay (`hh:mm:ss`) | `00:00:01` |
| `ReportFileOptions:OutputDirectoryPath` | CSV output directory | `C:\PowerPositionReports` |
| `ReportFileOptions:TimeZone` | Windows timezone ID for local time | `GMT Standard Time` |
| `ReportFileOptions:FileNameFormat` | Filename pattern | `PowerPosition_{0:yyyyMMdd}_{1:HHmm}.csv` |

`TimeZone` must be a valid Windows timezone ID (e.g. `"Central European Standard Time"` for CET).

### Polly Retry

`ResilientPowerServiceDecorator` wraps `IPowerService.GetTrades()` with a Polly resilience pipeline:

- **Exponential backoff:** `BaseDelay × 2^attempt` → 1 s, 2 s, 4 s for 3 retries
- **`MaxRetryAttempts = 0`:** single attempt, no retry
- Each retry logs a `Warning` with the attempt number and exception message
- If all attempts are exhausted, the exception propagates and the current tick is aborted

---

## 5. Execution Model

```
Startup
   │
   ├─► Immediate extraction (t = 0)
   │
   ├─► Wait X minutes
   │
   ├─► Scheduled extraction (t = X)
   │
   ├─► Wait X minutes
   │
   └─► ... (runs indefinitely until Ctrl+C / stop signal)
```

If an extraction takes longer than the configured interval:

```
t= 0  [Extraction A — started]
t=10  [Tick: A still running → extraction B skipped — WARNING logged]
t=12  [Extraction A — finished]
t=20  [Tick: Extraction C — started normally]
```

---

## 6. Running

### CLI

```powershell
# Production (uses appsettings.json)
dotnet run --project src/AxpoGroupChallenge.Reports.Host

# Development (uses appsettings.Development.json — 5 min interval, Information log level)
$env:DOTNET_ENVIRONMENT = "Development"
dotnet run --project src/AxpoGroupChallenge.Reports.Host

# Override values at runtime
dotnet run --project src/AxpoGroupChallenge.Reports.Host `
  --ReportFileOptions:OutputDirectoryPath=C:\exports `
  --ReportFileOptions:TimeZone="Central European Standard Time" `
  --WorkerExecutionOptions:ExtractionIntervalMinutes=15 `
  --RetryOptions:MaxRetryAttempts=5
```

### Visual Studio 2022 / 2026

The environment variable is already configured in `launchSettings.json`:

```json
{
  "profiles": {
    "AxpoGroupChallenge.Reports.Host": {
      "commandName": "Project",
      "environmentVariables": {
        "DOTNET_ENVIRONMENT": "Development"
      }
    }
  }
}
```

Press **F5** (debug) or **Ctrl+F5** (without debug) — the profile automatically activates `appsettings.Development.json`.

To run in production mode from Visual Studio, remove `DOTNET_ENVIRONMENT` from the profile or add a second profile without the variable.

### Environments

| `DOTNET_ENVIRONMENT` | Config loaded | Log level | Interval |
|---|---|---|---|
| `Development` | `appsettings.json` + `appsettings.Development.json` | `Information` | 5 min |
| absent / `Production` | `appsettings.json` only | `Error` | 10 min |

---

## 7. CSV Format

```
Local Time,Volume
23:00,150.00
00:00,150.00
...
22:00,80.00
```

25 lines: 1 header + 24 data rows. Volumes are summed across all trades per period, formatted to 2 decimal places using `InvariantCulture`.

---

## 8. Tests

| Project | Covers |
|---|---|
| `Application.UnitTests` | `PowerTradeAgregatorService`, `GeneratePowerPositionReportUseCase` |
| `Infrastructure.UnitTests` | `CsvFileExportService`, `ResilientPowerServiceDecorator` |
| `Host.IntegrationTests` | End-to-end flow with real DI |
