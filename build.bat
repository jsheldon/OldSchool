@ECHO OFF
SET VERSION="1.2.3.4"
IF NOT "%1"=="" (
    SET VERSION=%1
)

@powershell .\Build\Build.ps1 -CommandName Test -Version %VERSION%