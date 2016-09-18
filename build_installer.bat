@ECHO OFF

REM Configure tool locations
SET MSBUILD=C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe
SET WIX=C:\Program Files (x86)\WiX Toolset v3.10\bin

REM ===========================================================

"%MSBUILD%" -t:Build -p:Configuration=Release || GOTO END

REM FIXME: JUST SINCE I DON'T NEED IT ATM
COPY NUL CobraWinLDTP\ldtp\Ldtp.jar || GOTO END

"%WIX%\candle.exe" -pedantic CobraWinLDTP\CobraWinLDTP.wxs -out CobraWinLDTP\CobraWinLDTP.wixobj || GOTO END
"%WIX%\light.exe" -pedantic -spdb -dcl:high -ext WixUIExtension -ext WixUtilExtension -dWixUILicenseRtf=License.rtf -out CobraWinLDTP.msi CobraWinLDTP\CobraWinLDTP.wixobj || GOTO END
EXIT /B 0

:END
echo "Command errored, aborting..."
EXIT /B %ERRORLEVEL%
