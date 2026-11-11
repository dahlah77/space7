#!/usr/bin/env python3
"""Human-reviewed fixes for short labels that machine translation mishandled."""
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / "Sources/Vorssaint/Core"
OVERRIDES = {
    "Localization.swift": {
        "statusIdleTooltip": "Space7: mode tidur normal",
        "menuActivateFor": "Aktifkan selama…",
        "updateChecking": "Memeriksa…",
        "indefinitely": "Tanpa batas waktu",
        "panelHotkeyHint": "Pintasan pengalih",
        "keepAwakeTitle": "Cegah Mac Tidur",
        "keepAwakeActiveIconLight": "Lampu",
        "clamshellOnCaption": "Mode tidur benar-benar dinonaktifkan. Perhatikan konsumsi daya.",
    },
    "NotchAgentStrings.swift": {
        "cardsHint": "Seret untuk mengubah urutan. Grafik memakai seluruh lebar panel.",
        "mostUsed": "Paling sering digunakan",
    },
    "ScreenshotStrings.swift": {"saveAsButton": "Simpan sebagai…"},
    "RecorderStrings.swift": {"saveAsButton": "Simpan sebagai…"},
    "CommandBarStrings.swift": {"selectionSearch": "Gunakan untuk pencarian"},
    "WindowMaximizerExclusionStrings.swift": {"addButton": "Tambah aplikasi…"},
    "WindowPreviewExclusionStrings.swift": {"addButton": "Tambah aplikasi…"},
    "ClipboardIgnoredAppsStrings.swift": {"addButton": "Tambah aplikasi…"},
    "MouseExceptionStrings.swift": {"addButton": "Tambah aplikasi…"},
    "AutoQuitDockAppsStrings.swift": {"addButton": "Tambah aplikasi…"},
    "SwitcherAppRulesStrings.swift": {"addButton": "Tambah aplikasi…"},
}
count = 0
for filename, overrides in OVERRIDES.items():
    path = ROOT / filename
    text = path.read_text()
    token = "static let id ="
    if token not in text:
        raise ValueError(f"No Indonesian table in {filename}")
    front, rest = text.split(token, 1)
    block, boundary, tail = rest.partition("\n    static let ")
    for key, value in overrides.items():
        keypattern = re.compile(
            rf'(?m)^(\s+){re.escape(key)}: "(?:\\.|[^"\\])*"(,?)$'
        )
        match = keypattern.search(block)
        if not match:
            raise ValueError(f"Key {key} missing in {filename}")
        block = keypattern.sub(
            lambda m: f'{m.group(1)}{key}: "{value}"{m.group(2)}',
            block, count=1,
        )
        count += 1
    path.write_text(front + token + block + boundary + tail)
print(f"Verified {count} high-visibility Indonesian strings")
