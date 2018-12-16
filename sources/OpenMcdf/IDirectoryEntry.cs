/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. 
 * 
 * The Original Code is OpenMCDF - Compound Document Format library.
 * 
 * The Initial Developer of the Original Code is Federico Blaseotto.*/


using System;
using System.IO;
using RedBlackTree;

namespace OpenMcdf
{
    internal interface IDirectoryEntry : IComparable, IRBNode
    {
        int Child { get; set; }
        byte[] CreationDate { get; set; }
        byte[] EntryName { get; }
        int LeftSibling { get; set; }
        byte[] ModifyDate { get; set; }
        string Name { get; }
        ushort NameLength { get; set; }
        int RightSibling { get; set; }
        int SID { get; set; }
        long Size { get; set; }
        int StartSetc { get; set; }
        int StateBits { get; set; }
        StgColor StgColor { get; set; }
        StgType StgType { get; set; }
        Guid StorageCLSID { get; set; }
        string GetEntryName();
        void Read(Stream stream, CFSVersion ver = CFSVersion.Ver_3);
        void SetEntryName(string entryName);
        void Write(Stream stream);
    }
}