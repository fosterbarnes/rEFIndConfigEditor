[Code]
var
  RemoveSettings: Boolean;

function InitializeUninstall(): Boolean;
var
  SettingsDir: String;
begin
  RemoveSettings := False;
  SettingsDir := ExpandConstant('{userappdata}\{#AppName}');

  if DirExists(SettingsDir) and not UninstallSilent() then
    RemoveSettings := MsgBox(
      'Do you also want to remove all settings and data stored in:' + #13#10 + #13#10 +
      SettingsDir + #13#10 + #13#10 +
      'This cannot be undone.',
      mbConfirmation, MB_YESNO) = IDYES;

  Result := True;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  SettingsDir: String;
begin
  if (CurUninstallStep = usPostUninstall) and RemoveSettings then
  begin
    SettingsDir := ExpandConstant('{userappdata}\{#AppName}');
    if DirExists(SettingsDir) then
      DelTree(SettingsDir, True, True, True);
  end;
end;
