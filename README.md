[![Build Status](https://fb8.visualstudio.com/Openmcdf/_apis/build/status/Openmcdf-CI?branchName=master)](https://fb8.visualstudio.com/Openmcdf/_build/latest?definitionId=1&branchName=master)
![GitHub Actions](https://github.com/ironfede/openmcdf/actions/workflows/dotnet.yml/badge.svg)

# OpenMcdf

OpenMcdf is a 100% .NET / C# component that allows developers to manipulate [Compound File Binary File Format](https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-cfb/53989ce4-7b05-4f8d-829b-d08d6148375b) files (also known as OLE structured storage). 

Compound file includes multiple streams of information (document summary, user data) in a single container. 

This file format is used under the hood by a lot of applications: all the documents created by Microsoft Office until the 2007 product release are structured storage files. Windows thumbnails cache files (thumbs.db) are compound documents as well as .msg Outlook messages. Visual Studio .suo files (solution options) are compound files and a lot of audio/video editing tools save project file in a compound container (*.aaf files for example).

OpenMcdf supports read/write operations on streams and storages and traversal of structures tree. It supports version 3 and 4 of the specifications, uses lazy loading wherever possible to reduce memory usage and offer an intuitive API to work with structured files.

It's very easy to **create a new compound file**

```C#
byte[] b = new byte[10000];

using var root = RootStorage.Create("test.cfb");
using CfbStream stream = root.CreateStream("MyStream");
stream.Write(b, 0, b.Length);
```

You can **open an existing one**, an Excel workbook (.xls) and use its main data stream

```C#
using var root = RootStorage.OpenRead("report.xls");
using CfbStream workbookStream = root.OpenStream("Workbook");
```

Adding **storage and stream items** is just as easy...

```C#
using var root = RootStorage.Create("test.cfb");
root.AddStorage("MyStorage");
root.AddStream("MyStream");
```
...as removing them

```C#
root.Delete("MyStream");
```

For transacted storages, changes can either be committed or reverted:

```C#
using var root = RootStorage.Create("test.cfb", StorageModeFlags.Transacted);
root.Commit();
//
root.Revert();
```

If you need to compress a compound file, you can purge its unused space:

```C#
root.Flush(consolidate: true);
```

[OLE Properties](https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-oleps/bf7aeae8-c47a-4939-9f45-700158dac3bc) handling for DocumentSummaryInfo`` and SummaryInfo streams  
is available via extension methods ***(experimental - API subjected to changes)***

```C#
PropertySetStream mgr = ((CFStream)target).AsOLEProperties();
for (int i = 0; i < mgr.PropertySet0.NumProperties; i++)
{
  ITypedPropertyValue p = mgr.PropertySet0.Properties[i];
  ...
```

OpenMcdf runs happily on the [Mono](http://www.mono-project.com/) platform and multi-targets **netstandard2.0** and **net8.0** to allow maximum client compatibility.
