using System;
namespace OLECompoundFileStorage
{
    interface ISector
    {
        byte[] Data { get; set; }
        int Id { get; set; }
        bool IsDIFATSector { get; set; }
        bool IsFATSector { get; set; }
        int Next { get; set; }
        int Size { get; }
    }
}
