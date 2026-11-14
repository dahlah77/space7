# Space7

**Space7** adalah fork aplikasi utilitas macOS berbasis [Vorssaint](https://github.com/vorssaint/vorssaint-utils), dengan identitas baru, logo S7, dan antarmuka Bahasa Indonesia.

> Proyek independen, bukan rilis resmi atau produk yang didukung tim Vorssaint. Kode turunan tetap mengikuti lisensi [GPL-3.0-or-later](LICENSE); hak merek dan atribusi pengembang asal tetap dihormati. Dokumentasi asli tersimpan di [UPSTREAM_README.md](docs/UPSTREAM_README.md).

## Kompatibilitas

- macOS 14 atau lebih baru; aplikasi desktop native Swift/SwiftUI.
- Build mengikuti CPU Mac yang digunakan (Intel x86_64 atau Apple Silicon arm64).
- Varian Intel diuji pada Mac Intel dengan macOS 26.7.2.
- Sejumlah fitur yang bergantung pada hardware atau perizinan khusus dapat berbeda antar-Mac, terutama kontrol kipas dan fungsi yang awalnya hanya diuji untuk Apple Silicon.

## Fungsi

Monitor sistem, mixer audio per aplikasi, pengelola jendela/Dock, utilitas keyboard dan mouse, clipboard, pengelolaan file, pengelola aplikasi, pembersihan manual, rekam layar, dan fitur lainnya dari kode asal.

Pembersihan dan penghapusan **tidak berjalan otomatis hanya karena aplikasi dipasang**. Setiap tindakan tetap perlu dipilih oleh pengguna.

## Instalasi dari source

Memerlukan Xcode/Apple Command Line Tools dan akses ke source:

~~~bash
git clone https://github.com/dahlah77/space7.git
cd space7
./build.sh --install
~~~

Aplikasi dibuat sebagai /Applications/Space7.app, identitas bundle com.space7.utils. Installer menggunakan identitas penandatanganan lokal **Space7 Local Signing** yang terpisah, sehingga tidak menimpa aplikasi Vorssaint. Pada instalasi pertama, macOS dapat meminta izin Aksesibilitas, Perekaman Layar, Mikrofon, dan izin lain sesuai fitur yang digunakan.

Untuk menghasilkan bundle tanpa memasang ke Applications:

~~~bash
./build.sh
~~~

## Bahasa

Bahasa Indonesia menjadi pilihan awal. Katalog UI utama diterjemahkan secara lokal menggunakan model OPUS-MT Inggris–Indonesia; label utama sudah diperiksa manual. Beberapa teks spesialis yang belum punya terjemahan khusus mungkin masih tampil dalam Bahasa Inggris. Koreksi terjemahan dapat dilakukan di katalog static let id pada source Swift.

## Pembaruan dan keamanan

- Mekanisme pembaruan otomatis aplikasi utama dari repo Vorssaint **dinonaktifkan** untuk Space7 agar tidak memasang build upstream ke aplikasi fork.
- Pembaruan Space7 dilakukan dengan membangun ulang source yang sudah diperiksa.
- Migrasi nama/instalasi lama dari aplikasi Vorssaint dinonaktifkan.
- Sebelum memakai kontrol kipas beta pada Intel, pastikan perangkat dan sensor didukung.
- Tidak ada kredensial, model terjemahan, atau virtual environment lokal yang masuk ke repository.

## Lisensi dan atribusi

Copyright sumber asli tetap milik kontributor Vorssaint, sesuai [LICENSE](LICENSE) dan [TRADEMARKS.md](TRADEMARKS.md). Dokumentasi dan source asal: [vorssaint/vorssaint-utils](https://github.com/vorssaint/vorssaint-utils).

Model bahasa untuk proses penerjemahan: OPUS-MT, Jörg Tiedemann dan Santhosh Thottingal, CC BY 4.0. Model tidak dibundel dengan aplikasi.
