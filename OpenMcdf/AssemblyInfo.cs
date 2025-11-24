using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// In SDK-style projects such as this one, several assembly attributes that were historically
// defined in this file are now automatically added during build and populated with
// values defined in project properties. For details of which attributes are included
// and how to customize this process see: https://aka.ms/assembly-info-properties

// Setting ComVisible to false makes the types in this assembly not visible to COM
// components.  If you need to access a type in this assembly from COM, set the ComVisible
// attribute to true on that type.

[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM.

[assembly: Guid("a96ebb34-8c16-4c7e-b9f7-651ba754b722")]

#if NETSTANDARD2_0
[assembly: InternalsVisibleTo("OpenMcdf.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010085b50cbc1e40df696f8c30eaafc59a01e22303cb038fc832289b2c393f908a65c9aaa0d28026a47c6e5f85cc236f0735bea17236dbaaf91fea0003ddc1bb9c4cd318c5b855e7ef5877df5a7fc8394ee747d3573b69622e045837d546befb2fc13257e984db53a73dd59254a9a1d3c99a8ca6876c91304ea96899ac06a88d7bc6")]
#else
[assembly: InternalsVisibleTo("OpenMcdf.Tests")]
#endif
