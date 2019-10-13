
# Make sure you have this in your project:
#
# <PublishSingleFile>true</PublishSingleFile>
# <PublishTrimmed>true</PublishTrimmed>
# <RuntimeIdentifier>win-x64</RuntimeIdentifier>
#
# and you disable the Package Build
#
# <PackAsTool>false</PackAsTool>
#
# Note:
# For Razor Compilation `PublishTrimmed` does not work because
# it relies on dynamic interfaces when compiling Razor at runtime.
# If you compile with Razor disabled, or don't plan on using Razor
# with this LiveReloadServer, you can set /p:PublishTrimmed=true
# to cut the size of the exe in half.

remove-item ./LiveReloadWebServer.exe
remove-item ./SingleFileExe/*.*

dotnet publish -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true -r win-x64 --output SingleFileExe

Move-Item ./SingleFileExe/LiveReloadServer.exe ./LiveReloadWebServer.exe -force
remove-item ./SingleFileExe/*.*