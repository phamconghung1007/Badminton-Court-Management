@echo off
chcp 65001 >nul
echo ==========================================================================================
echo          KIỂM CHỨNG CÀI ĐẶT MÁY MỚI (FRESH-MACHINE SETUP VERIFICATION - DAY 10)
echo ==========================================================================================
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Verify-FreshMachine.ps1"
pause
