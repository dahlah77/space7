#!/usr/bin/env python3
"""Report suspicious Indonesian machine translations for review."""
import re
from pathlib import Path
root = Path(__file__).resolve().parents[1] / "Sources/Vorssaint/Core"
pattern = re.compile(r'(?m)^\s+([A-Za-z][A-Za-z0-9_]*): "((?:\\.|[^"\\])*)",?$')
suspect = []
count = 0
for path in sorted(root.glob("*.swift")):
    data = path.read_text()
    if "static let id =" not in data:
        continue
    en = data.split("static let enUS =", 1)[1].split("static let id =", 1)[0]
    indo = data.split("static let id =", 1)[1].split("\n    static let ", 1)[0]
    en_map = dict(pattern.findall(en))
    id_map = dict(pattern.findall(indo))
    for key in en_map.keys() & id_map.keys():
        source = en_map[key]
        target = id_map[key]
        if not source:
            continue
        count += 1
        ratio = len(target) / max(len(source), 1)
        if ratio > 2.15 or " in Texas" in target or " United States" in target or "City in" in target:
            suspect.append((ratio, path.name, key, source[:80], target[:115]))
print("total compared", count, "suspicious", len(suspect))
for item in sorted(suspect, reverse=True)[:90]:
    print("%.1f %s %s: %s => %s" % item)
