# AxpoGroupChallenge.Reports

Worker service that extracts intraday power trading positions from Axpo's `PowerService` and exports them as CSV files at a configurable interval.

## What it does

On startup — and then every `ExtractionIntervalMinutes` minutes — the worker:

1. Retrieves today's power trades from `PowerService`
2. Aggregates volumes across all trades, grouped by period (1–24)
3. Maps each period to a local hour (Period 1 → 23:00, Period 2 → 00:00, …, Period 24 → 22:00)
4. Writes a CSV file named `PowerPosition_YYYYMMDD_HHMM.csv` to the configured output directory

## Architecture

Three projects under `src/`:

| Project | Responsibility |
|---|---|
| `Application` | Interfaces, use cases, aggregation service, configuration records |
| `Infrastructure` | CSV export, system clock, PowerService adapter |
| `Host` | Background worker, periodic executor, DI wiring, Serilog |

`libs/PowerService.dll` is an external dependency referenced only by `Infrastructure`.

## Running

```bash
dotnet run --project src/AxpoGroupChallenge.Reports.Host
```

The output directory defaults to `C:\PowerPositionReports`. Override via CLI:

```bash
dotnet run --project src/AxpoGroupChallenge.Reports.Host \
  --ReportFileOptions:OutputDirectoryPath=C:\exports \
  --ReportFileOptions:TimeZone="GMT Standard Time" \
  --WorkerExecutionOptions:ExtractionIntervalMinutes=15
```

## CSV format

```
Local Time,Volume
23:00,100.5
00:00,200.0
01:00,-50.25
...
22:00,175.0
```

25 lines total: 1 header + 24 data rows. Volumes are summed across all trades for each period.
