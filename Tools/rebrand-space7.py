#!/usr/bin/env python3
"""Rename user-facing text literals in the Space7 fork without touching Swift symbols.

The upstream author, GPL notices, and source links remain unchanged.
"""
import re
from pathlib import Path

root = Path(__file__).resolve().parents[1] / "Sources/Vorssaint"
literal = re.compile(r'"(?:\\.|[^"\\])*"')
exclusions = ("https://", "http://", "©", "Copyright", "GPL-3", "source repository")
total = 0
for path in root.rglob("*.swift"):
    source = path.read_text()
    def change(m):
        global total
        value = m.group()
        if "Vorssaint" not in value or any(x in value for x in exclusions):
            return value
        total += 1
        return value.replace("Vorssaint", "Space7")
    updated = "\n".join(
        literal.sub(change, line) if not line.lstrip().startswith(("//", "/*", "*")) else line
        for line in source.split("\n")
    )
    if updated != source:
        path.write_text(updated)
print(f"Updated {total} UI/text literals to Space7")
