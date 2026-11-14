// Space7 Windows Edition — Mac-style menu panel
// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Space7Windows {
    internal static class Style7 {
        internal static readonly Color Back = Color.FromArgb(24,24,24);
        internal static readonly Color Card = Color.FromArgb(43,43,43);
        internal static readonly Color Hover = Color.FromArgb(54,54,54);
        internal static readonly Color Border = Color.FromArgb(68,68,68);
        internal static readonly Color Accent = Color.FromArgb(0,122,255);
        internal static readonly Color BlueFill = Color.FromArgb(36,59,91);
        internal static readonly Color Text = Color.FromArgb(236,236,237);
        internal static readonly Color Sub = Color.FromArgb(158,158,160);
        internal static readonly Color Green = Color.FromArgb(50,214,119);
        internal static Font Font(float size,bool bold=false){return new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular);}
        internal static GraphicsPath Round(Rectangle r,int radius) {
            GraphicsPath p=new GraphicsPath();int d=radius*2;
            if(r.Width<=d||r.Height<=d){p.AddRectangle(r);return p;}
            p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);
            p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;
        }
        internal static void Rounded(Graphics g,Rectangle rect,int radius,Color fill,Color border) {
            g.SmoothingMode=SmoothingMode.AntiAlias;using(GraphicsPath p=Round(rect,radius)) {
                using(SolidBrush b=new SolidBrush(fill))g.FillPath(b,p);
                using(Pen pen=new Pen(border,1))g.DrawPath(pen,p);
            }
        }
        internal static Label L(string txt,int x,int y,int w,int h,float size,bool bold=false,Color? color=null) {
            return new Label {Text=txt,Location=new Point(x,y),Size=new Size(w,h),BackColor=Color.Transparent,
                ForeColor=color??Text,Font=Font(size,bold),TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true};
        }
        internal static Button B(string text,int w,int h,EventHandler click,bool accent=false){
            Button b=new Button{Text=text,Size=new Size(w,h),BackColor=accent?Accent:Card,ForeColor=Text,FlatStyle=FlatStyle.Flat,
                Font=Font(9.2f,true),Cursor=Cursors.Hand,Margin=new Padding(0,0,8,0)};
            b.FlatAppearance.BorderColor=Border;b.FlatAppearance.BorderSize=1;
            b.FlatAppearance.MouseOverBackColor=accent?Color.FromArgb(35,143,255):Hover;
            b.Click+=click;return b;
        }
        internal static Control CardContainer(int height){
            return new RoundedPanel{Height=height,Width=328,BackColor=Back,Fill=Card,Stroke=Border,Radius=12,Margin=new Padding(0,0,0,9)};
        }
    }
    internal sealed class RoundedPanel:Panel {
        public int Radius=12;public Color Fill=Style7.Card;public Color Stroke=Style7.Border;
        public RoundedPanel(){DoubleBuffered=true;}
        protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);Style7.Rounded(e.Graphics,new Rectangle(1,1,Width-3,Height-3),Radius,Fill,Stroke);}
    }
    internal sealed class Switch7:Control {
        bool state;
        public bool State {get{return state;}set{state=value;Invalidate();}}
        public event EventHandler Changed;
        public Switch7(bool value) {state=value;Size=new Size(45,25);Cursor=Cursors.Hand;DoubleBuffered=true;}
        protected override void OnClick(EventArgs e) {base.OnClick(e);State=!State;if(Changed!=null)Changed(this,e);}
        protected override void OnPaint(PaintEventArgs e) {
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            using(GraphicsPath p=Style7.Round(new Rectangle(0,0,Width-1,Height-1),12))
            using(SolidBrush b=new SolidBrush(state?Style7.Accent:Color.FromArgb(85,85,85))) e.Graphics.FillPath(b,p);
            using(SolidBrush b=new SolidBrush(Color.White))e.Graphics.FillEllipse(b,state?Width-23:2,2,21,21);
        }
    }
    internal sealed class Graph7:Control {
        readonly double[] values=new double[44]; int idx=0; readonly Color line;
        public Graph7(Color lineColor){line=lineColor;DoubleBuffered=true;BackColor=Style7.Card;Size=new Size(318,44);}
        public void Push(double v){values[idx++%values.Length]=Math.Max(0,Math.Min(100,v));Invalidate();}
        protected override void OnPaint(PaintEventArgs e) {
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;int w=Width,h=Height;
            using(Pen basePen=new Pen(Color.FromArgb(65,65,65),1))e.Graphics.DrawLine(basePen,1,h-2,w-1,h-2);
            PointF[] pts=new PointF[values.Length];int pos=idx%values.Length;
            for(int i=0;i<values.Length;i++)pts[i]=new PointF(2+(w-4)*i/(float)(values.Length-1),h-4-(float)values[(pos+i)%values.Length]*(h-7)/100f);
            using(Pen p=new Pen(line,2)){if(pts.Length>=2)e.Graphics.DrawLines(p,pts);}
        }
    }
    public sealed partial class MacPanel:Form {
        readonly string[] tabs={"Daya","Mixer","Sistem","Jaringan","Disk","Energi","Utilitas","Kontrol"};
        readonly string[] symbols={"☾","☷","▣","◎","▱","ϟ","⚒","☷"};
        RoundedPanel toolbar;
        Panel scroll,header,footer;
        FlowLayoutPanel list;
        Label sectionTitle;
        Label cpuValue,ramValue,batteryValue,netValue;
        Graph7 cpuGraph,memGraph;
        NotifyIcon tray;
        ContextMenuStrip trayMenu;
        System.Windows.Forms.Timer timer;
        int active=2;

        bool awake=false,awakeScreen=false,closing=false;
        float cpuLast=0;
        PerformanceCounter cp;



        public MacPanel() {
            Text="Space7";Font=Style7.Font(10);BackColor=Style7.Back;ForeColor=Style7.Text;
            FormBorderStyle=FormBorderStyle.None;ClientSize=new Size(374,695);ShowInTaskbar=false;TopMost=true;
            StartPosition=FormStartPosition.Manual;DoubleBuffered=true;MaximizeBox=false;MinimizeBox=false;TopMost=Prefs7.Get("always-on-top",true);
            try {this.Icon=System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);} catch{}
            BuildShell();
            Rectangle wa=Screen.PrimaryScreen.WorkingArea;
            Location=new Point(wa.Right-Width-14,wa.Bottom-Height-14);
            trayMenu=new ContextMenuStrip();
            trayMenu.Items.Add("Tampilkan Space7",null,(s,e)=>ShowPanel());
            trayMenu.Items.Add("Pengaturan...",null,(s,e)=>OpenSettings());
            trayMenu.Items.Add("Keluar",null,(s,e)=>ExitApp());
            tray=new NotifyIcon{Icon=this.Icon,Text="Space7 — Windows Edition",ContextMenuStrip=trayMenu,Visible=true};
            tray.MouseClick+=(s,e)=>{if(e.Button==MouseButtons.Left){if(Visible)Hide();else ShowPanel();}};
            timer=new System.Windows.Forms.Timer{Interval=2400};timer.Tick+=(s,e)=>RefreshMetrics();timer.Start();
            try{cp=new PerformanceCounter("Processor","% Processor Time","_Total");cp.NextValue();}catch{}
            Render(2);
        }
        protected override void OnPaint(PaintEventArgs e){
            base.OnPaint(e);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            using(Pen p=new Pen(Color.FromArgb(90,90,90)))using(GraphicsPath shape=Style7.Round(new Rectangle(0,0,Width-1,Height-1),20))e.Graphics.DrawPath(p,shape);
        }
        protected override void OnShown(EventArgs e){base.OnShown(e);using(GraphicsPath p=Style7.Round(new Rectangle(0,0,Width,Height),21))Region=new Region(p);}
        void BuildShell(){
            header=new Panel{Location=new Point(13,14),Size=new Size(348,108),BackColor=Style7.Back};Controls.Add(header);
            PictureBox logo=new PictureBox{Size=new Size(88,45),Location=new Point(130,0),SizeMode=PictureBoxSizeMode.Zoom,BackColor=Style7.Back};
            try{
                string f=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"brand-logo.png");
                if(File.Exists(f)) {
                    using(Bitmap src=new Bitmap(f)) { Bitmap dst=new Bitmap(src.Width,src.Height);for(int x=0;x<src.Width;x++)for(int y=0;y<src.Height;y++){Color c=src.GetPixel(x,y);dst.SetPixel(x,y,Color.FromArgb(c.A,255,255,255));}logo.Image=dst;}
                }
            }catch{} header.Controls.Add(logo);
            if(logo.Image==null)header.Controls.Add(Style7.L("◌",139,0,85,45,30,true));
            toolbar=new RoundedPanel{Location=new Point(0,57),Size=new Size(348,46),Fill=Color.FromArgb(43,43,43),Radius=13,Stroke=Color.FromArgb(65,65,65)};header.Controls.Add(toolbar);
            for(int i=0;i<tabs.Length;i++){
                int tab=i;
                Button b=new Button{Text=symbols[i],Tag=i,Location=new Point(6+i*42,5),Size=new Size(40,36),FlatStyle=FlatStyle.Flat,BackColor=Color.Transparent,ForeColor=Style7.Sub,Font=Style7.Font(16,true),Cursor=Cursors.Hand};
                b.FlatAppearance.BorderSize=0;b.FlatAppearance.MouseOverBackColor=Color.FromArgb(63,63,63);b.Click+=(s,e)=>Render(tab);
                toolbar.Controls.Add(b);
            }
            sectionTitle=Style7.L("",21,128,325,30,10,true,Style7.Sub);Controls.Add(sectionTitle);
            scroll=new Panel{Location=new Point(10,166),Size=new Size(354,459),BackColor=Style7.Back,AutoScroll=true};scroll.HorizontalScroll.Enabled=false;scroll.HorizontalScroll.Visible=false;Controls.Add(scroll);
            list=new FlowLayoutPanel{AutoSize=false,Location=new Point(0,0),Size=new Size(336,460),FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=false,BackColor=Style7.Back};
            scroll.Controls.Add(list);
            footer=new Panel{Location=new Point(20,634),Size=new Size(335,44),BackColor=Style7.Back};Controls.Add(footer);
            footer.Controls.Add(Style7.B("⚙  Pengaturan",160,35,(s,e)=>OpenSettings()));
            var quit=Style7.B("⏻  Keluar",160,35,(s,e)=>ExitApp());quit.Location=new Point(168,0);footer.Controls.Add(quit);
            footer.Controls[0].Location=new Point(0,0);
        }
        void BuildList(){
            list.Controls.Clear();cpuValue=ramValue=batteryValue=netValue=null;cpuGraph=memGraph=null;
            list.SuspendLayout();list.Size=new Size(336,3000);scroll.VerticalScroll.Value=0;
        }
        void Done(){
            list.ResumeLayout(true);
            int total=0;foreach(Control c in list.Controls)total=Math.Max(total,c.Bottom+c.Margin.Bottom);
            int available=Math.Min(535,Math.Max(130,total+10));
            int h=166+available+61;
            h=Math.Max(375,Math.Min(h,800));
            ClientSize=new Size(374,h);
            scroll.Height=h-230;footer.Top=h-52;
            list.Height=total+25;
            Rectangle wa=Screen.FromControl(this).WorkingArea;
            Location=new Point(wa.Right-Width-14,wa.Bottom-Height-14);
            using(GraphicsPath p=Style7.Round(new Rectangle(0,0,Width,Height),21))Region=new Region(p);
        }
        public void SelectTab(int index){Render(index);ShowPanel();}
        void Render(int index){
            active=index;BuildList();
            for(int i=0;i<tabs.Length;i++){
                Button b=(Button)toolbar.Controls[i];
                b.BackColor=i==active?Style7.BlueFill:Color.Transparent;
                b.ForeColor=i==active?Style7.Accent:Color.FromArgb(139,139,139);
            }
            sectionTitle.Text=tabs[index].ToUpperInvariant();
            switch(index) {
                case 0:PageAwake();break;
                case 1:PageMixer();break;
                case 2:PageSystem();break;
                case 3:PageNetwork();break;
                case 4:PageDisk();break;
                case 5:PageEnergy();break;
                case 6:PageTools();break;
                case 7:PageControls();break;
            }
            Done();RefreshMetrics();
        }
        RoundedPanel Card(int height) {
            RoundedPanel p=(RoundedPanel)Style7.CardContainer(height);
            list.Controls.Add(p);return p;
        }
        void Heading(string text,Color? color=null) {
            Label l=Style7.L(text.ToUpperInvariant(),5,0,316,26,9.8f,true,color??Style7.Sub);
            Panel line=new Panel{Height=31,Width=328,Margin=new Padding(0,6,0,2)};
            line.Controls.Add(l);list.Controls.Add(line);
        }
        Label RowText(Panel p,string title,string desc,int y=13,int left=46,int w=210){
            Label main=Style7.L(title,left,y,w,28,11.2f,true);p.Controls.Add(main);
            if(desc!=""){
                Label sub=Style7.L(desc,left,y+28,w,p.Height-y-36,9.2f,false,Style7.Sub);
                sub.AutoEllipsis=false;sub.TextAlign=ContentAlignment.TopLeft;p.Controls.Add(sub);
            }
            return main;
        }
        void AddIcon(Panel p,string sym,int y){Label l=Style7.L(sym,13,y,27,36,18,true,Style7.Accent);p.Controls.Add(l);}
        void Item(string title,string desc,string ico,Action click,int height=78) {
            RoundedPanel p=Card(height);AddIcon(p,ico,Math.Max(14,height/2-20));
            RowText(p,title,desc,Math.Max(8,height/2-28),46,242);
            var more=Style7.L("›",300,height/2-22,20,35,21,false,Style7.Sub);p.Controls.Add(more);
            p.Cursor=Cursors.Hand;
            EventHandler tap=(s,e)=>{if(click!=null)click();};
            p.Click+=tap;foreach(Control c in p.Controls)c.Click+=tap;
        }
        Switch7 ToggleRow(string title,string desc,string ico,bool checkedOn,Action<bool> change,int height=83){
            RoundedPanel p=Card(height);AddIcon(p,ico,Math.Max(12,height/2-20));
            RowText(p,title,desc,10,46,218);
            Switch7 sw=new Switch7(checkedOn){Location=new Point(274,(height-25)/2)};p.Controls.Add(sw);
            sw.Changed+=(s,e)=>{if(change!=null)change(sw.State);};
            p.Click+=(s,e)=>{sw.State=!sw.State;if(change!=null)change(sw.State);};return sw;
        }
        void Plain(string title,string value,Color? col=null) {
            RoundedPanel p=Card(58);p.Controls.Add(Style7.L(title,16,7,195,43,10,false,Style7.Sub));
            Label right=Style7.L(value,206,7,109,43,10,true,col??Style7.Text);right.TextAlign=ContentAlignment.MiddleRight;p.Controls.Add(right);
        }
        void PageAwake(){
            ToggleRow("Tetap terjaga","Mencegah Windows memasuki mode tidur.","☾",awake,(v)=>SetAwake(v,awakeScreen),84);
            ToggleRow("Layar tetap menyala","Menjaga layar aktif saat mode tetap terjaga.","▣",awakeScreen,(v)=>SetAwake(awake,v),84);
            Heading("Pintasan daya");
            Item("Pengaturan daya","Atur sleep, baterai dan layar Windows.","ϟ",()=>Open("ms-settings:powersleep"),76);
            Item("Penghemat baterai","Buka pengaturan baterai Windows.","▱",()=>Open("ms-settings:batterysaver"),76);
            Item("Screen saver","Atur penguncian dan screen saver.","☾",()=>Open("control.exe","desk.cpl,,1"),76);
        }
        [DllImport("kernel32.dll")] static extern uint SetThreadExecutionState(uint flags);
        void SetAwake(bool state,bool screen){
            awake=state;awakeScreen=screen;
            try{SetThreadExecutionState(0x80000000U|(awake?1U:0U)|(awake&&screen?2U:0U));}catch{}
        }
        void PageMixer(){ PageMixerNative(); }
        void PageSystem(){
            RoundedPanel p=Card(235);
            p.Controls.Add(Style7.L("Penggunaan perangkat",12,10,240,31,12,true));
            p.Controls.Add(Style7.L("CPU",15,48,62,22,9.8f,false,Style7.Sub));
            cpuValue=Style7.L("—",250,48,61,23,10,true,Style7.Accent);cpuValue.TextAlign=ContentAlignment.MiddleRight;p.Controls.Add(cpuValue);
            cpuGraph=new Graph7(Style7.Accent){Location=new Point(13,77),Size=new Size(302,45)};p.Controls.Add(cpuGraph);
            p.Controls.Add(Style7.L("Memori",15,139,82,22,9.8f,false,Style7.Sub));
            ramValue=Style7.L("—",245,139,66,23,10,true,Style7.Green);ramValue.TextAlign=ContentAlignment.MiddleRight;p.Controls.Add(ramValue);
            memGraph=new Graph7(Style7.Green){Location=new Point(13,169),Size=new Size(302,45)};p.Controls.Add(memGraph);
            Heading("Status lainnya");
            RoundedPanel batt=Card(65);batt.Controls.Add(Style7.L("▱  Baterai",15,10,155,41,10.2f,true));batteryValue=Style7.L("—",203,10,108,41,10.3f,true,Style7.Green);batteryValue.TextAlign=ContentAlignment.MiddleRight;batt.Controls.Add(batteryValue);
            Item("Rincian proses","Penggunaan RAM dan daftar proses aktif.","▣",()=>ShowLegacy("Proses"),76);
            Item("Monitor perangkat","Lihat info hardware dan pengaturan monitor.","◉",()=>Open("msinfo32.exe"),76);
            Item("Kontrol kipas Mac","Baca RPM/temperatur dari aplikasi terpasang.","ϟ",()=>RunFan(),88);
        }
        void PageNetwork(){
            RoundedPanel p=Card(126);
            p.Controls.Add(Style7.L("Koneksi aktif",17,12,295,29,12,true));
            netValue=Style7.L("Memeriksa jaringan…",17,46,286,49,11,false,Style7.Accent);p.Controls.Add(netValue);
            Heading("Jaringan");
            Item("Adapter jaringan","Lihat Wi-Fi/Ethernet di Windows.","◎",()=>Open("ncpa.cpl"),80);
            Item("Pengaturan Wi-Fi","Kelola jaringan dan hotspot.","≋",()=>Open("ms-settings:network-wifi"),80);
            Item("Penggunaan data","Lihat konsumsi koneksi Windows.","▦",()=>Open("ms-settings:datausage"),80);
        }
        void PageDisk(){
            RoundedPanel p=Card(126);p.Controls.Add(Style7.L("Penyimpanan C:",17,11,292,27,11,true));
            try{
                DriveInfo d=new DriveInfo("C");double per=(double)(d.TotalSize-d.AvailableFreeSpace)/d.TotalSize*100;
                p.Controls.Add(Style7.L(Math.Round(per)+"% digunakan",17,42,292,27,11,true,Style7.Accent));
                ProgressBar bar=new ProgressBar{Minimum=0,Maximum=100,Value=(int)Math.Min(100,per),Location=new Point(17,77),Size=new Size(294,12),Style=ProgressBarStyle.Continuous};p.Controls.Add(bar);
                p.Controls.Add(Style7.L(FormatBytes(d.AvailableFreeSpace)+" tersedia dari "+FormatBytes(d.TotalSize),17,95,297,20,9,false,Style7.Sub));
            }catch{}
            Heading("Penyimpanan");
            Item("Pembersih","Cari file Temp lama sebelum menghapus.","✦",()=>ShowLegacy("Pembersih"),79);
            Item("Pengelola berkas","Buka File Explorer.","▤",()=>Open("explorer.exe"),76);
            Item("Penyimpanan Windows","Lihat kategori file dan Storage Sense.","▰",()=>Open("ms-settings:storagesense"),79);
        }
        void PageEnergy(){
            RoundedPanel p=Card(115);
            p.Controls.Add(Style7.L("Kondisi baterai",17,12,290,30,12,true));
            batteryValue=Style7.L("—",17,49,292,42,19,true,Style7.Green);p.Controls.Add(batteryValue);
            Heading("Daya dan suhu");
            Item("Mode hemat daya","Pengaturan efisiensi energi Windows.","ϟ",()=>Open("ms-settings:powersleep"),76);
            Item("Suhu & RPM kipas","Aplikasi Macs Fan Control (bila terpasang).","▣",()=>RunFan(),82);
            Item("Diagnostik baterai","Buat laporan baterai Windows.","▱",()=>BatteryReport(),86);
        }
        void PageTools(){
            Item("Panel cepat","Buka utilitas Windows dari satu tempat.","▦",()=>ShowLegacy("Peralatan"),82);
            Item("Pembersih","Temukan file sementara dan pilih yang aman.","✦",()=>ShowLegacy("Pembersih"),80);
            Item("Paket aplikasi","Kelola aplikasi Windows dengan winget.","⬡",()=>Open("ms-settings:appsfeatures"),79);
            Item("Media","Screenshot dan pengaturan perekaman Windows.","▣",()=>ShowMedia(),88);
            Item("Clipboard","Baca/salin/hapus clipboard secara manual.","▤",()=>ShowLegacy("Clipboard"),89);
            Item("Tata letak jendela","Pindah jendela ke kiri/kanan/atas.","▣",()=>ShowLayout(),87);
            Item("Uninstaller","Tinjau aplikasi sebelum menghapus.","♜",()=>ShowLegacy("Aplikasi"),83);
            Item("Bersihkan URL","Hapus pelacak dari link yang dipilih.","◎",()=>ShowUrlCleaner(),85);
            Item("Mode pembersihan","Kunci input sementara belum tersedia di Windows.","▦",()=>InfoUnsupported(),90);
            Item("Salin teks layar","Gunakan alat bawaan Snipping Tool Windows.","▤",()=>Open("ms-screenclip:"),88);
        }
        void PageControls(){
            Heading("Jendela");
            ToggleRow("Pengatur jendela","Aktifkan akses tata letak cepat.","▣",Prefs7.Get("window-layout",true),(v)=>Prefs7.Set("window-layout",v),85);
            Item("Pindah jendela aktif","Geser ke kiri, kanan, tengah atau maksimalkan.","▣",()=>ShowLayout(),80);
            Item("App switcher","Pindah aplikasi dengan Alt+Tab bawaan Windows.","◉",()=>ShowLegacy("Jendela"),85);
            Heading("Mouse dan keyboard");
            Item("Mouse dan trackpad","Pengaturan bawaan Windows.","☷",()=>Open("ms-settings:mousetouchpad"),80);
            Item("Keyboard Mac","Pengaturan Boot Camp keyboard/backlight.","▦",()=>OpenBootCamp(),85);
            Heading("Berkas");
            Item("Clipboard","Manajemen clipboard manual Space7.","▤",()=>ShowLegacy("Clipboard"),78);
            Item("Pembersih","Pilih file sementara lama untuk dibersihkan.","✦",()=>ShowLegacy("Pembersih"),78);
        }
        void RefreshMetrics(){
            try{
                if(cpuValue!=null){
                    double cpu=cp!=null?Math.Round(cp.NextValue()):0;cpuLast=(float)cpu;
                    cpuValue.Text=cpu+"%";if(cpuGraph!=null)cpuGraph.Push(cpu);
                }
                if(ramValue!=null){
                    using(ManagementObjectSearcher q=new ManagementObjectSearcher("SELECT TotalVisibleMemorySize,FreePhysicalMemory FROM Win32_OperatingSystem"))
                    foreach(ManagementObject o in q.Get()){
                        double total=Convert.ToDouble(o["TotalVisibleMemorySize"]),free=Convert.ToDouble(o["FreePhysicalMemory"]);double pct=Math.Round((total-free)/total*100);
                        ramValue.Text=pct+"%";if(memGraph!=null)memGraph.Push(pct);
                    }
                }
                if(batteryValue!=null){
                    PowerStatus s=SystemInformation.PowerStatus;int pct=(int)Math.Round(100*s.BatteryLifePercent);
                    bool charge=(s.PowerLineStatus==PowerLineStatus.Online);
                    batteryValue.Text=(pct>=0&&pct<=100?pct+"%":"—")+(charge?"  ·  AC":"");
                }
                if(netValue!=null){
                    var nics=NetworkInterface.GetAllNetworkInterfaces().Where(n=>n.OperationalStatus==OperationalStatus.Up&&n.NetworkInterfaceType!=NetworkInterfaceType.Loopback).ToArray();
                    netValue.Text=nics.Length>0?(nics[0].Name+"\n"+nics[0].NetworkInterfaceType.ToString()):"Tidak ada adapter aktif";
                }
            }catch{}
        }
        static string FormatBytes(long n){if(n>1073741824)return (n/1073741824d).ToString("0.0")+" GB";if(n>1048576)return(n/1048576d).ToString("0.0")+" MB";return (n/1024d).ToString("0")+" KB";}
        void Open(string f,string arg=""){try{ProcessStartInfo x=new ProcessStartInfo(f,arg){UseShellExecute=true};Process.Start(x);}catch(Exception ex){MessageBox.Show(ex.Message,"Space7");}}
        void OpenBootCamp(){string f=@"C:\Windows\System32\AppleControlPanel.exe";if(File.Exists(f))Open(f);else MessageBox.Show("Boot Camp tidak ditemukan.","Space7");}
        void RunFan(){string[] a={@"C:\Program Files (x86)\Macs Fan Control\MacsFanControl.exe",@"C:\Program Files\Macs Fan Control\MacsFanControl.exe"};string f=a.FirstOrDefault(File.Exists);if(f==null)MessageBox.Show("Macs Fan Control tidak ditemukan.","Space7");else Open(f);}
        void ShowLegacy(string section){
            MainWindow w=new MainWindow();w.ShowInTaskbar=true;w.TopMost=false;w.Show();
            try{var m=typeof(MainWindow).GetMethod("Navigate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);m.Invoke(w,new object[]{section});}catch{}
        }
        void ShowMedia() {
            Form f=new Form{Text="Space7 • Media",Size=new Size(480,230),StartPosition=FormStartPosition.CenterScreen,BackColor=Style7.Back,ForeColor=Style7.Text,Font=Style7.Font(10)};
            FlowLayoutPanel p=new FlowLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(16),BackColor=Style7.Back};f.Controls.Add(p);
            p.Controls.Add(Style7.B("Ambil screenshot",184,43,(s,e)=>ShowLegacy("Peralatan")));
            p.Controls.Add(Style7.B("Alat cuplikan",184,43,(s,e)=>Open("ms-screenclip:")));
            p.Controls.Add(Style7.B("Pengaturan rekaman",188,43,(s,e)=>Open("ms-settings:gaming-gamedvr")));
            f.Show();
        }
        void ShowLayout() {
            new Layout7().Show();
        }
        void ShowUrlCleaner(){new UrlCleaner7().Show();}
        void BatteryReport(){
            try{
                string path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),"Space7-BatteryReport.html");
                Process.Start(new ProcessStartInfo("powercfg.exe","/batteryreport /output \""+path+"\""){UseShellExecute=false,CreateNoWindow=true}).WaitForExit(5000);
                if(File.Exists(path)){Open(path);}else MessageBox.Show("Laporan baterai tidak tersedia.","Space7");
            }catch(Exception ex){MessageBox.Show(ex.Message,"Space7");}
        }
        void InfoUnsupported(){MessageBox.Show("Fitur ini belum selesai dipindahkan ke Windows. Space7 tidak akan memasang driver keyboard atau menahan input tanpa mekanisme pemulihan yang aman.","Space7");}
        void OpenSettings(){new Settings7(this).Show();Hide();}
        void ShowPanel(){Show();BringToFront();Activate();}
        void ExitApp(){closing=true;SetAwake(false,false);tray.Visible=false;tray.Dispose();Close();Application.Exit();}
        protected override void OnFormClosing(FormClosingEventArgs e){
            if(!closing&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;Hide();return;}
            try{SetAwake(false,false);if(tray!=null){tray.Visible=false;tray.Dispose();}if(cp!=null)cp.Dispose();if(timer!=null)timer.Dispose();}catch{}
            base.OnFormClosing(e);
        }
    }
    internal static class Prefs7 {
        public static bool Get(string name,bool fallback){try{using(var key=Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Space7Windows")){object v=key==null?null:key.GetValue(name);return v==null?fallback:Convert.ToInt32(v)==1;}}catch{return fallback;}}
        public static void Set(string name,bool value){using(var k=Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Space7Windows")){k.SetValue(name,value?1:0,Microsoft.Win32.RegistryValueKind.DWord);}}
    }
    internal static class Audio7 {
        [ComImport,Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]sealed class MMDeviceEnumerator{}
        [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IMMDeviceEnumerator {
            int EnumAudioEndpoints(int d,uint mask,out IntPtr devices);
            int GetDefaultAudioEndpoint(int dataFlow,int role,out IMMDevice device);
            int GetDevice([MarshalAs(UnmanagedType.LPWStr)]string id,out IMMDevice device);
            int RegisterEndpointNotificationCallback(IntPtr callback);
            int UnregisterEndpointNotificationCallback(IntPtr callback);
        }
        [Guid("D666063F-1587-4E43-81F1-B948E807363F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IMMDevice {
            int Activate([In,MarshalAs(UnmanagedType.LPStruct)]Guid iid,int ctx,IntPtr activationParams,[MarshalAs(UnmanagedType.IUnknown)]out object iface);
            int OpenPropertyStore(int access,out IntPtr store);
            int GetId([MarshalAs(UnmanagedType.LPWStr)]out string id);
            int GetState(out int state);
        }
        [Guid("5CDF2C82-841E-4546-9722-0CF74078229A"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IAudioEndpointVolume {
            int RegisterControlChangeNotify(IntPtr callback);int UnregisterControlChangeNotify(IntPtr callback);
            int GetChannelCount(out uint count);
            int SetMasterVolumeLevel(float level,Guid ctx);
            int SetMasterVolumeLevelScalar(float level,Guid ctx);
            int GetMasterVolumeLevel(out float level);
            int GetMasterVolumeLevelScalar(out float level);
            int SetChannelVolumeLevel(uint c,float level,Guid ctx);
            int SetChannelVolumeLevelScalar(uint c,float level,Guid ctx);
            int GetChannelVolumeLevel(uint c,out float level);
            int GetChannelVolumeLevelScalar(uint c,out float level);
            int SetMute([MarshalAs(UnmanagedType.Bool)]bool mute,Guid ctx);
            int GetMute([MarshalAs(UnmanagedType.Bool)]out bool mute);
        }
        static IAudioEndpointVolume Volume(){
            IMMDeviceEnumerator en=(IMMDeviceEnumerator)(object)new MMDeviceEnumerator();IMMDevice device;
            int h=en.GetDefaultAudioEndpoint(0,1,out device);if(h!=0)Marshal.ThrowExceptionForHR(h);
            object obj;Guid iid=new Guid("5CDF2C82-841E-4546-9722-0CF74078229A");
            h=device.Activate(iid,23,IntPtr.Zero,out obj);if(h!=0)Marshal.ThrowExceptionForHR(h);
            return (IAudioEndpointVolume)obj;
        }
        public static int GetVolume(){try{float v;Volume().GetMasterVolumeLevelScalar(out v);return (int)Math.Round(v*100);}catch{return 50;}}
        public static void SetVolume(int x){try{Volume().SetMasterVolumeLevelScalar(x/100f,Guid.Empty);}catch{}}
        public static void ToggleMute(){try{bool mute;IAudioEndpointVolume v=Volume();v.GetMute(out mute);v.SetMute(!mute,Guid.Empty);}catch{}}
    }
}
