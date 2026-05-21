# ZGRZYT Desktop — status i ograniczenia

Dokument opisuje zgodność aplikacji Avalonia z wymaganiami projektu oraz ograniczenia wynikające z API (bez zmian backendu).

## Co działa

- Logowanie (`POST /api/login`), Bearer, `GET /api/user`, wylogowanie (`POST /api/logout`)
- Prośba o konto (`POST /api/request-account`) — wymaga zalogowania
- Lista zgłoszeń: `tickets`, `active-tickets`, `unassigned-tickets` (filtry, wyszukiwanie, paginacja)
- Szczegóły zgłoszenia, wiadomości (`GET/POST .../messages`, pole JSON `body`)
- Edycja statusu/priorytetu i przypisanie — role **it** / **admin**
- Kategorie (Hardware, Software, Sieć) — zapis w tytule `[Kategoria]` i linii opisu `Kategoria: ...` (brak pola `category` w API)
- Statusy UI: Nowe / W toku / Rozwiązane → API: `nowe` / `w trakcie` / `zamknięte`
- Toast w aplikacji, tryb jasny/ciemny/system (zapis w ustawieniach)
- Szyfrowanie lokalne (DPAPI): token, cache użytkownika, cache zgłoszeń, audyt lokalny
- **Historia lokalnych zmian** w szczegółach zgłoszenia (audyt desktopowy, nie logi backendu)

## Ograniczenia API (świadome)

| Obszar | Ograniczenie |
|--------|----------------|
| Logi systemowe (`GET /api/logs`) | Brak endpointu w OpenAPI — opis w tym pliku; dane z backendu nie są pobierane w UI |
| Pole `category` | Nie wysyłane w requestach (uniknięcie 422); kategoria tylko w tytule/opisie |
| Zamknięcie zgłoszenia przez **user** | Przycisk „Zamknij” widoczny także dla autora zgłoszenia; jeśli API zwróci **403**, desktop pokazuje komunikat — backend może nadal wymagać roli it/admin |
| Panel administracji użytkowników | Sekcja **Administracja** (rola admin): `GET users`, `POST users/{id}/ban`, `POST users/{id}/activate` — przy braku uprawnień: **403** |
| `POST /api/refresh` | Dostępne w serwisie; brak przycisku w uproszczonych ustawieniach UI |
| Statystyki globalne | Domyślnie **bieżąca strona**; przycisk „Pobierz statystyki ze wszystkich stron” agreguje listę przez wielokrotne `GET tickets` (bez osobnego raportu) |
| `DELETE /api/tickets/{id}` | Dostępne w serwisie; w UI tylko dla ról z uprawnieniem; przy braku uprawnień: 403 |

## Pliki lokalne (AppData)

- `%AppData%/ZgrzytDesktop/token.txt` — token (DPAPI)
- `%AppData%/ZgrzytDesktop/Cache/` — cache użytkownika i zgłoszeń (DPAPI)
- `%AppData%/ZgrzytDesktop/audit-log.json` — lokalny audyt (DPAPI)
- `%AppData%/ZgrzytDesktop/Settings/settings.json` — motyw aplikacji (adres API jest stały w kodzie, nie edytowany w UI)

## Role w UI

- **user**: lista, szczegóły (po wyborze wiersza), wiadomości, tworzenie zgłoszeń, **Zgłoś nowe konto** w menu
- **it** / **admin**: dodatkowo edycja statusu/priorytetu, przypisanie, zamknięcie, usuwanie; **Nowe konto** w panelu Administracja
- **admin**: dodatkowo zakładka Użytkownicy (ban/aktywacja)

## Dane testowe

Tytuły i treści zgłoszeń na liście pochodzą z API backendu — aplikacja desktop nie filtruje ich treści. Przykładowe dane w środowisku dev należy czyścić po stronie bazy/API.

Autoryzacja końcowa zawsze po stronie API (401/403).
