@echo off

REM MIT License

REM Copyright(c) 2019 Peter Kirmeier

REM Permission is hereby granted, free of charge, to any person obtaining a copy
REM of this software and associated documentation files (the "Software"), to deal
REM in the Software without restriction, including without limitation the rights
REM to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
REM copies of the Software, and to permit persons to whom the Software is
REM furnished to do so, subject to the following conditions:

REM The above copyright notice and this permission notice shall be included in all
REM copies or substantial portions of the Software.

REM THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
REM IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
REM FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
REM AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
REM LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
REM OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
REM SOFTWARE.

echo PrepareRelease.bat START ===========

REM To run this script you need to have 7zip installed at the default path
REM or you have to update the path:

set PATH=%PATH%;C:\Program Files\7-Zip
set PR_FINAL=FinalFiles
if not exist %PR_FINAL% mkdir %PR_FINAL%

echo Packing Portable Release:
set PR_BASE=bin\Release
set PR_TARGET=%PR_FINAL%\ReleasePortable
set PR_OUTPUT=%PR_FINAL%\IfrViewer_Portable_v1.x.y.z.zip
rmdir /S /Q %PR_TARGET% 2>nul
mkdir %PR_TARGET%

echo ^ ^ Copy files from %PR_BASE%
FOR %%G IN (IfrViewer.exe) DO copy %PR_BASE%\%%G %PR_TARGET%

del %PR_OUTPUT% 2>nul
7z a %PR_OUTPUT% .\%PR_TARGET%\*

echo PrepareRelease.bat END ===========
