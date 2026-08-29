@echo off
title Publicando EJLAcademy AVA no IIS...
echo ========================================================
echo Publicando atualizacoes do EJLAcademy AVA no IIS
echo ========================================================
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0setup-iis.ps1"

echo.
pause
