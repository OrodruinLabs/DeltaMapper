# OpenTelemetry Tracing

## Overview

`DeltaMapper.OpenTelemetry` emits `System.Diagnostics.Activity` spans for every mapping operation, compatible with any OpenTelemetry SDK.

## Install

```bash
dotnet add package DeltaMapper.OpenTelemetry
```

## Usage

```csharp
var config = MapperConfiguration.Create(cfg =>
{
    cfg.AddProfile<UserProfile>();
    cfg.AddMapperTracing();
});
```

Wire up the `"DeltaMapper"` source in your OpenTelemetry SDK setup:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("DeltaMapper")
        .AddOtlpExporter());
```

## Span Details

Each span is named `"Map {SourceType} -> {DestType}"` and carries:

| Tag | Value |
|---|---|
| `mapper.source_type` | Fully-qualified source type name |
| `mapper.dest_type` | Fully-qualified destination type name |

On error: span status is set to `Error` with an `"exception"` event containing `exception.type` and `exception.message`.

## Zero-Overhead Fast Path

`ActivitySource.HasListeners()` is checked before any span creation. When no listener is attached, the entire tracing path is bypassed with zero allocation.
