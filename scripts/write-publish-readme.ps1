#Requires -Version 5.1
<#
.SYNOPSIS
    Generuje krótki README_RELEASE.txt w folderze publish (ZIP / instalator).
#>
param(
    [Parameter(Mandatory)]
    [string]$DestinationPath
)

$content = @'
ZGRZYT Desktop — instrukcja uruchomienia (Windows x64)
=====================================================

1. Rozpakuj CAŁE archiwum ZIP do folderu, np. C:\ZgrzytDesktop\
   W folderze muszą być ZgrzytDesktop.exe, pliki DLL i podfolder runtime.
   Nie przenoś samego pliku EXE — aplikacja nie wystartuje.

2. Uruchom: ZgrzytDesktop.exe

3. Połączenie z API
   Domyślnie: https://zgrzyt-api.onrender.com/api/
   Po dłuższej bezczynności serwer (Render) może odpowiadać wolno — odczekaj ok. 30–60 s i spróbuj ponownie.

4. Kto może korzystać z aplikacji
   Tylko konta z rolą IT lub administrator (admin).
   Zwykły użytkownik (user) nie uzyska dostępu do panelu po logowaniu.

5. Logowanie
   Użyj loginu i hasła z systemu ZGRZYT (konto IT lub admin).

6. Dane na tym komputerze
   Token, cache i ustawienia: %AppData%\ZgrzytDesktop\
   Nie wymaga uprawnień administratora Windows.

7. Język
   W Ustawieniach: polski lub angielski. Interfejs jest tylko w jasnym motywie.

8. Problemy
   - Antywirus blokuje EXE: dodaj cały folder publish do wyjątków.
   - Brak internetu: możliwy podgląd wcześniej zapisanych zgłoszeń z cache.
   - Błąd połączenia: sprawdź firewall i dostęp do internetu.

Wersja: Release, win-x64, self-contained
Pełna dokumentacja projektu: README.md w repozytorium GitHub.
'@

$directory = Split-Path -Parent $DestinationPath
if ($directory -and -not (Test-Path $directory)) {
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
}

[System.IO.File]::WriteAllText($DestinationPath, $content, [Text.UTF8Encoding]::new($false))
