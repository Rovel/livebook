!include "MUI2.nsh"

!define App "Demo"
!define ProgId "Demo.Demo"
!define AppDir "bin\Release\net6.0-windows"

Name "${App}"
OutFile "${App}Install.exe"
InstallDir "$LOCALAPPDATA\${App}"
Unicode True
ManifestDPIAware true
; Need admin for registering URL scheme
; RequestExecutionLevel admin

!define MUI_ABORTWARNING
!define MUI_ICON "erl_icon.ico"

!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "English"

Section "Install"
  SetOutPath "$INSTDIR"

  ; File vcredist_x64.exe
  ; ExecWait '"$INSTDIR\vcredist_x64.exe" /install /quiet /norestart'

  File /a /r "${AppDir}\*"
  Delete "$INSTDIR\Installer.nsi"

  CreateDirectory "$INSTDIR\Logs"
  WriteUninstaller "$INSTDIR\${App}Uninstall.exe"
SectionEnd

!define FileExt "demo"
Section "-RegisterFile"
  DetailPrint "Register .${FileExt} File Handler"
  WriteRegStr HKCR ".${FileExt}" "" "${ProgId}"
  WriteRegStr HKCR "${ProgId}" "" "${App}"
  WriteRegStr HKCR "${ProgId}\DefaultIcon" "" "$INSTDIR\${App}.exe,0"
  WriteRegStr HKCR "${ProgId}\shell\open\command" "" '"$INSTDIR\${App}.exe" "%1"'
SectionEnd

Section "-un.RegisterFile"
  DeleteRegKey HKCR ".${FileExt}"
  DeleteRegKey HKCR "${ProgId}"
  DeleteRegKey HKCR "${ProgId}\DefaultIcon"
  DeleteRegKey HKCR "${ProgId}\shell\open\command"
SectionEnd

!define URLScheme "demo"
Section "-RegisterURL"
  DetailPrint "Register ${URLScheme}:// URL Handler"
  WriteRegStr HKCR "${URLScheme}" "" "${URLScheme} Protocol"
  WriteRegStr HKCR "${URLScheme}" "URL Protocol" ""
  WriteRegStr HKCR "${URLScheme}\DefaultIcon" "" "$INSTDIR\${App}.exe,0"
  WriteRegStr HKCR "${URLScheme}\shell\open\command" "" '"$INSTDIR\${App}.exe" "%1"'
SectionEnd

Section "-un.RegisterURL"
SectionEnd

Section "Desktop Shortcut"
  CreateShortCut "$DESKTOP\${App}.lnk" "$INSTDIR\${App}.exe" ""
SectionEnd

Section "Uninstall"
  Delete "$DESKTOP\${App}.lnk"
  ExecWait "taskkill.exe /F /IM epmd.exe"
  RMDir /r "$INSTDIR"
SectionEnd
