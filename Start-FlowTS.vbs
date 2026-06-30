Set shell = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")
root = fso.GetParentFolderName(WScript.ScriptFullName)
exe = root & "\dist\FlowTS\FlowTS.exe"
shell.Run Chr(34) & exe & Chr(34), 0, False
