﻿using System.Diagnostics.CodeAnalysis;

namespace OpenMcdf;

/// <summary>
/// Encapsulates an entry in the File Allocation Table (FAT).
/// </summary>
internal record struct FatEntry(uint Index, uint Value)
{
    public readonly bool IsFree => Value == SectorType.Free;

    [ExcludeFromCodeCoverage]
    public override readonly string ToString() => $"#{Index}: {Value}";
}
