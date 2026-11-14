# Space7 — Windows Edition 0.2

Versi Windows native dari [Space7](https://github.com/dahlah77/space7), dirancang dengan acuan UI popup menu bar dan Settings pada Space7 macOS. Proyek macOS Swift/SwiftUI tetap dipertahankan; ini port C# Windows Forms yang terpisah, bukan menjalankan file macOS di Windows.

## Tampilan

- Panel gelap berukuran ringkas, logo Saturnus, delapan tab ikon (Daya, Mixer, Sistem, Jaringan, Disk, Energi, Utilitas, Kontrol)
- Kartu panel, tombol switch, grafik CPU/RAM, detail network/baterai, scroll vertikal
- Settings terpisah dengan search dan sidebar berkelompok, menyerupai struktur Settings asli
- Ikon tray Windows yang dapat menampilkan/menyembunyikan panel

## Fungsi aktif

- Tetap terjaga / jaga layar menyala memakai SetThreadExecutionState (kembali normal saat dimatikan/keluar)
- Master volume dan mute audio Windows
- Volume dan mute **per aplikasi** memakai antarmuka COM WASAPI/Audio Sessions (tidak membutuhkan NAudio atau driver tambahan)
- Ringkasan CPU, RAM, baterai, disk, network; tren CPU/RAM
- Tata letak jendela (kiri/kanan/atas/bawah/tengah/sudut) berdasarkan pemilihan jendela
- Pembersih parameter pelacak URL secara lokal
- Pembersihan manual file Temp pengguna yang berumur minimal 24 jam, selalu pilih + konfirmasi
- Manajer proses, daftar aplikasi, pengelola jendela, screenshot, dan clipboard manual (melalui jendela utilitas lanjutan)
- Pintasan Boot Camp Control Panel, Macs Fan Control, Windows audio, jaringan, daya dan pengaturan terkait

## Belum setara macOS 1:1

Notch/Dynamic Island, Dock Preview, keyboard hooking/switcher khusus macOS, perekam layar & editor video tingkat lanjut, Homebrew asli, shelf drag-and-drop, fitur media lengkap dan fan-control berbasis SMC langsung belum diport. Fitur seperti ini **tidak diklaim berfungsi**. Jika memungkinkan, Space7 memanggil alternatif sistem bawaan Windows.

## Build

Perangkat target Windows 10/11 x64. Dibuat dengan compiler C# .NET Framework 4.x yang tersedia di:

    C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe

Semua source ada di folder ini; tanpa modul pihak ketiga. Jalankan PowerShell:

    powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Install

Hasilnya dipasang pada %LOCALAPPDATA%\Programs\Space7 tanpa hak administrator; shortcut dibuat di Desktop dan Start Menu.

Untuk build saja:

    powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1

Uji dasar:

    powershell -NoProfile -STA -ExecutionPolicy Bypass -File .\Test-Space7.ps1

## Privasi dan batasan

Tidak menyimpan histori clipboard, meng-upload data atau menghapus file/proses otomatis. Pemilihan jendela, pembersihan, dan terminasi proses memerlukan tindakan pengguna. Volume per aplikasi hanya menjangkau sesi audio Windows yang aktif di output default. Kecepatan kipas tidak diubah oleh Space7.

## Lisensi

GPL-3.0-or-later. Menghormati kredit dan lisensi asal Space7/Vorssaint. Ini port independen, bukan rilis resmi dari pengembang Vorssaint.
