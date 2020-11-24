# 
# This starts the application with `dotnet watch run` 
# and disables .NET 5.0's native browser refresh to avoid overlapping behavior
# (which doesn't seem to work anyway)
#

# Turn off .NET 5 auto refresh (which doesn't seem to work anyway)
$env:DOTNET_WATCH_SUPPRESS_BROWSER_REFRESH=1

dotnet watch run





# 
# if you want to test the 'native' behavior of .NET 5.0's browser refresh,
# go into `startup.cs` and comment `app.UseLiveReload()` and
# change 
#
# $env:DOTNET_WATCH_SUPPRESS_BROWSER_REFRESH=0
#
# in the script above.
#
# Note: for me this doesn't do anything, but according to the docs .NET docs this 
#       is supposed to work.
#