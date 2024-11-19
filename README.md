![GitHub Actions](https://github.com/ironfede/openmcdf/actions/workflows/dotnet-desktop.yml/badge.svg)
[![NuGet Version](https://img.shields.io/nuget/vpre/OpenMcdf)](https://www.nuget.org/packages/OpenMcdf)
[![NuGet Downloads](https://img.shields.io/nuget/dt/OpenMcdf)](https://www.nuget.org/packages/OpenMcdf)

# OpenMcdf

OpenMcdf is a fully .NET / C# library to manipulate [Compound File Binary File Format](https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-cfb/53989ce4-7b05-4f8d-829b-d08d6148375b) files, also known as [Structured Storage](https://learn.microsoft.com/en-us/windows/win32/stg/structured-storage-start-page). 

Compound files include multiple streams of information (document summary, user data) in a single container, and is used as the bases for many different file formats:
- Microsoft Office (.doc, .xls, .ppt)
- Windows thumbnails cache files (thumbs.db) 
- Outlook messages (.msg)
- Visual Studio Solution Options (.suo) 
- Advanced Authoring Format (.aaf)

OpenMcdf v3 has a rewritten API and supports:
- And idiomatic dotnet API and exception hierarchy
- Fast and efficient enumeration and manipulation of storages and streams
- Files sizes up to 16 TB (using major format version 4 with 4096 byte sectors)
- Transactions (i.e. commit and/or revert)
- Consolidation (i.e. reclamation of space by removing free sectors)
- Nullable attributes

Limitations:
- No support for bed-black tree balancing (directory entries are stored in a tree, but are not balanced. i.e. trees are "all-black")
- No support for single writer, multiple readers

## Getting started

To create a new compound file:

```C#
byte[] b = new byte[10000];

using var root = RootStorage.Create("test.cfb");
using CfbStream stream = root.CreateStream("MyStream");
stream.Write(b, 0, b.Length);
```

To open an Excel workbook (.xls) and access its main data stream:

```C#
using var root = RootStorage.OpenRead("report.xls");
using CfbStream workbookStream = root.OpenStream("Workbook");
```

To create or delete storages and streams:

```C#
using var root = RootStorage.Create("test.cfb");
root.CreateStorage("MyStorage");
root.CreateStream("MyStream");
root.Delete("MyStream");
```

For transacted storages, changes can either be committed or reverted:

```C#
using var root = RootStorage.Create("test.cfb", StorageModeFlags.Transacted);
root.Commit();
//
root.Revert();
```

A root storage can be consolidated to reduce its on-disk size:

```C#
root.Flush(consolidate: true);
```

## Object Linking and Embedding (OLE) Property Set Data Structures

Support for reading and writing [OLE Properties](https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-oleps/bf7aeae8-c47a-4939-9f45-700158dac3bc) 
is available via the OpenMcdf.Ole package. However, ***the API is experimental and subject to changes)***

```C#
OlePropertiesContainer co = new(stream);
foreach (OleProperty prop in co.Properties)
{
  ...
}
```

OpenMcdf runs happily on the [Mono](http://www.mono-project.com/) platform and multi-targets [**netstandard2.0**](https://learn.microsoft.com/en-us/dotnet/standard/net-standard?tabs=net-standard-2-0) and **net8.0** to maximize client compatibility and support modern dotnet features.
