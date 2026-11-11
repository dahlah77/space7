#!/usr/bin/env python3
"""Generate Indonesian UI string catalogs for the Space7 macOS fork.

Translation runs locally with the open-source Argos English->Indonesian model.
The original English and other language catalogs stay intact.
"""
from __future__ import annotations

import json
import os
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CORE = ROOT / "Sources/Vorssaint/Core"
MODEL = ROOT / ".translation-en-id.argosmodel"
CACHE = ROOT / "Tools/Space7-translation-cache.json"
ENTRY = re.compile(r'^(\s+)([A-Za-z][A-Za-z0-9_]*): "((?:\\.|[^"\\])*)"(,?)$', re.M)
START = re.compile(r'(?m)^( {4})static let enUS\s*=\s*([A-Za-z_][A-Za-z_0-9]*)\(')
END = re.compile(r'(?m)^ {4}\)$')
KEEP = re.compile(r'%(?:\d+\$)?[+\-#0-9.]*[a-zA-Z@]|\\[nrt"\\()]|https?://|\$\{|\{[a-z_]+\}', re.I)

GLOSSARY = {
    "Settings": "Pengaturan", "Settings…": "Pengaturan…",
    "Features": "Fitur", "Permissions": "Izin",
    "Quit": "Keluar", "Close": "Tutup", "Cancel": "Batal",
    "Save": "Simpan", "Done": "Selesai", "Back": "Kembali",
    "Continue": "Lanjutkan", "Next": "Berikutnya",
    "Search": "Cari", "Clear": "Bersihkan", "Remove": "Hapus",
    "Delete": "Hapus", "Edit": "Edit", "Copy": "Salin",
    "Paste": "Tempel", "Cut": "Potong", "Select All": "Pilih Semua",
    "Show All": "Tampilkan Semua", "Hide Others": "Sembunyikan Lainnya",
    "Undo": "Urungkan", "Redo": "Ulangi", "Window": "Jendela",
    "Minimize": "Minimalkan", "Zoom": "Perbesar", "Play": "Putar",
    "Pause": "Jeda", "Mute": "Bisukan", "Unmute": "Aktifkan Suara",
    "System": "Sistem", "Memory": "Memori", "Battery": "Baterai",
    "Network": "Jaringan", "Options": "Opsi", "Start": "Mulai",
    "Interval": "Interval", "Duration": "Durasi", "Install": "Pasang",
    "Uninstall": "Copot", "Yes": "Ya", "No": "Tidak",
    "On": "Aktif", "Off": "Nonaktif", "Open": "Buka",
    "Refresh": "Segarkan", "Retry": "Coba Lagi",
    "General": "Umum", "Appearance": "Tampilan",
    "Utilities": "Utilitas", "Keyboard": "Papan Ketik",
    "Mouse": "Tetikus", "Clipboard": "Papan Klip",
    "Updates": "Pembaruan", "About": "Tentang",
    "Normal": "Normal", "Caution": "Waspada",
    "Minutes": "Menit", "Hours": "Jam",
}
cache = json.loads(CACHE.read_text()) if CACHE.exists() else {}
translated = 0
fallback = 0

def translate(source: str) -> str:
    global translated, fallback
    if not source or not re.search(r'[A-Za-z]', source):
        return source
    if source in GLOSSARY:
        return GLOSSARY[source]
    if source in cache:
        return cache[source]
    # Keep placeholders, format specifiers, escape sequences and dynamic Swift
    # interpolation exact. Do not guess a translated format contract.
    if KEEP.search(source):
        fallback += 1
        return source.replace("Vorssaint", "Space7")
    try:
        target = translator.translate(source.replace("Vorssaint", "Space7"))
        if not target or len(target) > max(1000, 4 * len(source)):
            raise ValueError("suspicious output")
        target = target.replace("\r", " ").replace("\n", " ").strip()
        cache[source] = target
        translated += 1
        if translated % 100 == 0:
            print(f"translated {translated} distinct strings", flush=True)
            CACHE.write_text(json.dumps(cache, ensure_ascii=False, indent=2) + "\n")
        return target
    except Exception as exc:
        fallback += 1
        print(f"translation fallback for {source[:45]!r}: {exc}", flush=True)
        return source.replace("Vorssaint", "Space7")

def escape_swift(s: str) -> str:
    return s.replace('\\', '\\\\').replace('"', '\\"')

def localized_block(block: str) -> str:
    def replace(m: re.Match) -> str:
        indent, key, raw, comma = m.groups()
        # Preserve Swift source escapes (including escaped newlines) safely.
        if '\\' in raw:
            target = raw.replace("Vorssaint", "Space7")
            return f'{indent}{key}: "{target}"{comma}'
        target = translate(raw)
        return f'{indent}{key}: "{escape_swift(target)}"{comma}'
    return ENTRY.sub(replace, block)

def initializer_end(text: str, start: int) -> int:
    """Return the character just after the matching initializer ')'."""
    level = 1
    inside = False
    escaped = False
    line_comment = False
    for index in range(start, len(text)):
        char = text[index]
        if line_comment:
            if char == "\n":
                line_comment = False
            continue
        if inside:
            if escaped:
                escaped = False
            elif char == "\\":
                escaped = True
            elif char == '"':
                inside = False
            continue
        if char == '"' :
            inside = True
        elif char == "/" and text[index:index + 2] == "//":
            line_comment = True
        elif char == "(":
            level += 1
        elif char == ")":
            level -= 1
            if level == 0:
                return index + 1
    raise RuntimeError("Unclosed Swift initializer")

def main() -> None:
    global translator
    if not MODEL.exists():
        raise SystemExit(f"Missing local translation model: {MODEL}")
    import ctranslate2
    from subword_nmt.apply_bpe import BPE
    from sacremoses import MosesTokenizer, MosesDetokenizer
    model_dir = ROOT / ".translation-model/translate-en_id-1_9"
    with (model_dir / "bpe.model").open() as model_file:
        bpe = BPE(model_file)
    tokenizer = MosesTokenizer(lang="en")
    detokenizer = MosesDetokenizer(lang="id")
    model = ctranslate2.Translator(
        str(model_dir / "model"), device="cpu", inter_threads=1, intra_threads=2
    )
    class LocalTranslator:
        def translate(self, value: str) -> str:
            pieces = bpe.process_line(tokenizer.tokenize(value, return_str=True)).split()
            result = model.translate_batch([pieces], beam_size=3)[0]
            joined = " ".join(result.hypotheses[0]).replace("@@ ", "").replace("</s>", "")
            return detokenizer.detokenize(joined.split())
    translator = LocalTranslator()

    files = sorted((ROOT / "Sources/Vorssaint").rglob("*.swift"))
    tables = 0
    selectors = 0
    for path in files:
        data = path.read_text()
        if "case .enUS" not in data and "static let enUS" not in data:
            continue
        if path.name == "Localization.swift":
            continue
        match = START.search(data)
        if match and "static let id =" not in data:
            close = initializer_end(data, match.end())
            entire = data[match.start():close]
            cloned = localized_block(entire.replace("static let enUS", "static let id", 1))
            data = data[:close] + "\n\n" + cloned + data[close:]
            tables += 1
            selector = "case .enUS: return .enUS"
            if selector in data:
                data = data.replace(selector, "case .enUS: return .enUS\n        case .id: return .id")
                selectors += 1
        if "static let id =" in data:
            # Repair selectors created by the first interrupted translation pass.
            data = data.replace("case .enUS, .id: return .enUS", "case .enUS: return .enUS")
        else:
            # Specialist catalogs without an Indonesian table fall back safely.
            data = re.sub(r'case \.enUS(?=:|,(?!\s*\.id))', 'case .enUS, .id', data)
        path.write_text(data)
    # The primary catalog has a second group of language-specific files.
    path = CORE / "Localization.swift"
    data = path.read_text()
    match = START.search(data)
    if not match:
        raise RuntimeError("Main English catalog missing")
    if "static let id = Strings(" not in data:
        close = initializer_end(data, match.end())
        entire = data[match.start():close]
        cloned = localized_block(entire.replace("static let enUS", "static let id", 1))
        data = data[:close] + "\n\n" + cloned + data[close:]
    data = data.replace('    case enUS = "en-US"', '    case id = "id"\n    case enUS = "en-US"', 1)
    data = data.replace('        case .enUS: return "English (US)"',
                        '        case .id: return "Bahasa Indonesia"\n        case .enUS: return "English (US)"', 1)
    data = data.replace('            language = .systemDefault', '            language = .id', 1)
    data = data.replace('        case .enUS: return .enUS',
                        '        case .id: return .id\n        case .enUS: return .enUS', 1)
    # Additional language switches, e.g. a count pluralization helper, have
    # default branches; do not add redundant cases.
    path.write_text(data)
    tables += 1
    cache_path = CACHE
    cache_path.write_text(json.dumps(cache, ensure_ascii=False, indent=2) + "\n")
    print(f"COMPLETE catalogs={tables} selectors={selectors} machine_translated={translated} placeholders_unchanged={fallback}", flush=True)

if __name__ == "__main__":
    main()
