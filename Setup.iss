#ifndef PublishSourcePath
  #define PublishSourcePath "publish"
#endif
#ifndef InstallerOutputPath
  #define InstallerOutputPath "artifacts\installer"
#endif
#ifndef MyAppVersion
  #define MyAppVersion GetVersionNumbersString(PublishSourcePath + "\Almoxarifado.exe")
#endif

#define MyAppName "Almoxarifado S.I.G."
#define MyAppPublisher "Cipolatti, Inc."
#define MyAppURL "https://www.cipolatti.com.br"
#define MyAppExeName "Almoxarifado.exe"
#ifndef DotNetRuntimeInstaller
  #define DotNetRuntimeInstaller "redist\windowsdesktop-runtime-10.0-win-x64.exe"
#endif
#ifndef SkipRuntimeBundle
  #ifexist DotNetRuntimeInstaller
    #define BundleRuntime
  #endif
#endif

[Setup]
AppId={{1E86DF56-2734-45E9-883F-9674030AE0B0}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL=https://atualizasig.cipolatti.com.br
VersionInfoVersion={#MyAppVersion}
DefaultDirName=C:\SIG\{#MyAppName}
DisableDirPage=yes
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
RestartApplications=no
PrivilegesRequired=lowest
OutputDir={#InstallerOutputPath}
OutputBaseFilename=AlmoxarifadoSetup-{#MyAppVersion}
SetupIconFile=Almoxarifado\icones\logo.ico
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PublishSourcePath}\*"; DestDir: "{app}"; Excludes: "Almoxarifado.dll.config,Almoxarifado.exe.config,App.config"; Flags: ignoreversion recursesubdirs createallsubdirs
#ifdef BundleRuntime
Source: "{#DotNetRuntimeInstaller}"; DestDir: "{tmp}"; DestName: "windowsdesktop-runtime-10.0-win-x64.exe"; Flags: dontcopy
#endif

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
function IsDotNetDesktopRuntime10Installed: Boolean;
var
  Versions: TArrayOfString;
  I: Integer;
begin
  Result := False;
  if RegGetValueNames(HKLM64, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App', Versions) then
    for I := 0 to GetArrayLength(Versions) - 1 do
      if Copy(Versions[I], 1, 5) = '10.0.' then
      begin
        Result := True;
        Exit;
      end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
begin
  Result := '';
  if IsDotNetDesktopRuntime10Installed then
    Exit;
#ifdef BundleRuntime
  ExtractTemporaryFile('windowsdesktop-runtime-10.0-win-x64.exe');
  if not ShellExec('runas', ExpandConstant('{tmp}\windowsdesktop-runtime-10.0-win-x64.exe'),
    '/install /quiet /norestart', '', SW_SHOWNORMAL, ewWaitUntilTerminated, ResultCode) then
  begin
    Result := 'Nao foi possivel iniciar a instalacao do .NET Desktop Runtime 10 x64. Autorize a elevacao ou solicite apoio da TI.';
    Exit;
  end;
  if (ResultCode <> 0) and (ResultCode <> 3010) then
  begin
    Result := 'A instalacao do .NET Desktop Runtime 10 x64 falhou. Codigo: ' + IntToStr(ResultCode);
    Exit;
  end;
  if ResultCode = 3010 then
  begin
    NeedsRestart := True;
    Result := 'Reinicie o Windows para concluir a instalacao do .NET e execute este instalador novamente.';
    Exit;
  end;
  if not IsDotNetDesktopRuntime10Installed then
    Result := 'O .NET Desktop Runtime 10 x64 nao foi detectado apos a instalacao. Solicite apoio da TI.';
#else
  Result := 'Este instalador requer o .NET Desktop Runtime 10 x64. Instale o runtime e execute o instalador novamente.';
#endif
end;
