# Requirements — ZGRZYT Desktop

## 1. System

Aplikacja desktopowa jest przeznaczona na **Windows 10/11**.

Projekt używa `net10.0-windows` (Avalonia Desktop). Flaga `UseWindowsForms` w projekcie nie służy powiadomieniom systemowym — aplikacja **nie** wyświetla toastów ani ikon w zasobniku Windows.

## 2. Wymagane narzędzia

- Windows 10 lub Windows 11
- [.NET SDK 10](https://dotnet.microsoft.com/download) zgodny z `net10.0-windows`
- JetBrains Rider, Visual Studio, **Cursor** lub VS Code (opcjonalnie profil `.vscode/launch.json`)
- Działające API ZGRZYT w trybie online (domyślnie lokalnie)

## 3. Backend API

Domyślny adres (kod / `settings.json`, bez edycji w UI):

```text
http://127.0.0.1:9000/api/
```

Szczegóły endpointów i ograniczeń: [README_DESKTOP_STATUS.md](README_DESKTOP_STATUS.md).

## 4. Runtime na maszynie docelowej (publish)

Przy publikacji `--self-contained false` wymagany jest **.NET 10 Desktop Runtime** na PC użytkownika.
