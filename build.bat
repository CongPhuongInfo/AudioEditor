@echo off
setlocal

set VBC="C:\Windows\Microsoft.NET\Framework\v4.0.30319\vbc.exe"
set OUT=AudioExtractorApp.exe

echo Dang bien dich %OUT% ...

%VBC% /target:winexe ^
    /out:%OUT% ^
    /optimize+ ^
    /reference:System.dll ^
    /reference:System.Drawing.dll ^
    /reference:System.Windows.Forms.dll ^
    AudioExtractorApp.vb

if errorlevel 1 (
    echo.
    echo BUILD THAT BAI.
    exit /b 1
)

echo.
echo Build thanh cong: %OUT%
echo Nho copy ffmpeg.exe vao cung thu muc voi %OUT% truoc khi chay.
endlocal
