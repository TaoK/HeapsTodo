"%1..\..\..\BuildTools\ILRepack\ILRepack.exe" /lib:%1 /t:exe /out:%1HeapsTodoSyncTool.exe %1HeapsTodoSyncToolExeAssembly.exe %1DotNetOpenAuth.dll %1Google.Apis.Authentication.OAuth2.dll %1Google.Apis.dll %1Google.Apis.Samples.Helper.dll %1Google.Apis.Tasks.v1.dll %1HeapsTodoLib.dll %1HeapsTodoSyncLib.dll %1log4net.dll %1NDesk.Options.dll %1Newtonsoft.Json.Net35.dll
IF %ERRORLEVEL% NEQ 0 GOTO END

del %1HeapsTodoSyncToolExeAssembly.exe
del %1DotNetOpenAuth.dll
del %1Google.Apis.Authentication.OAuth2.dll
del %1Google.Apis.dll
del %1Google.Apis.Samples.Helper.dll
del %1Google.Apis.Tasks.v1.dll
del %1HeapsTodoLib.dll
del %1HeapsTodoSyncLib.dll
del %log4net.dll
del %1NDesk.Options.dll
del %1Newtonsoft.Json.Net35.dll
:END