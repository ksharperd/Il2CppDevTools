# Building

## Setup .Net SDK

Obtain nightly build from [here](https://github.com/dotnet/installer#table) or latest release from official download page, but **nightly build is highly recommended**.

## Setup Nuget Sources

Add the following sources to your nuget config.

```xml
  <packageSources>
    <add key="dotnet10" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet10/nuget/v3/index.json" />
  </packageSources>
```
