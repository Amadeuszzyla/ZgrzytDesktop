import re
import pathlib

root = pathlib.Path(__file__).resolve().parents[1] / "ZgrzytDesktop" / "Resources"
for name in ["AppStrings.resx", "AppStrings.en.resx"]:
    text = (root / name).read_text(encoding="utf-8")
    keys = [m for m in re.findall(r'<data name="([^"]+)"', text) if not m.startswith("res")]
    print(name, len(keys))
