#!/usr/bin/env python3
"""Rebuild Indonesian catalogs cleanly from upstream plus translated values.

This fixes overlapping code blocks produced by an earlier generator; the only
source of original code is the immediate upstream parent commit in this fork.
"""
import re
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CORE = ROOT / "Sources/Vorssaint/Core"
START = re.compile(r'(?m)^( {4})static let enUS\s*=\s*([A-Za-z_][A-Za-z_0-9]*)\(')
ID_START = re.compile(r'(?m)^( {4})static let id\s*=\s*([A-Za-z_][A-Za-z_0-9]*)\(')
LITERAL = re.compile(r'"(?:\\.|[^"\\])*"')
SKIP = ("https://", "http://", "©", "Copyright", "GPL-3", "source repository")

def end_of_initializer(text, start):
    level = 1
    quote = False
    escape = False
    comment = False
    for i in range(start, len(text)):
        c = text[i]
        if comment:
            if c == "\n":
                comment = False
            continue
        if quote:
            if escape:
                escape = False
            elif c == "\\":
                escape = True
            elif c == '"':
                quote = False
            continue
        if c == '"':
            quote = True
        elif text[i:i+2] == "//":
            comment = True
        elif c == "(":
            level += 1
        elif c == ")":
            level -= 1
            if level == 0:
                return i + 1
    raise ValueError("Unclosed initializer")

def rebrand(content):
    def change(m):
        val = m.group()
        if any(x in val for x in SKIP):
            return val
        return val.replace("Vorssaint", "Space7")
    return "\n".join(
        LITERAL.sub(change, line) if not line.lstrip().startswith(("//", "/*", "*")) else line
        for line in content.split("\n")
    )

def original_file(path):
    relative = path.relative_to(ROOT).as_posix()
    return subprocess.check_output(["git", "-C", str(ROOT), "show", "HEAD^:" + relative], text=True)

counts = []
for path in sorted(CORE.glob("*.swift")):
    if path.name == "Localization.swift":
        continue
    current = path.read_text()
    if "static let id =" not in current:
        continue
    prior = original_file(path)
    eng_matches = list(START.finditer(prior))
    if not eng_matches:
        raise ValueError(f"Missing source English table in {path.name}")
    translated = {}
    for m in ID_START.finditer(current):
        end = end_of_initializer(current, m.end())
        translated.setdefault(m.group(2), current[m.start():end])
    replacements = []
    for match in eng_matches:
        end = end_of_initializer(prior, match.end())
        original_block = prior[match.start():end]
        cls = match.group(2)
        id_block = translated.get(cls, original_block.replace("static let enUS", "static let id", 1))
        # An ID block can have been generated with the wrong indentation only
        # if a previous parser also cloned an extension; prevent that here.
        if "static let enUS" in id_block or "extension " in id_block:
            raise ValueError(f"Unexpected extra source in {path.name} / {cls}")
        replacements.append((end, "\n\n" + id_block))
    for point, insertion in reversed(replacements):
        prior = prior[:point] + insertion + prior[point:]
    prior = prior.replace("case .enUS: return .enUS",
                          "case .enUS: return .enUS\n        case .id: return .id")
    prior = re.sub(r'case \.enUS(?=:|,(?!\s*\.id))', 'case .enUS, .id', prior)
    # Revert the accidental merging for the paired locale-specific return.
    prior = prior.replace("case .enUS, .id: return .enUS", "case .enUS: return .enUS")
    # Every paired ID case remains separate and exhaustively handled.
    path.write_text(rebrand(prior))
    counts.append((path.name, len(eng_matches)))
print(f"Rebuilt {len(counts)} catalogs with {sum(n for _,n in counts)} English tables")
print("Multiple-table files:", [name for name,n in counts if n>1])
