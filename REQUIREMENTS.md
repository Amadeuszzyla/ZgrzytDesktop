# Requirements — ZGRZYT Desktop

## 1. System

Aplikacja desktopowa jest przygotowana pod system Windows.

Powód: projekt korzysta z `net10.0-windows` oraz `System.Windows.Forms.NotifyIcon` do systemowych powiadomień Windows.

## 2. Wymagane narzędzia

Do uruchomienia projektu wymagane są:

- Windows 10 lub Windows 11
- .NET SDK 10.0 lub nowszy zgodny z projektem
- JetBrains Rider albo inne IDE obsługujące projekty .NET
- Dostęp do lokalnego API ZGRZYT, jeżeli aplikacja ma działać w trybie online

## 3. Backend API

Domyślny adres API:

```text
http://127.0.0.1:9000/api/

