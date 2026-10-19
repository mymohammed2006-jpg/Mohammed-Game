; Mohammed Game installer (Godot version).
; It replaces the old PC app (Electron, made by electron-builder) in the same place, so:
; - the old app's auto-update downloads this file and runs it with "--updated /S --force-run", and the new game opens by itself;
; - same folder (%LOCALAPPDATA%\Programs\mohammed-game), same "Mohammed Game.exe", same shortcuts and uninstall entry;
; - the saves (%APPDATA%\Mohammed Game) are never touched; the game moves them over on its first start.
; Build: makensis -DVERSION=40.0.0 -DSRC=<exported game folder> -DOUT=<setup.exe> installer.nsi

Unicode true
ManifestDPIAware true
RequestExecutionLevel user
SetCompressor /SOLID lzma
SetCompressorDictSize 64

!include LogicLib.nsh
!include FileFunc.nsh

!ifndef VERSION
  !error "VERSION is missing"
!endif
!ifndef SRC
  !error "SRC is missing"
!endif
!ifndef OUT
  !define OUT "Mohammed-Game-Setup.exe"
!endif

!define APP "Mohammed Game"
!define EXE "Mohammed Game.exe"
!define UNINST "Uninstall Mohammed Game.exe"
; the same keys electron-builder used (appId com.mohammed.game), so Windows sees one app
!define GUID "e943cfda-4f29-5c57-ab80-a84886b03838"
!define INSTKEY "Software\${GUID}"
!define UNKEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\${GUID}"

Name "${APP}"
Caption "${APP}"
OutFile "${OUT}"
Icon "../assets/icon.ico"
UninstallIcon "../assets/icon.ico"
InstallDir "$LOCALAPPDATA\Programs\mohammed-game"
BrandingText " "
ShowInstDetails nevershow
ShowUninstDetails nevershow
AutoCloseWindow true
VIProductVersion "${VERSION}.0"
VIAddVersionKey "ProductName" "${APP}"
VIAddVersionKey "FileDescription" "${APP}"
VIAddVersionKey "CompanyName" "Mohammed"
VIAddVersionKey "FileVersion" "${VERSION}"
VIAddVersionKey "ProductVersion" "${VERSION}"
VIAddVersionKey "LegalCopyright" " "

Page instfiles
UninstPage instfiles

Var updated
Var forceRun

; waits until the running game (old or new) has closed and its files can be replaced;
; after 10 s it closes it (the old app sometimes leaves helper processes behind)
!macro WaitForGame
  StrCpy $0 0
  ${Do}
    ClearErrors
    ${IfNot} ${FileExists} "$INSTDIR\${EXE}"
      ${Break}
    ${EndIf}
    FileOpen $1 "$INSTDIR\${EXE}" a
    ${IfNot} ${Errors}
      FileClose $1
      ${Break}
    ${EndIf}
    IntOp $0 $0 + 1
    ${If} $0 = 50
      nsExec::Exec 'taskkill /F /T /IM "${EXE}"'
      Pop $1
    ${EndIf}
    ${If} $0 > 100
      ${Break}
    ${EndIf}
    Sleep 200
  ${Loop}
!macroend

Function .onInit
  SetShellVarContext current
  ${GetParameters} $R0
  ClearErrors
  ${GetOptions} $R0 "--updated" $R1
  ${IfNot} ${Errors}
    StrCpy $updated 1
  ${EndIf}
  ClearErrors
  ${GetOptions} $R0 "--force-run" $R1
  ${IfNot} ${Errors}
    StrCpy $forceRun 1
  ${EndIf}
FunctionEnd

Section
  SetShellVarContext current
  SetDetailsPrint textonly
  DetailPrint "Installing ${APP}..."
  SetDetailsPrint none
  !insertmacro WaitForGame

  ; remove the previous version's program files (old Electron app or an older Godot version).
  ; Only a folder that holds our uninstaller is emptied, never anything else.
  ${If} ${FileExists} "$INSTDIR\${UNINST}"
    RMDir /r "$INSTDIR"
  ${EndIf}
  ; the old app's update cache (its own copy of the installer); the running file stays if it is locked
  Delete "$LOCALAPPDATA\mohammed-game-updater\installer.exe"
  Delete "$LOCALAPPDATA\mohammed-game-updater\current.blockmap"

  SetOutPath "$INSTDIR"
  File /r "${SRC}\*.*"
  WriteUninstaller "$INSTDIR\${UNINST}"

  ; shortcuts: a fresh install makes both; an update leaves the player's shortcuts as they are
  ; (they already point at the same "Mohammed Game.exe"), like the old installer did
  ${If} $updated != 1
    CreateShortCut "$SMPROGRAMS\${APP}.lnk" "$INSTDIR\${EXE}" "" "$INSTDIR\${EXE}" 0
    CreateShortCut "$DESKTOP\${APP}.lnk" "$INSTDIR\${EXE}" "" "$INSTDIR\${EXE}" 0
  ${EndIf}
  System::Call 'Shell32::SHChangeNotify(i 0x8000000, i 0, i 0, i 0)'

  WriteRegStr HKCU "${INSTKEY}" InstallLocation "$INSTDIR"
  WriteRegStr HKCU "${INSTKEY}" KeepShortcuts "true"
  WriteRegStr HKCU "${INSTKEY}" ShortcutName "${APP}"
  WriteRegStr HKCU "${UNKEY}" DisplayName "${APP} ${VERSION}"
  WriteRegStr HKCU "${UNKEY}" UninstallString '"$INSTDIR\${UNINST}" /currentuser'
  WriteRegStr HKCU "${UNKEY}" QuietUninstallString '"$INSTDIR\${UNINST}" /currentuser /S'
  WriteRegStr HKCU "${UNKEY}" DisplayVersion "${VERSION}"
  WriteRegStr HKCU "${UNKEY}" DisplayIcon "$INSTDIR\${EXE},0"
  WriteRegStr HKCU "${UNKEY}" Publisher "Mohammed"
  WriteRegDWORD HKCU "${UNKEY}" NoModify 1
  WriteRegDWORD HKCU "${UNKEY}" NoRepair 1
  ${GetSize} "$INSTDIR" "/S=0K" $0 $1 $2
  IntFmt $0 "0x%08X" $0
  WriteRegDWORD HKCU "${UNKEY}" EstimatedSize "$0"

  ; open the game: always after a normal install, and after an update when asked (--force-run)
  ${If} $updated != 1
  ${OrIf} $forceRun == 1
    SetOutPath "$INSTDIR"
    Exec '"$INSTDIR\${EXE}"'
  ${EndIf}
SectionEnd

Function un.onInit
  SetShellVarContext current
FunctionEnd

Section "Uninstall"
  SetShellVarContext current
  !insertmacro WaitForGame
  Delete "$SMPROGRAMS\${APP}.lnk"
  Delete "$DESKTOP\${APP}.lnk"
  System::Call 'Shell32::SHChangeNotify(i 0x8000000, i 0, i 0, i 0)'
  DeleteRegKey HKCU "${UNKEY}"
  DeleteRegKey HKCU "${INSTKEY}"
  ; the saves in %APPDATA%\Mohammed Game stay, like before
  RMDir /r "$INSTDIR"
SectionEnd
