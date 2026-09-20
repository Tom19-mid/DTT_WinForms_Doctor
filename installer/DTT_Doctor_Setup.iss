; ============================================================================
; DTT Healthcare - Doctor Desktop : Inno Setup script
; Đóng gói bản publish (self-contained, win-x64) thành 1 file cài đặt Setup.exe
;
; Build:
;   1) dotnet publish DTT.Doctor.UI\DTT.Doctor.UI.csproj -c Release -r win-x64 --self-contained true -o publish\app
;   2) ISCC.exe installer\DTT_Doctor_Setup.iss      -> installer\output\DTT_Doctor_Setup_1.0.0.exe
;
; Cài theo kiểu per-user (không cần quyền Administrator) vào %LocalAppData%\Programs,
; vì app ghi file cấu hình (saved_credentials.dat, recent_logins.json) ngay cạnh file .exe.
; ============================================================================

#define MyAppName "DTT Healthcare - Doctor Desktop"
#define MyAppShortName "DTT Doctor"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "DTT Healthcare"
#define MyAppExeName "DTT.Doctor.UI.exe"

[Setup]
AppId={{B8D033BB-EC23-40C4-9AF1-2A7CC01913BA}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\DTT Healthcare Doctor
DefaultGroupName={#MyAppName}
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
SetupIconFile=..\DTT.Doctor.UI\DTT.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
OutputDir=output
OutputBaseFilename=DTT_Doctor_Setup_{#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
DisableProgramGroupPage=yes
DisableDirPage=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Tạo biểu tượng ngoài màn hình Desktop"; GroupDescription: "Biểu tượng bổ sung:"

[Files]
Source: "..\publish\app\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\{#MyAppShortName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Mở {#MyAppName}"; WorkingDir: "{app}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; File do app tự sinh ra lúc chạy (thông tin đăng nhập đã lưu) — xóa sạch khi gỡ cài đặt
Type: files; Name: "{app}\saved_credentials.dat"
Type: files; Name: "{app}\recent_logins.json"
