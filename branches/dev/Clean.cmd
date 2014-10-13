@ECHO OFF
CHOICE /CS /C Yn /M  "WARNING: THIS SCRIPT MUST RUN INSIDE OpenMcdf directory. FILES WILL BE DELETED WITHOUT PROMPT. Continue ? (Y/n)"

if ERRORLEVEL 2 GOTO SCRIPTEND

echo "Working..."

rd /s /Q "Memory Test\bin\Debug"
rd /s /Q "Memory Test\bin\Release"
rd /s /Q "Memory Test\obj\Debug"
rd /s /Q "Memory Test\obj\Release"
rd /s /Q "Memory Test\bin"
rd /s /Q "Memory Test\obj"

rd /s /Q  "Unit Test\bin\Debug"
rd /s /Q  "Unit Test\bin\Release"
rd /s /Q  "Unit Test\obj\Debug"
rd /s /Q  "Unit Test\obj\Release"
rd /s /Q  "Unit Test\bin"
rd /s /Q  "Unit Test\obj"


rd /s /Q  Src\bin\Debug
rd /s /Q  Src\bin\Release
rd /s /Q  Src\obj\Debug
rd /s /Q  Src\obj\Release
rd /s /Q  Src\bin
rd /s /Q  Src\obj

rd /s /Q  "OpenMcdf.Extensions\bin\Debug"
rd /s /Q  "OpenMcdf.Extensions\bin\Release
rd /s /Q  "OpenMcdf.Extensions\obj\Debug"
rd /s /Q  "OpenMcdf.Extensions\obj\Release"
rd /s /Q  "OpenMcdf.Extensions\bin"
rd /s /Q  "OpenMcdf.Extensions\obj"

rd /s /Q  "OpenMcdfExtensionsTest\bin\Debug"
rd /s /Q  "OpenMcdfExtensionsTest\bin\Release
rd /s /Q  "OpenMcdfExtensionsTest\obj\Debug"
rd /s /Q  "OpenMcdfExtensionsTest\obj\Release"
rd /s /Q  "OpenMcdfExtensionsTest\obj"
rd /s /Q  "OpenMcdfExtensionsTest\bin"


rd /s /Q  "Structured Storage Explorer\bin\Debug"
rd /s /Q  "Structured Storage Explorer\bin\Release
rd /s /Q  "Structured Storage Explorer\obj\Debug"
rd /s /Q  "Structured Storage Explorer\obj\Release"
rd /s /Q  "Structured Storage Explorer\bin"
rd /s /Q  "Structured Storage Explorer\obj"

rd /s /Q  "Performance Test\bin\Debug"
rd /s /Q  "Performance Test\bin\Release"
rd /s /Q  "Performance Test\obj\Debug"
rd /s /Q  "Performance Test\obj\Release"
rd /s /Q  "Performance Test\bin"
rd /s /Q  "Performance Test\obj"

rd /s /Q  TestResults

:SCRIPTEND
