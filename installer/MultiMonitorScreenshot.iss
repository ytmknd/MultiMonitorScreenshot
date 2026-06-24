; Inno Setup script for マルチモニター スクリーンショット
; 発行フォルダ (self-contained / single-file publish + 必要なネイティブ DLL) を
; そのままインストールします。
;
; ビルド方法:
;   1) 先に Visual Studio で「発行 (FolderProfile)」を実行し、
;      MultiMonitorScreenshot\bin\Release\net8.0-windows\publish\win-x64\ を生成する
;   2) "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\MultiMonitorScreenshot.iss
;   3) installer\Output\ に Setup 実行ファイルが生成される

#define MyAppName "マルチモニター スクリーンショット"
#define MyAppVersion "1.0.1"
#define MyAppPublisher "MultiMonitorScreenshot"
#define MyAppExeName "MultiMonitorScreenshot.exe"
; .iss からの相対パスで発行フォルダを指定
#define MyPublishDir "..\MultiMonitorScreenshot\bin\Release\net8.0-windows\publish\win-x64"

[Setup]
; AppId は一度発行したら変更しないこと（アップグレード判定に使用）
AppId={{8F3B6C1E-7A4D-4E2B-9C5A-2D1F0A6B7C8E}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\MultiMonitorScreenshot
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
; 64bit アプリ (win-x64) としてインストール
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; 既定では管理者権限でインストール (Program Files)
PrivilegesRequired=admin
OutputDir=Output
OutputBaseFilename=MultiMonitorScreenshot-{#MyAppVersion}-Setup
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; 発行フォルダの全ファイル (exe + 必要なネイティブ DLL) を含める。pdb は除外。
Source: "{#MyPublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb"

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent
