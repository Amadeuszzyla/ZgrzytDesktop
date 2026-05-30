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
   (Settings/, Cache/, Logs/, token.txt, audit-log.json, diagnostic.log)
   Nie wymaga uprawnień administratora Windows.

   Instalator (ZgrzytDesktopSetup.exe) używa logo projektu w kreatorze instalacji.
   Odinstalowanie przez Ustawienia Windows lub wpis w menu Start usuwa także
   katalog %AppData%\ZgrzytDesktop\ wraz z lokalnymi danymi aplikacji.

   Artefakty release (obok portable ZIP):
   - ZgrzytDesktopSetup.exe — instaluje aplikację w %LocalAppData%\Programs\ZgrzytDesktop\
   - ZgrzytDesktopUninstall.exe — uruchamia deinstalator Inno Setup zainstalowanej aplikacji
     (%LocalAppData%\Programs\ZgrzytDesktop\unins000.exe); nie usuwa plików ręcznie
   - Portable ZIP (ZgrzytDesktop-win-x64-release.zip) — usuwa się ręcznie (skasuj folder publish)

7. Język
   W Ustawieniach: polski lub angielski. Interfejs jest tylko w jasnym motywie.

8. Problemy
   - Antywirus blokuje EXE: dodaj cały folder publish do wyjątków.
   - Brak internetu: możliwy podgląd wcześniej zapisanych zgłoszeń z cache.
   - Błąd połączenia: sprawdź firewall i dostęp do internetu.

Wersja: Release, win-x64, self-contained


Smoke test release (GitHub Actions — dla QA / maintainerów)
============================================================

Po ręcznym uruchomieniu workflow Release w GitHub Actions pobierz artefakty
z runu workflow (Actions → Release → wybrany run → Artifacts):

  - ZgrzytDesktop-win-x64-release.zip
  - ZgrzytDesktop-win-x64-release.zip.sha256
  - ZgrzytDesktopSetup.exe
  - ZgrzytDesktopSetup.exe.sha256
  - ZgrzytDesktopUninstall.exe
  - ZgrzytDesktopUninstall.exe.sha256

1. Pobierz ZIP i plik SHA256 do tego samego folderu, np. C:\Temp\ZgrzytRelease\

2. Zweryfikuj checksumę (PowerShell):

   cd C:\Temp\ZgrzytRelease

   $zipPath = ".\ZgrzytDesktop-win-x64-release.zip"
   $checksumPath = ".\ZgrzytDesktop-win-x64-release.zip.sha256"

   $computed = (Get-FileHash -Path $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
   $expected = (Get-Content -Path $checksumPath -Raw).Trim().Split([char[]]@(' ', "`t"), [StringSplitOptions]::RemoveEmptyEntries)[0].ToLowerInvariant()

   if ($computed -eq $expected) {
       Write-Host "SHA256 OK: $computed"
   } else {
       throw "SHA256 mismatch. Expected=$expected Actual=$computed"
   }

   Skrót (tylko wyświetlenie hashy obok siebie):

   Get-FileHash -Path $zipPath -Algorithm SHA256
   Get-Content -Path $checksumPath

3. Rozpakuj ZIP do pustego folderu, np. C:\Temp\ZgrzytRelease\app\

   Expand-Archive -Path $zipPath -DestinationPath ".\app" -Force

4. Sprawdź zawartość paczki:

   Test-Path ".\app\ZgrzytDesktop.exe"        # musi być True
   Test-Path ".\app\README_RELEASE.txt"       # musi być True

5. Uruchom aplikację:

   Start-Process ".\app\ZgrzytDesktop.exe"

6. Smoke test UI (ręcznie):

   [ ] Otwiera się okno aplikacji (bez crasha przy starcie).
   [ ] Widoczny jest ekran logowania (pola login/hasło, przycisk logowania).
   [ ] Widoczna jest wersja aplikacji (badge wersji na ekranie logowania).
   [ ] README_RELEASE.txt jest w rozpakowanym folderze obok ZgrzytDesktop.exe.

7. Opcjonalnie: zaloguj się kontem IT/admin i sprawdź, czy dashboard się ładuje.
   (Wymaga działającego API i poprawnych danych logowania.)

Kryterium PASS: checksuma OK, EXE i README w paczce, aplikacja startuje,
ekran logowania i wersja widoczne. FAIL: brak pliku, błąd hash, crash przy starcie.


Smoke test instalatora (GitHub Actions — dla QA / maintainerów)
================================================================

Po ręcznym uruchomieniu workflow Release pobierz artefakty instalatora z runu
(Actions → Release → wybrany run → Artifacts):

  - ZgrzytDesktopSetup.exe
  - ZgrzytDesktopSetup.exe.sha256

1. Pobierz oba pliki do tego samego folderu, np. C:\Temp\ZgrzytInstaller\

2. Zweryfikuj checksumę (PowerShell):

   cd C:\Temp\ZgrzytInstaller

   $setupPath = ".\ZgrzytDesktopSetup.exe"
   $checksumPath = ".\ZgrzytDesktopSetup.exe.sha256"

   $computed = (Get-FileHash -Path $setupPath -Algorithm SHA256).Hash.ToLowerInvariant()
   $expected = (Get-Content -Path $checksumPath -Raw).Trim().Split([char[]]@(' ', "`t"), [StringSplitOptions]::RemoveEmptyEntries)[0].ToLowerInvariant()

   if ($computed -eq $expected) {
       Write-Host "SHA256 OK: $computed"
   } else {
       throw "SHA256 mismatch. Expected=$expected Actual=$computed"
   }

3. Uruchom instalator (double-click lub z wiersza poleceń):

   Start-Process -FilePath $setupPath -Wait

   Podczas instalacji:
   - kreator instalatora wyświetla logo ZgrzytDesktop,
   - domyślnie powstaje skrót w menu Start (ZgrzytDesktop),
   - opcjonalnie zaznacz skrót na pulpicie.

4. Uruchom aplikację ze skrótu Start Menu lub z folderu instalacji
   (domyślnie: %LocalAppData%\Programs\ZgrzytDesktop\ZgrzytDesktop.exe).

5. Smoke test UI (ręcznie):

   [ ] Instalator kończy się bez błędu.
   [ ] Skrót w menu Start działa i ma ikonę logo ZgrzytDesktop.
   [ ] ZgrzytDesktop.exe w folderze instalacji ma ikonę logo.
   [ ] Aplikacja startuje (ekran logowania, badge wersji).
   [ ] README_RELEASE.txt jest w folderze instalacji obok ZgrzytDesktop.exe.

6. Odinstalowanie (opcjonalnie):

   Ustawienia Windows → Aplikacje → ZgrzytDesktop → Odinstaluj
   (lub wpis „Odinstaluj ZgrzytDesktop” w menu Start).

   Po odinstalowaniu sprawdź, że usunięto:
   - folder instalacji (%LocalAppData%\Programs\ZgrzytDesktop\),
   - skróty Start Menu / pulpit,
   - dane użytkownika (%AppData%\ZgrzytDesktop\).

Kryterium PASS: checksuma OK, instalacja i skrót Start Menu działają, aplikacja startuje,
ikona logo widoczna, odinstalowanie usuwa pliki programu i dane z AppData.
FAIL: błąd hash, instalator się wykrzacza, brak skrótu, brak ikony, crash przy starcie,
pozostałe pliki w AppData po odinstalowaniu.


Smoke test deinstalatora (GitHub Actions — dla QA / maintainerów)
==================================================================

Po ręcznym uruchomieniu workflow Release pobierz artefakty deinstalatora z runu
(Actions → Release → wybrany run → Artifacts):

  - ZgrzytDesktopUninstall.exe
  - ZgrzytDesktopUninstall.exe.sha256

1. Zainstaluj aplikację przez ZgrzytDesktopSetup.exe (patrz sekcja powyżej).

2. Pobierz oba pliki deinstalatora do folderu, np. C:\Temp\ZgrzytUninstall\

3. Zweryfikuj checksumę (PowerShell):

   cd C:\Temp\ZgrzytUninstall

   $uninstallPath = ".\ZgrzytDesktopUninstall.exe"
   $checksumPath = ".\ZgrzytDesktopUninstall.exe.sha256"

   $computed = (Get-FileHash -Path $uninstallPath -Algorithm SHA256).Hash.ToLowerInvariant()
   $expected = (Get-Content -Path $checksumPath -Raw).Trim().Split([char[]]@(' ', "`t"), [StringSplitOptions]::RemoveEmptyEntries)[0].ToLowerInvariant()

   if ($computed -eq $expected) {
       Write-Host "SHA256 OK: $computed"
   } else {
       throw "SHA256 mismatch. Expected=$expected Actual=$computed"
   }

4. Uruchom ZgrzytDesktopUninstall.exe (double-click).

   [ ] Otwiera się kreator deinstalacji Inno Setup (unins000.exe).
   [ ] Po zakończeniu odinstalowania znikają folder instalacji i dane w AppData.

5. Uruchom ZgrzytDesktopUninstall.exe ponownie bez instalacji:

   [ ] Pojawia się komunikat: „ZgrzytDesktop nie jest zainstalowany albo deinstalator nie został znaleziony.”

Kryterium PASS: checksuma OK, wrapper uruchamia unins000.exe gdy aplikacja zainstalowana,
komunikat błędu gdy brak instalacji. FAIL: błąd hash, brak reakcji, brak komunikatu przy braku instalacji.
