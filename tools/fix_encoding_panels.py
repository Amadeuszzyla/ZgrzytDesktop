"""Fix mojibake in DashboardParts XAML (UTF-8)."""
import pathlib

ROOT = pathlib.Path(__file__).resolve().parents[1]
PARTS = ROOT / "ZgrzytDesktop" / "Views" / "DashboardParts"

FIXES = {
    "Na stron─Ö:": "Na stronę:",
    "Tytu┼é": "Tytuł",
    "zg┼éoszenie": "zgłoszenie",
    "zg┼éoszenia": "zgłoszenia",
    "zg┼éoszeniu": "zgłoszeniu",
    "zg┼éoszeniem": "zgłoszeniem",
    "Pro┼Ťba": "Prośba",
    "wys┼éana": "wysłana",
    "Zg┼éo┼Ť": "Zgłoś",
    "Uzupe┼énij": "Uzupełnij",
    "utw├│rz": "utwórz",
    "Utw├│rz": "Utwórz",
    "Imi─Ö": "Imię",
    "u┼╝ytkownika": "użytkownika",
    "Zg┼éo┼Ť": "Zgłoś",
    "Wype┼énij": "Wypełnij",
    "pro┼Ťb─Ö": "prośbę",
    "Wy┼Ťlij": "Wyślij",
    "wpis├│w": "wpisów",
    "wiadomo┼Ťci": "wiadomości",
    "Wiadomo┼Ťci": "Wiadomości",
    "Wpisz wiadomo┼Ť─ç": "Wpisz wiadomość...",
    "Zg┼éaszaj─ůcy": "Zgłaszający",
    "Mo┼╝esz": "Możesz",
    "zamkn─ů─ç": "zamknąć",
    "w┼éasne": "własne",
    "je┼Ťli": "jeśli",
    "b┼é─Ödzie": "błędzie",
    "dost─Öp": "dostęp",
    "podgl─ůdu": "podglądu",
    "s─ů": "są",
    "r├│l": "ról",
    "Zarz─ůdzanie": "Zarządzanie",
    "Usu┼ä": "Usuń",
    "dzia┼éa┼ä": "działań",
    "ÔÇö": "—",
    "ÔÇ╣": "Poprzednia",
    "ÔÇ║": "Następna",
    "┬ź": "Pierwsza",
    "┬╗": "Ostatnia",
    "┬Ě": "·",
}


def apply_fixes(text: str) -> str:
    for bad, good in FIXES.items():
        text = text.replace(bad, good)
    return text


def main() -> None:
    for path in sorted(PARTS.glob("*.axaml")):
        original = path.read_text(encoding="utf-8")
        fixed = apply_fixes(original)
        if fixed != original:
            path.write_text(fixed, encoding="utf-8")
            print(f"Fixed {path.name}")


if __name__ == "__main__":
    main()
