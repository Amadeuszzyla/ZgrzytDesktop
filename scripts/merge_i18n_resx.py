# -*- coding: utf-8 -*-
import pathlib

ROOT = pathlib.Path(__file__).resolve().parents[1]
pl_path = ROOT / "ZgrzytDesktop" / "Resources" / "AppStrings.resx"
en_path = ROOT / "ZgrzytDesktop" / "Resources" / "AppStrings.en.resx"
pl_keys = ROOT / "i18n_pl_keys.txt"
en_keys = ROOT / "i18n_en_keys.txt"

PHASE15_PL = ROOT / "scripts" / "phase15_pl_snippet.xml"
PHASE15_EN = ROOT / "scripts" / "phase15_en_snippet.xml"


def merge(path: pathlib.Path, phase15: pathlib.Path, new_keys: pathlib.Path, email_pl: str, email_en: str):
    text = path.read_text(encoding="utf-8")
    insert = ""
    if "App_BrandName" not in text and phase15.exists():
        insert += phase15.read_text(encoding="utf-8")
    if new_keys.exists():
        nk = new_keys.read_text(encoding="utf-8")
        if "Status_New" not in text:
            insert += nk
    if "RequestAccount_ValidationEmail" not in text:
        insert += email_pl if path.name == "AppStrings.resx" else email_en
    if insert:
        text = text.replace("</root>", insert + "</root>")
        path.write_text(text, encoding="utf-8")


merge(
    pl_path,
    PHASE15_PL,
    pl_keys,
    '  <data name="RequestAccount_ValidationEmail" xml:space="preserve"><value>Podaj adres e-mail.</value></data>\n',
    '  <data name="RequestAccount_ValidationEmail" xml:space="preserve"><value>Enter email address.</value></data>\n',
)
merge(
    en_path,
    PHASE15_EN,
    en_keys,
    "",
    '  <data name="RequestAccount_ValidationEmail" xml:space="preserve"><value>Enter email address.</value></data>\n',
)
print("merged", pl_path.stat().st_size, en_path.stat().st_size)
