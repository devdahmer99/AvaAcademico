@echo off
title Deploy AVA Academico - IIS e Modo Release
cls
echo ====================================================================
echo   INICIANDO DEPLOY DO AVA ACADEMICO COM ELEVACAO E EXECUTION POLICY
echo ====================================================================
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0deploy-release.ps1"
echo.
pause

