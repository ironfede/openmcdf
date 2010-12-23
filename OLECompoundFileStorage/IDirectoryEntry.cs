using System;
using BinaryTrees;

/*
     The contents of this file are subject to the Mozilla Public License
     Version 1.1 (the "License"); you may not use this file except in
     compliance with the License. You may obtain a copy of the License at
     http://www.mozilla.org/MPL/

     Software distributed under the License is distributed on an "AS IS"
     basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
     License for the specific language governing rights and limitations
     under the License.

     The Original Code is OpenMCDF - Compound Document Format library.

     The Initial Developer of the Original Code is Federico Blaseotto.
*/

namespace OleCompoundFileStorage
{
    internal interface IDirectoryEntry : IComparable
    {
        int Child { get; set; }
        byte[] CreationDate { get; set; }
        byte[] EntryName { get; }
        string GetEntryName();
        int LeftSibling { get; set; }
        byte[] ModifyDate { get; set; }
        string Name { get; }
        ushort NameLength { get; set; }
        void Read(System.IO.BinaryReader br);
        int RightSibling { get; set; }
        void SetEntryName(string entryName);
        int SID { get; set; }
        long Size { get; set; }
        int StartSetc { get; set; }
        int StateBits { get; set; }
        StgColor StgColor { get; set; }
        StgType StgType { get; set; }
        Guid StorageCLSID { get; set; }
        byte[] ToByteArray();
        void Write(System.IO.BinaryWriter bw);
    }
}
