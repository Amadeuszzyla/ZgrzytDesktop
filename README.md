# ZGRZYT Desktop

Desktopowy klient systemu ZGRZYT — Zintegrowanego Systemu Zgłoszeń i Rejestru Zdarzeń Technicznych.

Aplikacja służy do obsługi zgłoszeń technicznych z poziomu komputera stacjonarnego. Komunikuje się z backendem API przygotowanym w Laravelu.

## Funkcje aplikacji

Aktualnie aplikacja obsługuje:

- logowanie użytkownika,
- autologowanie na podstawie zapisanego tokenu,
- pobieranie listy zgłoszeń z API,
- paginację zgłoszeń,
- wyszukiwanie zgłoszeń,
- filtrowanie po statusie i priorytecie,
- sortowanie widocznych danych w tabeli,
- podgląd szczegółów zgłoszenia,
- tworzenie nowych zgłoszeń,
- wysyłanie wiadomości w zgłoszeniu,
- zmianę statusu i priorytetu zgłoszenia dla ról `admin` oraz `it`,
- przypisywanie zgłoszenia do zalogowanego pracownika IT,
- zamykanie zgłoszeń,
- obsługę ról użytkowników,
- lokalny cache zgłoszeń i użytkownika,
- tryb offline dla danych zapisanych lokalnie,
- ustawienia adresu API,
- test połączenia z API,
- systemowe powiadomienia Windows,
- testy jednostkowe dla ustawień oraz cache.

## Struktura projektu

```text
ZgrzytDesktop/
│
├── ZgrzytDesktop/
│   ├── Assets/
│   ├── Cache/
│   ├── Converters/
│   ├── Exceptions/
│   ├── Models/
│   ├── Services/
│   ├── Storage/
│   ├── ViewModels/
│   ├── Views/
│   ├── App.axaml
│   ├── app.manifest
│   ├── Program.cs
│   ├── README.md
│   ├── REQUIREMENTS.md
│   ├── ZgrzytDesktop.csproj
│   └── .gitignore
│
├── ZgrzytDesktop.Tests/
│   ├── Cache/
│   ├── Services/
│   └── ZgrzytDesktop.Tests.csproj
│
└── ZgrzytDesktop.sln