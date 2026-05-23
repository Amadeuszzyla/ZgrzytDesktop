ZGRZYT Desktop — instrukcja uruchomienia (Windows x64)
=====================================================

1. Rozpakuj CAŁE archiwum ZIP do folderu, np.:
   C:\ZgrzytDesktop\

   W folderze muszą być m.in. ZgrzytDesktop.exe, pliki DLL i podfolder runtime.
   Nie przenoś samego pliku EXE do innego katalogu — aplikacja nie wystartuje.

2. Uruchom program:
   ZgrzytDesktop.exe

3. Wersja self-contained
   Ta paczka zawiera .NET runtime — nie musisz instalować .NET Desktop Runtime na tym komputerze.

4. API
   Aplikacja łączy się z:
   https://zgrzyt-api.onrender.com/api/

   Przy pierwszym uruchomieniu po bezczynności serwer na Render może odpowiadać wolno (cold start).
   Odczekaj ok. 30–60 sekund i spróbuj ponownie.

5. Logowanie
   Użyj konta utworzonego w systemie ZGRZYT (login i hasło z backendu).

6. Język i wygląd
   W sekcji Ustawienia możesz wybrać język interfejsu: polski lub angielski.
   Aplikacja używa wyłącznie jasnego motywu (light).

7. Dane lokalne (Windows)
   Token, cache i ustawienia są zapisywane per użytkownik Windows w:
   %AppData%\ZgrzytDesktop\

   Nie wymaga uprawnień administratora.

8. Offline
   Przy braku połączenia z API aplikacja może pokazać dane z lokalnego cache (jeśli były wcześniej pobrane).

9. Problemy
   - Antywirus blokuje EXE: dodaj cały folder publish do wyjątków.
   - Brak połączenia: sprawdź internet i firewall.
   - Stary adres API w settings.json: aplikacja migruje localhost na produkcyjny URL przy starcie.

Wersja: Release, win-x64, self-contained (folder publish)
Paczka: ZgrzytDesktop-win-x64-release.zip (skrypt scripts/publish-release.ps1)
