ZGRZYT Desktop — instrukcja uruchomienia (Windows x64)
=====================================================

1. Rozpakuj cały archiwum ZIP do folderu, np.:
   C:\ZgrzytDesktop\

2. Uruchom program:
   ZgrzytDesktop.exe

   WAŻNE: Nie przenoś samego pliku EXE poza folder publish.
   Aplikacja wymaga wszystkich bibliotek DLL i plików runtime w tym samym katalogu.

3. Wersja self-contained
   Ta paczka zawiera .NET runtime — nie musisz instalować .NET Desktop Runtime na tym komputerze.

4. API
   Aplikacja łączy się z:
   https://zgrzyt-api.onrender.com/api/

   Przy pierwszym uruchomieniu po bezczynności serwer na Render może odpowiadać wolno (cold start).
   Odczekaj ok. 30–60 sekund i spróbuj ponownie.

5. Logowanie
   Użyj konta utworzonego w systemie ZGRZYT (login i hasło z backendu).

6. Dane lokalne (Windows)
   Token, cache i ustawienia są zapisywane per użytkownik Windows w:
   %AppData%\ZgrzytDesktop\

   Nie wymaga uprawnień administratora.

7. Offline
   Przy braku połączenia z API aplikacja może pokazać dane z lokalnego cache (jeśli były wcześniej pobrane).

8. Problemy
   - Antywirus blokuje EXE: dodaj folder publish do wyjątków.
   - Brak połączenia: sprawdź internet i firewall.
   - Stary adres API w settings.json: aplikacja migruje localhost na produkcyjny URL przy starcie.

Wersja: Release, win-x64, self-contained (folder publish)
