// Space7 Windows Edition (GPL-3.0-or-later)
// Windows-native companion to the open-source macOS project.
// Non-destructive by default. No automatic cleaning, termination, or clipboard persistence.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Space7Windows {
    internal static class Program {
        [STAThread] public static void Main(string[] args) {
            if (args.Any(a => a == "--selftest")) { Console.WriteLine(SelfTest()); return; }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (args.Any(a => a == "--settings-preview")) { var p=new MacPanel(); p.Hide(); Application.Run(new Settings7(p)); return; }
            Application.Run(new MacPanel());
        }
        public static string SelfTest() {
            try {
                int count = Process.GetProcesses().Length;
                string os = Environment.OSVersion.VersionString;
                string temp = Path.GetTempPath();
                bool safe = Directory.Exists(temp);
                return "SPACE7_SELFTEST=PASS\nOS=" + os + "\nPROCESSES=" + count + "\nTEMP_EXISTS=" + safe + "\nWINFO_RM=" + Environment.Is64BitOperatingSystem;
            } catch(Exception e) { return "SPACE7_SELFTEST=FAIL "+e.Message; }
        }
    }
    public class MainWindow : Form {
        static readonly Color Bg=Color.FromArgb(24,24,24), Side=Color.FromArgb(34,34,34), PanelBg=Color.FromArgb(43,43,43), Elevated=Color.FromArgb(59,59,59), Cyan=Color.FromArgb(0,122,255), Light=Color.FromArgb(236,236,237), Muted=Color.FromArgb(160,160,160), Red=Color.FromArgb(255,116,128);
        Panel sidebar, main, top;
        FlowLayoutPanel page;
        Label headline, smallline, cpuLabel, ramLabel, diskLabel, uptimeLabel, scanInfo;
        readonly Dictionary<string,Button> navigation=new Dictionary<string,Button>();
        readonly Timer refresh=new Timer();
        PerformanceCounter cpuCounter;
        DataGridView processGrid, appGrid, windowGrid;
        CheckedListBox cleanupList;
        readonly List<CleanEntry> cleanupEntries=new List<CleanEntry>();
        TextBox clipboardBox;
        string currentPage="Beranda";
        readonly DateTime openedAt=DateTime.Now;
        public MainWindow() {
            Text="Space7 • Windows Edition"; StartPosition=FormStartPosition.CenterScreen;
            ClientSize=new Size(1180,735); MinimumSize=new Size(930,620); 
            BackColor=Bg; ForeColor=Light; Font=new Font("Segoe UI",10f); AutoScaleMode=AutoScaleMode.Dpi;
            try { Icon=MakeIcon(); } catch {}
            BuildShell(); Navigate("Beranda");
            refresh.Interval=3500;refresh.Tick+=(s,e)=>{if(currentPage=="Beranda") UpdateStats();}; refresh.Start();
            FormClosing+=(s,e)=>{ if(cpuCounter!=null) cpuCounter.Dispose(); refresh.Dispose(); };
        }
        Icon MakeIcon() {
            Bitmap b=new Bitmap(64,64);
            using(Graphics g=Graphics.FromImage(b)) {g.Clear(Bg); using(Pen p=new Pen(Cyan,6)) {g.DrawArc(p,10,9,45,45,210,280);} using(Font f=new Font("Segoe UI",19,FontStyle.Bold)){g.DrawString("7",f,Brushes.White,19,15);} }
            IntPtr h=b.GetHicon(); Icon ic=(Icon)Icon.FromHandle(h).Clone();DestroyIcon(h);b.Dispose();return ic;
        }
        [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr hIcon);
        [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hWnd,int nCmdShow);
        void BuildShell() {
            sidebar=new Panel {Dock=DockStyle.Left,Width=224,BackColor=Side};
            Controls.Add(sidebar);
            var brand=new Panel{Height=114,Dock=DockStyle.Top,BackColor=Side};sidebar.Controls.Add(brand);
            var logo=new Label {Text="S7",Location=new Point(20,23),Size=new Size(58,54),BackColor=Cyan,ForeColor=Bg,TextAlign=ContentAlignment.MiddleCenter,Font=new Font("Segoe UI",20,FontStyle.Bold)};brand.Controls.Add(logo);
            brand.Controls.Add(new Label{Text="SPACE7",ForeColor=Light,Location=new Point(91,27),Size=new Size(120,25),Font=new Font("Segoe UI",17,FontStyle.Bold)});
            brand.Controls.Add(new Label{Text="WINDOWS EDITION",ForeColor=Cyan,Location=new Point(92,58),Size=new Size(120,16),Font=new Font("Segoe UI",8.5f,FontStyle.Bold)});
            var nav=new FlowLayoutPanel {Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new Padding(11,16,8,5)};
            sidebar.Controls.Add(nav);nav.Dock=DockStyle.None;nav.Location=new Point(0,114);nav.Size=new Size(sidebar.Width,sidebar.Height-170);nav.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;nav.BringToFront();brand.BringToFront();
            string[] tabs={"Beranda","Proses","Pembersih","Aplikasi","Jendela","Clipboard","Peralatan","Tentang"};
            foreach(string t in tabs) {
                string label=t=="Beranda"?"◉  "+t:t=="Proses"?"▤  "+t:t=="Pembersih"?"✦  "+t:t=="Aplikasi"?"▦  "+t:t=="Jendela"?"▣  "+t:t=="Clipboard"?"▤  "+t:t=="Peralatan"?"⚙  "+t:"ⓘ  "+t;
                Button b=new Button {Text=label,Size=new Size(188,46),FlatStyle=FlatStyle.Flat,TextAlign=ContentAlignment.MiddleLeft,Padding=new Padding(14,0,0,0),BackColor=Side,ForeColor=Muted,Margin=new Padding(0,0,0,6),Cursor=Cursors.Hand,Font=new Font("Segoe UI",10,FontStyle.Regular)};
                b.FlatAppearance.BorderSize=0;b.FlatAppearance.MouseOverBackColor=Elevated;string tab=t;b.Click+=(s,e)=>Navigate(tab);nav.Controls.Add(b);navigation[t]=b;
            }
            var foot=new Label{Dock=DockStyle.Bottom,Height=56,Text="●  Native Windows\n     v0.1 • Preview",ForeColor=Muted,Font=new Font("Segoe UI",8.6f),Padding=new Padding(24,9,0,0)};
            sidebar.Controls.Add(foot);foot.BringToFront();
            main=new Panel{Dock=DockStyle.Fill,BackColor=Bg};Controls.Add(main);main.BringToFront();
            top=new Panel{Dock=DockStyle.Top,Height=93,BackColor=Bg};main.Controls.Add(top);
            headline=new Label{Text="Beranda",Location=new Point(30,16),Size=new Size(720,38),ForeColor=Light,Font=new Font("Segoe UI",23,FontStyle.Bold)};top.Controls.Add(headline);
            smallline=new Label{Text="Ringkasan perangkat Windows",Location=new Point(33,57),Size=new Size(730,24),ForeColor=Muted,Font=new Font("Segoe UI",9.5f)};top.Controls.Add(smallline);
            var rule=new Panel{Dock=DockStyle.Bottom,Height=1,BackColor=Elevated};top.Controls.Add(rule);
            page=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new Padding(30,25,20,25),BackColor=Bg};
            main.Controls.Add(page);page.Dock=DockStyle.None;page.Location=new Point(0,93);page.Size=new Size(main.Width,main.Height-93);page.Anchor=AnchorStyles.Left|AnchorStyles.Top|AnchorStyles.Right|AnchorStyles.Bottom;page.BringToFront();top.BringToFront();
            page.SizeChanged+=(s,e)=>LayoutBlocks();
        }
        Label Label(string text,int size=10,bool bold=false,Color? color=null) {
            return new Label{Text=text,ForeColor=color??Light,Font=new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular),AutoSize=false,TextAlign=ContentAlignment.MiddleLeft};
        }
        Button Action(string text,EventHandler onClick,bool primary=false,int width=148) {
            Button b=new Button{Text=text,Size=new Size(width,38),BackColor=primary?Cyan:Elevated,ForeColor=primary?Bg:Light,Font=new Font("Segoe UI",9.1f,FontStyle.Bold),FlatStyle=FlatStyle.Flat,Cursor=Cursors.Hand,Margin=new Padding(0,0,10,6)};
            b.FlatAppearance.BorderSize=0;b.FlatAppearance.MouseOverBackColor=primary?Color.FromArgb(140,244,255):Color.FromArgb(44,64,78);
            b.Click+=onClick;return b;
        }
        Panel Block(int height) {
            Panel p=new Panel{Height=height,Width=Math.Max(440,page.ClientSize.Width-68),BackColor=PanelBg,Margin=new Padding(0,0,0,17),Padding=new Padding(18)};
            page.Controls.Add(p);return p;
        }
        void LayoutBlocks() {
            int width=Math.Max(440,page.ClientSize.Width-68);
            foreach(Control c in page.Controls) if(c is Panel) c.Width=width;
        }
        FlowLayoutPanel Actions(Panel parent,int topY) {
            var f=new FlowLayoutPanel{Location=new Point(18,topY),Size=new Size(parent.Width-36,46),Anchor=AnchorStyles.Left|AnchorStyles.Right|AnchorStyles.Top,WrapContents=false};
            parent.Controls.Add(f);return f;
        }
        void Clear() {
            page.SuspendLayout();page.Controls.Clear();page.ResumeLayout();
            processGrid=null;appGrid=null;windowGrid=null;cleanupList=null;clipboardBox=null;
        }
        void Navigate(string title) {
            currentPage=title;headline.Text=title;
            foreach(var p in navigation) {p.Value.BackColor=(p.Key==title)?Elevated:Side;p.Value.ForeColor=(p.Key==title)?Cyan:Muted;}
            Clear();
            if(title=="Beranda") Home();
            else if(title=="Proses") Processes();
            else if(title=="Pembersih") Cleaner();
            else if(title=="Aplikasi") Applications();
            else if(title=="Jendela") Windows();
            else if(title=="Clipboard") ClipboardPage();
            else if(title=="Peralatan") Tools();
            else About();
            LayoutBlocks();
        }
        void Section(Panel p,string title,string subtitle="") {
            var t=Label(title,13,true);t.Location=new Point(20,12);t.Size=new Size(p.Width-40,29);t.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;p.Controls.Add(t);
            if(subtitle!="") {var l=Label(subtitle,9,false,Muted);l.Location=new Point(20,41);l.Size=new Size(p.Width-40,28);l.Anchor=t.Anchor;p.Controls.Add(l);}
        }
        Panel Card(Panel owner,string caption,int idx) {
            var c=new Panel{BackColor=Elevated,Height=102,Width=184,Location=new Point(20+idx*197,76),Anchor=AnchorStyles.Left|AnchorStyles.Top};
            c.Controls.Add(new Label{Text=caption,ForeColor=Muted,Location=new Point(14,11),Size=new Size(155,22),Font=new Font("Segoe UI",9)});
            owner.Controls.Add(c);return c;
        }
        Label CardValue(Panel p) {
            var l=new Label{Text="—",ForeColor=Cyan,Location=new Point(14,40),Size=new Size(166,48),Font=new Font("Segoe UI",22,FontStyle.Bold),TextAlign=ContentAlignment.MiddleLeft};p.Controls.Add(l);return l;
        }
        void Home() {
            smallline.Text="Monitor perangkat dan akses cepat untuk MacBook di Boot Camp.";
            Panel p=Block(206);Section(p,"Status perangkat","Pembacaan CPU, RAM, penyimpanan dan waktu aktif Windows.");
            cpuLabel=CardValue(Card(p,"CPU",0));ramLabel=CardValue(Card(p,"RAM",1));diskLabel=CardValue(Card(p,"DISK C:",2));uptimeLabel=CardValue(Card(p,"UPTIME",3));
            var quick=Block(162);Section(quick,"Akses cepat","Semua tindakan dijalankan hanya setelah BOS memilih.");
            var action=Actions(quick,82);
            action.Controls.Add(Action("Boot Camp",(s,e)=>LaunchBootCamp(),true,145));
            action.Controls.Add(Action("Kontrol kipas",(s,e)=>LaunchFan(),false,150));
            action.Controls.Add(Action("Task Manager",(s,e)=>SafeRun("taskmgr.exe"),false,150));
            action.Controls.Add(Action("Pembersihan",(s,e)=>Navigate("Pembersih"),false,150));
            var info=Block(126);Section(info,"Proteksi dan privasi","Tidak ada pembersihan otomatis, tidak membaca clipboard tanpa diminta,\ndan tidak mengakhiri proses tanpa konfirmasi.");
            var n=Label("Space7 Windows adalah implementasi native awal, bukan seluruh fungsi macOS.",9,false,Muted);n.Location=new Point(20,80);n.Size=new Size(info.Width-40,25);n.Anchor=AnchorStyles.Left|AnchorStyles.Right;info.Controls.Add(n);
            try{cpuCounter=new PerformanceCounter("Processor","% Processor Time","_Total");cpuCounter.NextValue();}catch{}
            UpdateStats();
        }
        void UpdateStats() {
            try { if(cpuLabel!=null) cpuLabel.Text=cpuCounter==null?"N/A":Math.Round(cpuCounter.NextValue()).ToString()+"%";}catch { if(cpuLabel!=null) cpuLabel.Text="N/A";}
            try {
                using(var s=new ManagementObjectSearcher("SELECT TotalVisibleMemorySize,FreePhysicalMemory FROM Win32_OperatingSystem"))
                foreach(ManagementObject o in s.Get()) {
                    double total=Convert.ToDouble(o["TotalVisibleMemorySize"]),free=Convert.ToDouble(o["FreePhysicalMemory"]);
                    if(ramLabel!=null) ramLabel.Text=Math.Round((total-free)/total*100).ToString()+"%";
                }
            } catch {if(ramLabel!=null)ramLabel.Text="N/A";}
            try {DriveInfo d=new DriveInfo("C");if(diskLabel!=null) diskLabel.Text=Math.Round((double)(d.TotalSize-d.AvailableFreeSpace)/d.TotalSize*100).ToString()+"%";}catch{}
            if(uptimeLabel!=null) { TimeSpan t=TimeSpan.FromMilliseconds(Environment.TickCount & Int32.MaxValue);uptimeLabel.Text=t.Days+"h "+t.Hours+"j"; }
        }
        DataGridView Grid(Panel p,int y) {
            DataGridView d=new DataGridView{Location=new Point(18,y),Size=new Size(p.Width-36,p.Height-y-16),Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right,BackgroundColor=PanelBg,GridColor=Elevated,BorderStyle=BorderStyle.None,ColumnHeadersBorderStyle=DataGridViewHeaderBorderStyle.None,CellBorderStyle=DataGridViewCellBorderStyle.SingleHorizontal,RowHeadersVisible=false,AllowUserToAddRows=false,AllowUserToDeleteRows=false,ReadOnly=true,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,EnableHeadersVisualStyles=false,RowTemplate={Height=32}};
            d.ColumnHeadersDefaultCellStyle.BackColor=Elevated;d.ColumnHeadersDefaultCellStyle.ForeColor=Cyan;d.ColumnHeadersDefaultCellStyle.Font=new Font("Segoe UI",9,FontStyle.Bold);d.ColumnHeadersHeight=37;
            d.DefaultCellStyle.BackColor=PanelBg;d.DefaultCellStyle.ForeColor=Light;d.DefaultCellStyle.SelectionBackColor=Color.FromArgb(32,91,106);d.DefaultCellStyle.SelectionForeColor=Color.White;d.AlternatingRowsDefaultCellStyle.BackColor=Color.FromArgb(23,37,51);d.DefaultCellStyle.Font=new Font("Segoe UI",9);d.ScrollBars=ScrollBars.Both;p.Controls.Add(d);return d;
        }
        void Processes() {
            smallline.Text="Inspeksi proses aktif. Tutup proses hanya jika benar-benar diperlukan.";
            Panel p=Block(540);Section(p,"Manajer proses","Urutkan proses berdasarkan nama, PID, atau penggunaan RAM.");
            var a=Actions(p,74);a.Controls.Add(Action("Perbarui",(s,e)=>LoadProcesses(),true));
            a.Controls.Add(Action("Akhiri terpilih",(s,e)=>EndProcess(),false,150));
            a.Controls.Add(Action("Task Manager",(s,e)=>SafeRun("taskmgr.exe"),false,146));
            processGrid=Grid(p,133);processGrid.Columns.Add("name","Proses");processGrid.Columns.Add("pid","PID");processGrid.Columns.Add("memory","RAM (MB)");
            LoadProcesses();
        }
        void LoadProcesses() {
            if(processGrid==null) return;
            var rows=new List<object[]>();foreach(var proc in Process.GetProcesses()) { try{rows.Add(new object[]{proc.ProcessName,proc.Id,Math.Round(proc.WorkingSet64/1048576d,1)});}catch{}finally{proc.Dispose();}}
            processGrid.Rows.Clear();foreach(var r in rows.OrderByDescending(x=>Convert.ToDouble(x[2])).Take(450)) processGrid.Rows.Add(r);
        }
        void EndProcess() {
            if(processGrid==null||processGrid.SelectedRows.Count==0){MessageBox.Show("Pilih satu proses.","Space7");return;}
            string name=Convert.ToString(processGrid.SelectedRows[0].Cells[0].Value);int pid=Convert.ToInt32(processGrid.SelectedRows[0].Cells[1].Value);
            string[] protectedNames={"System","Idle","Registry","smss","csrss","services","wininit","winlogon","lsass","dwm","explorer","BootCampService","AppleOSSMgr","MacsFanControl","Space7"};
            if(pid==Process.GetCurrentProcess().Id||protectedNames.Any(x=>String.Equals(x,name,StringComparison.OrdinalIgnoreCase))) {MessageBox.Show("Proses sistem/pengendali perangkat dilindungi. Gunakan Task Manager untuk pengelolaan lanjutan.","Space7");return;}
            if(MessageBox.Show("Akhiri "+name+" (PID "+pid+")?\nPekerjaan yang belum tersimpan dapat hilang.","Konfirmasi",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;
            try{using(Process proc=Process.GetProcessById(pid)){if(!String.Equals(proc.ProcessName,name,StringComparison.OrdinalIgnoreCase))throw new Exception("Identitas proses berubah.");proc.Kill();}LoadProcesses();}
            catch(Exception ex){Error(ex.Message);}
        }
        sealed class CleanEntry {public string Path,Label;public DateTime Modified;public bool Folder;public long Bytes;}
        void Cleaner() {
            smallline.Text="Pembersihan manual: lihat kandidat sebelum memilih file atau folder.";
            Panel p=Block(545);Section(p,"Pembersih file sementara","Hanya folder Temp milik pengguna; tidak menyentuh file sistem atau dokumen pribadi.");
            var a=Actions(p,74);a.Controls.Add(Action("Pindai ulang",(s,e)=>ScanTemp(),true));
            a.Controls.Add(Action("Hapus yang dipilih",(s,e)=>DeleteChecked(),false,181));
            a.Controls.Add(Action("Buka Temp",(s,e)=>SafeRun("explorer.exe",Path.GetTempPath()),false,130));
            cleanupList=new CheckedListBox{Location=new Point(18,135),Size=new Size(p.Width-36,340),Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right|AnchorStyles.Bottom,BackColor=Elevated,ForeColor=Light,BorderStyle=BorderStyle.None,Font=new Font("Consolas",9),CheckOnClick=true,HorizontalScrollbar=true};
            p.Controls.Add(cleanupList);scanInfo=Label("Belum dipindai.",9,false,Muted);scanInfo.Location=new Point(19,488);scanInfo.Size=new Size(p.Width-40,32);scanInfo.Anchor=AnchorStyles.Left|AnchorStyles.Right|AnchorStyles.Bottom;p.Controls.Add(scanInfo);
            ScanTemp();
        }
        void ScanTemp() {
            cleanupEntries.Clear();if(cleanupList==null)return;cleanupList.Items.Clear();
            string dir=Path.GetFullPath(Path.GetTempPath());int skipped=0;
            try{foreach(string x in Directory.GetFileSystemEntries(dir).Take(650)) {
                try{
                    FileAttributes attr=File.GetAttributes(x);
                    if((attr&FileAttributes.ReparsePoint)!=0){skipped++;continue;}
                    bool folder=(attr&FileAttributes.Directory)!=0;
                    DateTime modified=folder?Directory.GetLastWriteTime(x):File.GetLastWriteTime(x);
                    if(modified>DateTime.Now.AddDays(-1)){skipped++;continue;}
                    long bytes=folder?0:new FileInfo(x).Length;
                    var entry=new CleanEntry{Path=x,Folder=folder,Modified=modified,Bytes=bytes,Label=(folder?"[DIR] ":"[FILE] ")+Path.GetFileName(x)+"  ·  "+modified.ToString("dd/MM/yyyy")+(folder?"":" · "+FormatBytes(bytes))};
                    cleanupEntries.Add(entry);cleanupList.Items.Add(entry.Label,false);
                }catch{skipped++;}
            }} catch(Exception ex){Error(ex.Message);}
            scanInfo.Text=cleanupEntries.Count+" kandidat lama (24 jam+) • "+skipped+" baru/terkunci/diabaikan • Tidak ada yang dipilih otomatis.";
        }
        static string FormatBytes(long n){if(n>=1073741824) return (n/1073741824d).ToString("0.0")+" GB";if(n>=1048576)return (n/1048576d).ToString("0.0")+" MB";if(n>=1024)return (n/1024d).ToString("0.0")+" KB";return n+" B";}
        void DeleteChecked() {
            if(cleanupList==null||cleanupList.CheckedIndices.Count==0){MessageBox.Show("Centang dulu kandidat yang ingin dihapus.","Space7");return;}
            var list=new List<CleanEntry>();foreach(int i in cleanupList.CheckedIndices)list.Add(cleanupEntries[i]);
            if(MessageBox.Show("Hapus permanen "+list.Count+" item terpilih dari folder Temp?\nItem yang sedang digunakan akan dilewati.\nTidak ada tindakan pada item lain.","Konfirmasi pembersihan",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;
            int ok=0,fail=0;string tempRoot=Path.GetFullPath(Path.GetTempPath()).TrimEnd('\\')+"\\";
            foreach(CleanEntry x in list) {
                try {
                    string full=Path.GetFullPath(x.Path);
                    if(!full.StartsWith(tempRoot,StringComparison.OrdinalIgnoreCase)||full==tempRoot.TrimEnd('\\'))throw new IOException("Di luar Temp.");
                    if(x.Folder) {if((File.GetAttributes(full)&FileAttributes.ReparsePoint)!=0)throw new IOException("Tautan diabaikan."); Directory.Delete(full,true);}
                    else File.Delete(full);ok++;
                }catch{fail++;}
            }
            MessageBox.Show("Berhasil: "+ok+"\nDilewati/gagal: "+fail+"\nFile yang digunakan aplikasi bisa terkunci.","Hasil pembersihan");ScanTemp();
        }
        void Applications() {
            smallline.Text="Inventaris program terpasang melalui Registry Windows.";
            Panel p=Block(535);Section(p,"Aplikasi terinstal","Gunakan halaman Apps & Features Windows untuk menghapus aplikasi dengan aman.");
            var a=Actions(p,75);a.Controls.Add(Action("Perbarui",(s,e)=>LoadApps(),true));
            a.Controls.Add(Action("Apps & Features",(s,e)=>SafeRun("ms-settings:appsfeatures"),false,160));
            appGrid=Grid(p,133);appGrid.Columns.Add("title","Nama aplikasi");appGrid.Columns.Add("vendor","Penerbit");appGrid.Columns.Add("version","Versi");LoadApps();
        }
        void LoadApps() {
            if(appGrid==null)return;
            var items=new SortedDictionary<string,string[]>(StringComparer.OrdinalIgnoreCase);
            string[] paths={@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"};
            foreach(RegistryKey hive in new[]{Registry.LocalMachine,Registry.CurrentUser}) foreach(string path in paths) {
                try {using(RegistryKey k=hive.OpenSubKey(path)) {if(k==null)continue;foreach(string sub in k.GetSubKeyNames())try {
                    using(RegistryKey s=k.OpenSubKey(sub)){if(s==null)continue;string name=Convert.ToString(s.GetValue("DisplayName"));if(String.IsNullOrWhiteSpace(name))continue;string publisher=Convert.ToString(s.GetValue("Publisher"));string ver=Convert.ToString(s.GetValue("DisplayVersion"));items[name+"|"+ver]=new[]{name,publisher,ver};}
                }catch{}}}catch{}
            }
            appGrid.Rows.Clear();foreach(var item in items.Values.Take(900))appGrid.Rows.Add(item);
        }
        void Windows() {
            smallline.Text="Tampilkan atau minimalkan jendela aplikasi aktif.";
            Panel p=Block(535);Section(p,"Pengelola jendela","Menampilkan jendela yang memiliki judul dan belum ditutup.");
            var a=Actions(p,75);a.Controls.Add(Action("Perbarui",(s,e)=>LoadWindows(),true));
            a.Controls.Add(Action("Fokus jendela",(s,e)=>WindowAction(false),false,150));
            a.Controls.Add(Action("Minimalkan",(s,e)=>WindowAction(true),false,148));
            windowGrid=Grid(p,133);windowGrid.Columns.Add("title","Judul");windowGrid.Columns.Add("name","Aplikasi");windowGrid.Columns.Add("pid","PID");LoadWindows();
        }
        void LoadWindows(){
            if(windowGrid==null)return;windowGrid.Rows.Clear();
            foreach(Process proc in Process.GetProcesses())try {
                if(proc.MainWindowHandle!=IntPtr.Zero&&!String.IsNullOrWhiteSpace(proc.MainWindowTitle))windowGrid.Rows.Add(proc.MainWindowTitle,proc.ProcessName,proc.Id);
            }catch{}finally{proc.Dispose();}
        }
        void WindowAction(bool minimize) {
            if(windowGrid==null||windowGrid.SelectedRows.Count==0)return;
            int pid=Convert.ToInt32(windowGrid.SelectedRows[0].Cells[2].Value);
            try{using(Process proc=Process.GetProcessById(pid)){if(minimize)ShowWindow(proc.MainWindowHandle,6);else{ShowWindow(proc.MainWindowHandle,9);SetForegroundWindow(proc.MainWindowHandle);}}}catch(Exception ex){Error(ex.Message);}
        }
        void ClipboardPage() {
            smallline.Text="Clipboard tidak disimpan atau dibaca secara otomatis.";
            Panel p=Block(421);Section(p,"Papan klip","Klik Baca clipboard untuk menampilkan teks yang sedang tersimpan.");
            var a=Actions(p,77);a.Controls.Add(Action("Baca clipboard",(s,e)=>ReadClipboard(),true,155));
            a.Controls.Add(Action("Salin editor",(s,e)=>CopyClipboard(),false,140));
            a.Controls.Add(Action("Bersihkan clipboard",(s,e)=>ClearClipboard(),false,170));
            clipboardBox=new TextBox{Location=new Point(18,136),Size=new Size(p.Width-36,265),Anchor=AnchorStyles.Left|AnchorStyles.Right|AnchorStyles.Top|AnchorStyles.Bottom,Multiline=true,ScrollBars=ScrollBars.Vertical,Font=new Font("Consolas",10),BackColor=Elevated,ForeColor=Light,BorderStyle=BorderStyle.FixedSingle};
            p.Controls.Add(clipboardBox);
        }
        void ReadClipboard(){try{clipboardBox.Text=Clipboard.ContainsText()?Clipboard.GetText():"Clipboard tidak berisi teks.";}catch(Exception ex){Error(ex.Message);}}
        void CopyClipboard(){try{Clipboard.SetText(clipboardBox.Text??"");}catch(Exception ex){Error(ex.Message);}}
        void ClearClipboard(){if(MessageBox.Show("Hapus isi clipboard Windows?","Konfirmasi",MessageBoxButtons.YesNo)==DialogResult.Yes){try{Clipboard.Clear();clipboardBox.Clear();}catch(Exception ex){Error(ex.Message);}}}
        void Tools() {
            smallline.Text="Integrasi fitur asli Windows dan utilitas Boot Camp.";
            Panel p=Block(405);Section(p,"Peralatan","Buka utilitas sistem dengan satu klik.");
            var f=new FlowLayoutPanel{Location=new Point(18,80),Size=new Size(p.Width-34,295),Anchor=AnchorStyles.Left|AnchorStyles.Right|AnchorStyles.Top,WrapContents=true,AutoScroll=true};p.Controls.Add(f);
            f.Controls.Add(Action("Boot Camp",(s,e)=>LaunchBootCamp(),true,180));
            f.Controls.Add(Action("Macs Fan Control",(s,e)=>LaunchFan(),false,180));
            f.Controls.Add(Action("Device Manager",(s,e)=>SafeRun("devmgmt.msc"),false,180));
            f.Controls.Add(Action("Task Manager",(s,e)=>SafeRun("taskmgr.exe"),false,180));
            f.Controls.Add(Action("Pengaturan Windows",(s,e)=>SafeRun("ms-settings:"),false,180));
            f.Controls.Add(Action("Volume Mixer",(s,e)=>SafeRun("sndvol.exe"),false,180));
            f.Controls.Add(Action("File Explorer",(s,e)=>SafeRun("explorer.exe"),false,180));
            f.Controls.Add(Action("Jaringan",(s,e)=>SafeRun("ncpa.cpl"),false,180));
            f.Controls.Add(Action("Screenshot",(s,e)=>Screenshot(),false,180));
            f.Controls.Add(Action("Startup Apps",(s,e)=>SafeRun("ms-settings:startupapps"),false,180));
            f.Controls.Add(Action("Windows Update",(s,e)=>SafeRun("ms-settings:windowsupdate"),false,180));
            f.Controls.Add(Action("Informasi Sistem",(s,e)=>SafeRun("msinfo32.exe"),false,180));
        }
        void Screenshot(){
            try {
                Rectangle bounds=SystemInformation.VirtualScreen;
                string outdir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),"Space7");Directory.CreateDirectory(outdir);
                string dest=Path.Combine(outdir,"Space7_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".png");
                using(Bitmap bmp=new Bitmap(bounds.Width,bounds.Height)){using(Graphics g=Graphics.FromImage(bmp))g.CopyFromScreen(bounds.Left,bounds.Top,0,0,bmp.Size);bmp.Save(dest,System.Drawing.Imaging.ImageFormat.Png);}
                MessageBox.Show("Screenshot disimpan di:\n"+dest,"Space7");
            }catch(Exception ex){Error(ex.Message);}
        }
        void About(){
            smallline.Text="Informasi versi, kompatibilitas, dan batasan.";
            var p=Block(317);Section(p,"Space7 • Windows Edition 0.1","Aplikasi native Windows untuk perangkat Boot Camp.");
            var t=Label("Dibuat sebagai port fungsional bertahap dari Space7 untuk macOS.\n\nTersedia: Monitor sistem, daftar proses, pembersih Temp manual, inventaris aplikasi,\npengelola jendela, clipboard manual, tangkapan layar, dan pintasan utilitas.\n\nTidak termasuk: fitur SwiftUI/macOS, mixer audio per-aplikasi, dan pengendalian kipas langsung.\nKontrol kipas memakai aplikasi Macs Fan Control terpisah.\n\nLisensi GPL-3.0-or-later. Proyek: github.com/dahlah77/space7",9,false,Light);
            t.Location=new Point(20,80);t.Size=new Size(p.Width-40,194);t.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;p.Controls.Add(t);
            var actions=Actions(p,265);actions.Controls.Add(Action("Buka repository",(s,e)=>SafeRun("https://github.com/dahlah77/space7"),true,165));
        }
        void LaunchBootCamp(){
            string path=@"C:\Windows\System32\AppleControlPanel.exe";
            if(File.Exists(path))SafeRun(path);else Error("Boot Camp Control Panel tidak ditemukan.");
        }
        void LaunchFan(){
            string[] paths={@"C:\Program Files (x86)\Macs Fan Control\MacsFanControl.exe",@"C:\Program Files\Macs Fan Control\MacsFanControl.exe"};
            string path=paths.FirstOrDefault(File.Exists);
            if(path!=null)SafeRun(path);else Error("Macs Fan Control tidak terpasang.");
        }
        void SafeRun(string file,string args=""){
            try{
                ProcessStartInfo p=new ProcessStartInfo(file);p.UseShellExecute=true;
                if(!String.IsNullOrEmpty(args))p.Arguments="\""+args+"\"";Process.Start(p);
            }catch(Exception e){Error("Tidak dapat membuka "+file+".\n"+e.Message);}
        }
        void Error(string msg) {MessageBox.Show(msg,"Space7",MessageBoxButtons.OK,MessageBoxIcon.Information);}
    }
}
