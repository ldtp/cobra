Cobra WinLDTP is based on Linux Desktop Testing Project - http://ldtp.freedesktop.org 

LDTP works on Windows/Linux/Mac/Solairs/FreeBSD/NetBSD/Palm Source, yes its Cross Platform GUI testing tool. Please share your feedback with us (nagappan@gmail.com).

### Linux 

GUI testing is known to work on all major Linux distribution with: 

  * GNOME 
  * KDE (Qt >= 4.8)
  * Java Swing
  * LibreOffice
  * Mozilla application 

### Windows 

GUI testing is known to work on (minimum requirement >= .NET3.5)

  * Windows XP SP3
  * Vista SP2
  * Windows 7
  * Windows 8 
  * Windows 8.1
  * Windows 10

with application written in 

  * .NET
  * C++ (MFC, WPF)
  * Java Swing
  * Qt >= 4.8 

### Mac 

GUI testing is known to work on 

  * OS X Snow Leopard
  * OS X Lion
  * OS X Mountain Lion
  
Where ever ATOMac runs, LDTP should work on it

Test scripts can be written in Python / Ruby / Java / C# / VB.NET / PowerShell / Clojure / Perl and it can be extended to other languages. 

## Download

Download latest Cobra binary from http://code.google.com/p/cobra-winldtp/downloads/list

On Windows XP SP3 make sure you have installed:
.NET3.0 and .NET3.5 and KB971513
http://www.microsoft.com/download/en/details.aspx?displaylang=en&id=13821 (KB971513)

.NET restributable package download info - http://www.pagestart.com/netframeworkdwnldlinks.html

On Windows 7: Default .NET with the system should work fine

## Supported languages to write test script

* Python >= 2.5
* Java >= 1.5
* C# >= 3.5
* VB.NET
* Power Shell
* Ruby >= 1.8.x
* Perl
* Clojure

## Build and Package Cobra

Compile SetEnvironmentVariable and CobraWinLdtp solutions, place the binary where you have all the dll's, README.txt, License.rtf, before running Wix installer commands. LDTP packages are created with WiX installer - http://wix.tramontana.co.hu

To create CobraWinLDTP package (Credit: David Connet @VMware):
If planing to build package, copy WinLdtpdService.exe to the folder where rest of DLL's exist
```
"c:\Program Files (x86)\Windows Installer XML v3.5\bin\candle.exe" -pedantic CobraWinLDTP.wxs
"c:\Program Files (x86)\Windows Installer XML v3.5\bin\light.exe" -pedantic -spdb -sadv -dcl:high -ext WixUIExtension -ext WixUtilExtension -dWixUILicenseRtf=License.rtf -out CobraWinLDTP.msi CobraWinLDTP.wixobj
```

## Using Cobra

By default LDTP listens in localhost, to listen in all ports, set environment variable LDTP_LISTEN_ALL_INTERFACE and then run WinLdtpdService.exe as an user with administrator privillage in Windows 7, else you will get exception Access Denied. Other option is: Disable ACL in Control Panel->User Accounts->Change User Account Control settings Other option (Still you need to set LDTP_LISTEN_ALL_INTERFACE), you need to run as administrator:
set LDTP_LISTEN_ALL_INTERFACE=1 # To listen on all interface
Required for Windows >= 7
```
netsh http add urlacl url=http://localhost:4118/ user=User
netsh http add urlacl url=http://+:4118/ user=User
netsh http add urlacl url=http://*:4118/ user=User
```

## Other Details

CobraWinLDTP source files are distributed under MIT X11 license. Following files are re-distributed as-is:
Microsoft DLL's (Interop.UIAutomationClient.dll, UIAComWrapper.dll, WUIATestLibrary.dll) - http://uiautomationverify.codeplex.com/ - MS-PL license
XML RPC .NET library (CookComputing.XmlRpcV2.dll) - http://www.xml-rpc.net/ - MIT X11 license

CobraWinLDTP works based on Microsoft accessibility layer. To check whether your application is accessibility enabled, download the binary from http://uiautomationverify.codeplex.com/ and verify the same.

Verified with Windows 8 developer edition. Minimum requirement .NET4.0
To compile for Windows 8 environment (You can compile it from Windows 7, Visual studio 2010), but make sure you change target framework as .NET 4.0 for Windows 8 and .NET 3.5 for Windows XP/7
NOTE: Don't select client profile

## Windows 8 

For Windows 7 the following steps are optional, but its required for Windows 8
```
netsh http add urlacl url=http://localhost:4118/ user=User
netsh http add urlacl url=http://+:4118/ user=User
```

## Windows Domain

If you run CobraWinLDTP where you have logged in as a domain user
```
netsh http add urlacl url=http://localhost:4118/ user=DOMAIN\User
netsh http add urlacl url=http://+:4118/ user=DOMAIN\User
```

## Java library compilation

1. Create directory CobraWinLDTP\ldtp\Java\lib\ 
2. Download and place in this directory next files:

  * [commons-codec-1.6.jar](http://central.maven.org/maven2/commons-codec/commons-codec/1.6/commons-codec-1.6.jar)
  * [ws-commons-util-1.0.2.jar](http://central.maven.org/maven2/org/apache/ws/commons/util/ws-commons-util/1.0.2/ws-commons-util-1.0.2.jar)
  * [xmlrpc-client-3.1.3.jar](http://central.maven.org/maven2/org/apache/xmlrpc/xmlrpc-client/3.1.3/xmlrpc-client-3.1.3.jar)
  * [xmlrpc-common-3.1.3.jar](http://central.maven.org/maven2/org/apache/xmlrpc/xmlrpc-common/3.1.3/xmlrpc-common-3.1.3.jar)
  * [commons-logging-1.1.1.jar](http://central.maven.org/maven2/commons-logging/commons-logging/1.1.1/commons-logging-1.1.1.jar)
  * [commons-logging-adapters-1.1.1.jar](http://central.maven.org/maven2/commons-logging/commons-logging-adapters/1.1/commons-logging-adapters-1.1.jar)
  * [commons-logging-api-1.1.1.jar](http://central.maven.org/maven2/commons-logging/commons-logging-api/1.1/commons-logging-api-1.1.jar)

  Download jar files from this location or any other apache mirror. Make sure you have the version mentioned in the jar or latest.

3. For build Ldtp.jar:

```
set LIBS_PATH=CobraWinLDTP\ldtp\Java\lib
set SRC_PATH=CobraWinLDTP\ldtp\Java\src
javac -cp .;%LIBS_PATH%\*; %SRC_PATH%\com\cobra\ldtp\*.java
cd %SRC_PATH%
jar cvf ..\..\ldtp.jar com\cobra\ldtp\*.class
```

To use LDTP Java library just include Ldtp.jar file available under ldtp folder in your project.

## How do I contact LDTP team incase of any help ?

  - Join the LDTP team on IRC for technical help, online
    Server  : irc.freenode.net
    Channel : #ldtp or Web Chat - http://webchat.freenode.net/?randomnick=1&channels=ldtp&uio=d4
  - Join the LDTP Mailing List - http://ldtp.freedesktop.org/wiki/Mailing%20list%20/%20IRC

