# EF Core Integration

## Overview

`DeltaMapper.EFCore` provides middleware that detects Castle.Core dynamic proxies emitted by EF Core's lazy loading and skips unloaded navigation properties during mapping.

## Install

```bash
dotnet add package DeltaMapper.EFCore
```

## Usage

```csharp
var config = MapperConfiguration.Create(cfg =>
{
    cfg.AddProfile<UserProfile>();
    cfg.AddEFCoreSupport();
});

IMapper mapper = config.CreateMapper();

// Proxy entities are mapped safely — no lazy load triggered
var dto = mapper.Map<User, UserDto>(proxyUser);
```

`AddEFCoreSupport()` is an extension on `MapperConfigurationBuilder` and returns the builder for fluent chaining.

## How It Works

The `EFCoreProxyMiddleware` checks if the source object's type inherits from a Castle.Core dynamic proxy base class. When a proxy is detected, unloaded navigation properties are skipped to prevent triggering lazy loading during mapping.

For non-proxy entities, the middleware is a pass-through with negligible overhead.
