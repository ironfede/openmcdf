using System;
namespace OpenMcdf
{
    public interface ICFItem
    {
        Guid CLSID { get; set; }
        int CompareTo(object obj);
        DateTime CreationDate { get; set; }
        bool Equals(object obj);
        int GetHashCode();
        bool IsRoot { get; }
        bool IsStorage { get; }
        bool IsStream { get; }
        DateTime ModifyDate { get; set; }
        string Name { get; }
        long Size { get; }
    
    }

   
}
