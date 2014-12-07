msbuild OpenMcdf.sln /property:Configuration=Release
robocopy ./src NEW_RELEASE/src /MIR /xd bin obj
robocopy ./src/bin/Release NEW_RELEASE *.*
robocopy "./Structured Storage Explorer\bin\Release" NEW_RELEASE\StructuredStorageExplorer\ StucturedStorageExplorer.exe Be.Windows.Forms.HexBox.dll OpenMcdf.dll 
robocopy ./ NEW_RELEASE "PRE_RELEASE_note.txt" "Release notes.txt" "Support.txt" "License.txt"