Cobra WinLDTP is based on Linux Desktop Testing Project - http://ldtp.freedesktop.org
LDTP works on Windows/Linux/Mac/Solairs/FreeBSD/NetBSD/Palm Source, yes its Cross Platform GUI testing tool

Verified with Windows XP SP3 / Windows 7 SP1 / Windows 8

Please share your feedback with us (nagappan@gmail.com).

Minimum requirement .NET3.5 for Windows Xp/7

Test scripts can be written in Python / Ruby / Java / C# / VB.NET / PowerShell / Clojure / Perl and it can be extended to other languages.

On Windows XP SP3 make sure you have installed:
.NET3.0 and .NET3.5 and KB971513
http://www.microsoft.com/download/en/details.aspx?displaylang=en&id=13821 (KB971513)

.NET restributable package download info - http://www.pagestart.com/netframeworkdwnldlinks.html

On Windows 7: Default .NET with the system should work fine

Supported languages:

Python >= 2.5
Java >= 1.5
C# >= 3.5
VB.NET
Power Shell
Ruby >= 1.8.x
Perl
Clojure

Compile SetEnvironmentVariable and CobraWinLdtp solutions, place the binary where you have all the dll's, README.txt, License.rtf, before running Wix installer commands

LDTP packages are created with WiX installer - http://wix.tramontana.co.hu

To create CobraWinLDTP package (Credit: David Connet <dconnet@vmware.com>):
If planing to build package, copy WinLdtpdService.exe to the folder where rest of DLL's exist
"c:\Program Files (x86)\Windows Installer XML v3.5\bin\candle.exe" -pedantic CobraWinLDTP.wxs
"c:\Program Files (x86)\Windows Installer XML v3.5\bin\light.exe" -pedantic -spdb -sadv -dcl:high -ext WixUIExtension -ext WixUtilExtension -dWixUILicenseRtf=License.rtf -out CobraWinLDTP.msi CobraWinLDTP.wixobj

By default LDTP listens in localhost, to listen in all ports, set environment variable
LDTP_LISTEN_ALL_INTERFACE and then run WinLdtpdService.exe as an user with
administrator privillage in Windows 7, else you will get exception Access Denied.
Other option is: Disable ACL in Control Panel->User Accounts->Change User Account Control settings
Other option (Still you need to set LDTP_LISTEN_ALL_INTERFACE), you need to run as administrator:
set LDTP_LISTEN_ALL_INTERFACE=1 # To listen on all interface
Required for Windows >= 7
netsh http add urlacl url=http://localhost:4118/ user=User
netsh http add urlacl url=http://+:4118/ user=User
netsh http add urlacl url=http://*:4118/ user=User

CobraWinLDTP source files are distributed under MIT X11 license
Following files are re-distributed as-is
Microsoft DLL's (Interop.UIAutomationClient.dll, UIAComWrapper.dll, WUIATestLibrary.dll) - http://uiautomationverify.codeplex.com/ - MS-PL license
XML RPC .NET library (CookComputing.XmlRpcV2.dll) - http://www.xml-rpc.net/ - MIT X11 license

CobraWinLDTP works based on Microsoft accessibility layer.
To check whether your application is accessibility enabled,
download the binary from http://uiautomationverify.codeplex.com/ and verify the same.

Verified with Windows 8 developer edition

Minimum requirement .NET4.0
To compile for Windows 8 environment (You can compile it from Windows 7, Visual studio 2010), but make sure you change target framework as .NET 4.0 for Windows 8 and .NET 3.5 for Windows XP/7
NOTE: Don't select client profile

For Windows 7 the following steps are optional, but its required for Windows 8
netsh http add urlacl url=http://localhost:4118/ user=User
netsh http add urlacl url=http://+:4118/ user=User

If you run CobraWinLDTP where you have logged in as a domain user
netsh http add urlacl url=http://localhost:4118/ user=DOMAIN\User
netsh http add urlacl url=http://+:4118/ user=DOMAIN\User

For Java compilation:

Download commons-codec-1.6.jar, ws-commons-util-1.0.2.jar, xmlrpc-client-3.1.3.jar, xmlrpc-common-3.1.3.jar and place it in JavaLDTP/lib/

Download jar files from this location or any other apache mirror. Make sure you have the version mentioned in the jar or latest
http://mirror.cc.columbia.edu/pub/software/apache/commons/codec/binaries/commons-codec-1.6-bin.zip
http://www.apache.org/dyn/closer.cgi/ws/xmlrpc/

In eclipse its compiled by default. FIXME: Write how to compile from command line

To create Ldtp.jar

cd ldtp\JavaLDTP\bin
jar cvf ..\..\Ldtp.jar * # Note: Tested this on Mac with a forward slash though, haven't created Jar on Windows

To use LDTP Java library:

Include Ldtp.jar file available under ldtp folder in your project

How do I contact LDTP team incase of any help ?

  - Join the LDTP team on IRC for technical help, online
    Server  : irc.freenode.net
    Channel : #ldtp
  - Join the LDTP Mailing List - http://ldtp.freedesktop.org/wiki/Mailing_20list
