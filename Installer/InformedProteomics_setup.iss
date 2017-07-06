; This is an Inno Setup configuration file
; http://www.jrsoftware.org/isinfo.php

#define ApplicationVersion GetFileVersion('..\InformedProteomics.Backend\bin\Release\InformedProteomics.Backend.dll')

[CustomMessages]
AppName=DeconTools
[Messages]
; WelcomeLabel2 set in the code section
; WelcomeLabel2=This will install [name/ver] on your computer.%n%n%n%nNOTICE:%nReading of some data files requires access to a "{code:GetInstallArch}"-bit ProteoWizard installation. Please install "{code:GetInstallArch}"-bit ProteoWizard before using the program to avoid errors.%n%n
; Example with multiple lines:
; WelcomeLabel2=Welcome message%n%nAdditional sentence
[Files]
; InformedProteomics.Backend
Source: InformedProteomics.Backend\bin\Release\InformedProteomics.Backend.dll                                                                     ; DestDir: {app}
Source: InformedProteomics.Backend\bin\Release\InformedProteomics.Backend.dll.config                                                              ; DestDir: {app}

; InformedProteomics.Backend Nuget libraries
Source: InformedProteomics.Backend\bin\Release\MathNet.Numerics.dll                                                                               ; DestDir: {app}
Source: InformedProteomics.Backend\bin\Release\PSI_Interface.dll                                                                                  ; DestDir: {app}
Source: InformedProteomics.Backend\bin\Release\DotNetZip.dll                                                                                      ; DestDir: {app}

; SAIS
Source: SAIS\bin\Release\SAIS.dll                                                                                                                 ; DestDir: {app}

; Manually managed libraries
Source: lib\alglibnet2.dll                                                                                                                        ; DestDir: {app}
Source: lib\PNNLOmics.dll                                                                                                                         ; DestDir: {app}
Source: lib\ProteinFileReader.dll                                                                                                                 ; DestDir: {app}
Source: lib\ThermoRawFileReader.dll                                                                                                               ; DestDir: {app}

; InformedProteomics.BottomUp
Source: InformedProteomics.BottomUp\bin\Release\InformedProteomics.BottomUp.dll                                                                   ; DestDir: {app}

; InformedProteomics.FeatureFinding Nuget libraries
Source: InformedProteomics.FeatureFinding\bin\Release\OxyPlot.dll                                                                                 ; DestDir: {app}
Source: InformedProteomics.FeatureFinding\bin\Release\OxyPlot.Wpf.dll                                                                             ; DestDir: {app}

; InformedProteomics.FeatureFinding libraries
Source: InformedProteomics.FeatureFinding\bin\Release\InformedProteomics.FeatureFinding.dll                                                       ; DestDir: {app}

; InformedProteomics.Scoring
Source: InformedProteomics.Scoring\bin\Release\InformedProteomics.Scoring.dll                                                                     ; DestDir: {app}

; InformedProteomics.TopDown
Source: InformedProteomics.TopDown\bin\Release\InformedProteomics.TopDown.dll                                                                     ; DestDir: {app}

; TopDownTrainer
Source: lib\TopDownTrainer\Accord.dll                                                                                                             ; DestDir: {app}
Source: lib\TopDownTrainer\Accord.MachineLearning.dll                                                                                             ; DestDir: {app}
Source: lib\TopDownTrainer\Accord.Math.Core.dll                                                                                                   ; DestDir: {app}
Source: lib\TopDownTrainer\Accord.Math.dll                                                                                                        ; DestDir: {app}
Source: lib\TopDownTrainer\Accord.Statistics.dll                                                                                                  ; DestDir: {app}
Source: lib\TopDownTrainer\TopDownTrainer.exe                                                                                                     ; DestDir: {app}

; InformedProteomics.TopDown Nuget libraries
;Source: InformedProteomics.TopDown\bin\Release\MathNet.Numerics.dll                                                                               ; DestDir: {app}

; MSPathFinder (bottom up)
;Source: MSPathFinder\bin\Release\MSPathFinder.exe                                                                                                 ; DestDir: {app}
;Source: MSPathFinder\bin\Release\MSPathFinder.exe.config                                                                                          ; DestDir: {app}

; MSPathFinder (top down)
Source: MSPathFinderT\bin\Release\MSPathFinderT.exe                                                                                               ; DestDir: {app}
Source: MSPathFinderT\bin\Release\MSPathFinderT.exe.config                                                                                        ; DestDir: {app}

; PbfGen
Source: PbfGen\bin\Release\PbfGen.exe                                                                                                             ; DestDir: {app}
Source: PbfGen\bin\Release\PbfGen.exe.config                                                                                                      ; DestDir: {app}

; ProMex
Source: ProMex\bin\Release\ProMex.exe                                                                                                             ; DestDir: {app}
Source: ProMex\bin\Release\ProMex.exe.config                                                                                                      ; DestDir: {app}

Source: README.md                                                                                                                                 ; DestDir: {app}; DestName: "Readme.txt"
;Source: RevisionHistory.txt                                                                                                                       ; DestDir: {app}
Source: Example_Files\Mod_Examples.txt                                                                                                            ; DestDir: {app}
Source: Example_Files\MSPathFinder_Mods.txt                                                                                                       ; DestDir: {app}
Source: Example_Files\MSPathFinder_Mods_Phospho.txt                                                                                               ; DestDir: {app}


[Dirs]
Name: {commonappdata}\Informed-Proteomics; Flags: uninsalwaysuninstall

;[Tasks]
;Name: desktopicon; Description: {cm:CreateDesktopIcon}; GroupDescription: {cm:AdditionalIcons}; Flags: unchecked
; Name: quicklaunchicon; Description: {cm:CreateQuickLaunchIcon}; GroupDescription: {cm:AdditionalIcons}; Flags: unchecked

[Icons]
Name: {group}\Command Line; Filename: {cmd}; Parameters: "/K ""set PATH=%PATH%;{app}"""; WorkingDir: {userdocs}; Comment: Command Prompt with direct access to Informed-Proteomics executables
Name: {group}\ReadMe; Filename: {app}\Readme.txt; Comment: Informed-Proteomics ReadMe
Name: {group}\Mod Examples; Filename: {app}\Mod_Examples.txt; Comment: Example Modifications
Name: {group}\MSPathFinder Mods; Filename: {app}\MSPathFinder_Mods.txt; Comment: Example Modifications file for MSPathFinder
Name: {group}\MSPathFinder Mods Phospho; Filename: {app}\MSPathFinder_Mods_Phospho.txt; Comment: Example Modifications file for MSPathFinder
Name: {group}\Uninstall Informed-Proteomics; Filename: {uninstallexe}

;C:\Windows\System32\cmd.exe /K "set PATH=%PATH%;%ProgramFiles%\InformedProteomics"

[Setup]
; As AnyCPU, we can install as 32-bit or 64-bit, so allow installing on 32-bit Windows, but make sure it installs as 64-bit on 64-bit Windows
ArchitecturesInstallIn64BitMode=x64
ArchitecturesAllowed=x64 x86
AppName=Informed-Proteomics
AppVersion={#ApplicationVersion}
;AppVerName=InformedProteomics
AppID=Informed-Proteomics-Id
AppPublisher=Pacific Northwest National Laboratory
AppPublisherURL=http://omics.pnl.gov/software
AppSupportURL=http://omics.pnl.gov/software
AppUpdatesURL=https://github.com/PNNL-Comp-Mass-Spec/Informed-Proteomics/releases
DefaultDirName={pf}\Informed-Proteomics
DefaultGroupName=Informed-Proteomics
AppCopyright=� PNNL
;LicenseFile=.\License.rtf
PrivilegesRequired=poweruser
OutputBaseFilename=Informed-Proteomics_Installer
;VersionInfoVersion=1.57
VersionInfoVersion={#ApplicationVersion}
VersionInfoCompany=PNNL
VersionInfoDescription=Informed-Proteomics
VersionInfoCopyright=PNNL
DisableFinishedPage=true
ShowLanguageDialog=no
;SetupIconFile=..\MageFileProcessor\wand.ico
;InfoBeforeFile=.\readme.rtf
ChangesAssociations=false
;WizardImageFile=..\Deploy\Images\MageSetupSideImage.bmp
;WizardSmallImageFile=..\Deploy\Images\MageSetupSmallImage.bmp
;InfoAfterFile=.\postinstall.rtf
EnableDirDoesntExistWarning=false
AlwaysShowDirOnReadyPage=true
UninstallDisplayIcon={app}\delete_16x.ico
ShowTasksTreeLines=true
OutputDir=Installer\Output
SourceDir=..\
Compression=lzma
SolidCompression=yes
[Registry]
;Root: HKCR; Subkey: MageFile; ValueType: string; ValueName: ; ValueData:Mage File; Flags: uninsdeletekey
;Root: HKCR; Subkey: MageSetting\DefaultIcon; ValueType: string; ValueData: {app}\wand.ico,0; Flags: uninsdeletevalue
[UninstallDelete]
Name: {app}; Type: filesandordirs


[Code]
function GetInstallArch(): String;
begin
  { Return a user value }
  if Is64BitInstallMode then
    Result := '64'
  else
    Result := '32';
end;

procedure InitializeWizard;
var
  message2_a: string;
  message2_b: string;
  message2_c: string;
  message2: string;
  appname: string;
  appversion: string;
  RichViewer: TRichEditViewer;
begin
  appname := '{#SetupSetting("AppName")}';
  appversion := '{#SetupSetting("AppVersion")}';
  (* This is the old version, that doesn't support any hyperlink. Using RTF instead to provide a link. *)
  (* #13 is carriage return, #10 is new line *)
  (*message2_a := 'This will install ' + appname + ' version ' + appversion + ' on your computer.' + #10#10#10 +   *)
  (*  'NOTICE:' + #10 + 'Reading of some data files requires access to a ';                                        *)
  (*message2_b := '-bit ProteoWizard or Thermo MSFileReader installation. Please install ';                        *)
  (*message2_c := '-bit ProteoWizard and/or Thermo MSFileReader before using the program to avoid errors.' + #10 + *)
  (*  'See https://github.com/PNNL-Comp-Mass-Spec/Informed-Proteomics/wiki/PBFGen-Usage for details.' + #10#10;    *)
  (*message2 := message2_a + GetInstallArch + message2_b + GetInstallArch + message2_c;                            *)
  (*WizardForm.WelcomeLabel2.Caption := message2;                                                                  *)

  RichViewer := TRichEditViewer.Create(WizardForm);
  RichViewer.Left := WizardForm.WelcomeLabel2.Left;
  RichViewer.Top := WizardForm.WelcomeLabel2.Top;
  RichViewer.Width := WizardForm.WelcomeLabel2.Width;
  RichViewer.Height := WizardForm.WelcomeLabel2.Height;
  RichViewer.Parent := WizardForm.WelcomeLabel2.Parent;
  RichViewer.BorderStyle := bsNone;
  RichViewer.TabStop := False;
  RichViewer.ReadOnly := True;
  WizardForm.WelcomeLabel2.Visible := False;

  RichViewer.RTFText := '{\rtf1{\colortbl ;\red0\green0\blue255;} This will install ' + appname + ' version ' + appversion + ' on your computer.' + '\line \line \line ' +
    '{\b NOTICE:}\line ' + 'Reading of some data files requires access to a {\b ' + GetInstallArch + '-bit} ProteoWizard or Thermo MSFileReader installation. Please install {\b ' +
    GetInstallArch + '-bit} ProteoWizard and/or Thermo MSFileReader before using the program to avoid errors.' + '\line \line ' +
    'See {\field{\*\fldinst HYPERLINK "https://github.com/PNNL-Comp-Mass-Spec/Informed-Proteomics/wiki/PBFGen-Usage" }{\fldrslt \b PBFGen Usage}} for details.' + '\line \line}';
end;
