// Space7 Windows Edition — Native settings, window layout and URL cleaner
// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Space7Windows {
    internal sealed class Settings7:Form {
        MacPanel owner;
        Panel side,body,top;
        FlowLayoutPanel menu,content;
        TextBox search;
        Label pageTitle;
        string selected="Fitur";
        readonly List<Tuple<string,string,string,int>> items=new List<Tuple<string,string,string,int>>();
        const int sw=238;
        [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr SendMessage(IntPtr h,int msg,IntPtr w,string text);
        readonly Color Active=Color.FromArgb(0,122,255);
        public Settings7(MacPanel parent){
            owner=parent;Text="Pengaturan Space7";StartPosition=FormStartPosition.CenterScreen;Size=new Size(1030,700);
            MinimumSize=new Size(775,520);BackColor=Color.FromArgb(37,37,37);ForeColor=Style7.Text;Font=Style7.Font(10);
            FormClosing+=(s,e)=>{owner.Show();owner.Activate();};
            BuildItems();Build();Select("Fitur");
        }
        void BuildItems(){
            Add("Penting","Umum","⚙",0);Add("Penting","Fitur","▦",6);Add("Penting","Energi","ϟ",5);Add("Penting","Monitor","▣",2);
            Add("Kontrol jendela","Mouse & Trackpad","☷",7);Add("Kontrol jendela","Switcher","▢",7);
            Add("Kontrol jendela","Tata letak jendela","▦",7);Add("Kontrol jendela","Tutup aplikasi","▤",7);
            Add("Berkas","Clipboard","▤",6);Add("Berkas","Cut & Paste","✂",6);Add("Berkas","Shelf","▱",6);Add("Berkas","Media","▣",6);
            Add("Utilitas","Alat cepat","✦",6);Add("Utilitas","Bersihkan URL","◎",6);Add("Utilitas","Paket aplikasi","⬡",6);
            Add("Utilitas","Uninstaller","♜",6);Add("Utilitas","Pembersih","✦",6);
            Add("Kustomisasi","Panel","▦",2);Add("Kustomisasi","Audio","☷",1);Add("Kustomisasi","Koneksi","◎",3);
            Add("Kustomisasi","Penyimpanan","▰",4);Add("Kustomisasi","Tampilan","◐",2);Add("Kustomisasi","Tentang","ⓘ",2);
        }
        void Add(string group,string name,string icon,int index){items.Add(Tuple.Create(group,name,icon,index));}
        void Build(){
            side=new Panel{Dock=DockStyle.Left,Width=sw,BackColor=Color.FromArgb(40,40,40)};Controls.Add(side);
            var line=new Panel{Dock=DockStyle.Right,Width=1,BackColor=Color.FromArgb(57,57,57)};side.Controls.Add(line);
            var head=new Panel{Dock=DockStyle.Top,Height=64,BackColor=side.BackColor};side.Controls.Add(head);
            search=new TextBox{BorderStyle=BorderStyle.FixedSingle,BackColor=Color.FromArgb(79,79,79),ForeColor=Color.White,
                Font=Style7.Font(10),Location=new Point(15,19),Size=new Size(sw-32,29)};
            head.Controls.Add(search);SendMessage(search.Handle,0x1501,IntPtr.Zero,"Cari pengaturan...");search.TextChanged+=(s,e)=>BuildMenu();
            menu=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new Padding(11,7,10,10),BackColor=side.BackColor};
            side.Controls.Add(menu);menu.Dock=DockStyle.None;menu.Location=new Point(0,64);menu.Size=new Size(sw-1,side.Height-64);menu.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;menu.BringToFront();head.BringToFront();
            body=new Panel{Dock=DockStyle.Fill,BackColor=Color.FromArgb(39,39,39)};Controls.Add(body);body.BringToFront();
            top=new Panel{Dock=DockStyle.Top,Height=72,BackColor=body.BackColor};body.Controls.Add(top);
            pageTitle=Style7.L("Fitur",25,13,530,48,22,true);top.Controls.Add(pageTitle);
            content=new FlowLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(24,9,19,28),FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,BackColor=body.BackColor};
            body.Controls.Add(content);content.Dock=DockStyle.None;content.Location=new Point(0,72);content.Size=new Size(body.Width,body.Height-72);content.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;content.BringToFront();top.BringToFront();
            body.Resize+=(s,e)=>FixWidths();BuildMenu();
        }
        void FixWidths(){if(content==null)return;int w=Math.Max(360,content.ClientSize.Width-53);foreach(Control c in content.Controls) c.Width=w;}
        void BuildMenu(){
            if(menu==null)return;menu.SuspendLayout();menu.Controls.Clear();
            string last="";
            foreach(var it in items){
                if(!String.IsNullOrWhiteSpace(search.Text)&&it.Item2.IndexOf(search.Text,StringComparison.OrdinalIgnoreCase)<0&&it.Item1.IndexOf(search.Text,StringComparison.OrdinalIgnoreCase)<0)continue;
                if(last!=it.Item1) {
                    last=it.Item1;
                    Label group=Style7.L(last,10,8,199,25,8.5f,true,Style7.Sub);group.Margin=new Padding(0,14,0,5);menu.Controls.Add(group);
                }
                string key=it.Item2;
                Button b=new Button{Size=new Size(206,34),Text=it.Item3+"     "+key,
                    Font=Style7.Font(9.6f,key==selected),FlatStyle=FlatStyle.Flat,TextAlign=ContentAlignment.MiddleLeft,BackColor=(key==selected?Active:Color.Transparent),
                    ForeColor=key==selected?Color.White:Style7.Text,Cursor=Cursors.Hand,Margin=new Padding(1,0,0,1)};
                b.FlatAppearance.BorderSize=0;b.FlatAppearance.MouseOverBackColor=key==selected?Active:Color.FromArgb(68,68,68);
                b.Click+=(s,e)=>Select(key);menu.Controls.Add(b);
            }
            menu.ResumeLayout();menu.AutoScrollPosition=new Point(0,0);
        }
        void Select(string key){
            selected=key;pageTitle.Text=key;BuildMenu();content.SuspendLayout();content.Controls.Clear();
            if(key=="Fitur")Features();
            else if(key=="Umum")General();
            else if(key=="Tentang")About();
            else Specific(key);
            content.ResumeLayout();FixWidths();content.AutoScrollPosition=new Point(0,0);
        }
        Panel Block(int height){
            Panel p=new RoundedPanel{BackColor=body.BackColor,Height=height,Width=Math.Max(380,content.ClientSize.Width-53),Margin=new Padding(0,0,0,14),Radius=12,Fill=Style7.Card,Stroke=Style7.Border};
            content.Controls.Add(p);return p;
        }
        Label H(string text, int y, int height=29,float size=12,bool bold=true){
            return Style7.L(text,17,y,600,height,size,bold);
        }
        void SectionLabel(string name){
            Label l=Style7.L(name,0,4,400,34,12,true);l.Margin=new Padding(0,16,0,5);content.Controls.Add(l);
        }
        void Intro(string title,string sub){
            Panel p=Block(120);p.Controls.Add(H(title,12,29,14));
            p.Controls.Add(Style7.L(sub,17,44,690,59,9.5f,false,Style7.Sub));
        }
        void SettingsRow(string title,string desc,string kind,Action go=null){
            Panel p=Block(76);
            p.Controls.Add(Style7.L("▣",14,22,35,35,15,true,Style7.Accent));
            p.Controls.Add(Style7.L(title,55,10,410,28,10.5f,true));
            p.Controls.Add(Style7.L(desc,55,37,570,29,9.2f,false,Style7.Sub));
            if(go!=null){
                Button b=Style7.B("Buka  ›",102,32,(s,e)=>go());
                b.Location=new Point(Math.Max(460,p.Width-119),22);b.Anchor=AnchorStyles.Top|AnchorStyles.Right;p.Controls.Add(b);
            } else {
                Label l=Style7.L(kind,Math.Max(440,p.Width-175),21,162,31,9,false,Style7.Sub);l.TextAlign=ContentAlignment.MiddleRight;l.Anchor=AnchorStyles.Top|AnchorStyles.Right;p.Controls.Add(l);
            }
        }
        void Features(){
            Panel topCard=Block(147);
            var featButton=Style7.B("Fitur",122,32,(s,e)=>Select("Fitur"));
            featButton.Location=new Point(17,14);topCard.Controls.Add(featButton);
            var permission=Style7.B("Izin akses",122,32,(s,e)=>Select("Umum"));permission.Location=new Point(144,14);topCard.Controls.Add(permission);
            topCard.Controls.Add(Style7.L("Pilih fungsi yang benar-benar tersedia di Windows. Fitur macOS yang belum dipindahkan akan ditandai.",17,57,690,36,9.4f,false,Style7.Sub));
            topCard.Controls.Add(Style7.L("Space7 Windows Preview • Status fitur",17,103,540,28,10,true,Style7.Text));
            SectionLabel("Paket awal");
            Panel bundles=Block(165);
            var defs=new[]{new[]{"★  Penting","Monitor, audio dan mode tetap terjaga","0"},new[]{"▦  Jendela","Layout jendela dan task switcher","7"},new[]{"▱  Baterai","Panel energi dan suhu kipas","5"}};
            for(int i=0;i<defs.Length;i++){
                int at=i;Panel p=new RoundedPanel{Location=new Point(13+i*237,11),Size=new Size(225,139),Fill=Color.FromArgb(55,55,55),Stroke=Style7.Border,Radius=11};
                bundles.Controls.Add(p);
                p.Controls.Add(Style7.L(defs[i][0],11,11,197,29,10.3f,true));
                p.Controls.Add(Style7.L(defs[i][1],11,42,200,47,9,false,Style7.Sub));
                Button b=Style7.B("Buka",81,30,(s,e)=>{owner.SelectTab(int.Parse(defs[at][2]));Close();});
                b.Location=new Point(11,95);p.Controls.Add(b);
            }
            SectionLabel("Jendela dan taskbar");
            SettingsRow("Pengalih aplikasi","Menampilkan daftar aplikasi dan jendela yang sedang aktif.","Windows",()=>Jump(7));
            SettingsRow("Tata letak jendela","Geser jendela ke area layar yang dipilih.","Windows",()=>Jump(7));
            SettingsRow("Dock Preview","Fitur macOS Dock Preview belum tersedia di Windows.","Belum didukung");
            SectionLabel("Sistem");
            SettingsRow("Monitor sistem","CPU, RAM, disk, baterai dan jaringan Windows.","Tersedia",()=>Jump(2));
            SettingsRow("Volume mixer","Master volume tersedia; gunakan mixer Windows untuk setiap aplikasi.","Parsial",()=>Jump(1));
            SettingsRow("Mode tetap terjaga","Mencegah Windows sleep selama fitur diaktifkan.","Tersedia",()=>Jump(0));
            SettingsRow("Kontrol kipas","Kipas dibaca melalui Macs Fan Control yang terpisah.","Aplikasi terpisah",()=>Jump(5));
            SectionLabel("Berkas dan utilitas");
            SettingsRow("Pembersih","Hapus file Temp lama hanya setelah diperiksa.","Tersedia",()=>Jump(6));
            SettingsRow("Clipboard","Baca, salin dan kosongkan clipboard manual.","Tersedia",()=>Jump(6));
            SettingsRow("Media","Tangkapan layar dan kontrol perekam Windows.","Parsial",()=>Jump(6));
            SettingsRow("Homebrew","Homebrew macOS tidak tersedia; gunakan Windows Package Manager.","Alternatif",()=>Jump(6));
        }
        void General(){
            Intro("Pengaturan umum","Space7 berjalan sebagai aplikasi Windows native, tanpa memodifikasi driver Boot Camp atau sistem keamanan.");
            SectionLabel("Pengoperasian");
            TogglePreference("Tampilkan panel di atas jendela lain","Panel tetap di atas selama dibuka.","always-on-top",true,v=>owner.TopMost=v);
            TogglePreference("Mode jendela Windows","Aktifkan pintasan tata letak jendela.","window-layout",true,null);
            SectionLabel("Akses perangkat");
            SettingsRow("Izin audio dan mikrofon","Pengaturan privasi Windows.","Windows",()=>Open("ms-settings:privacy-microphone"));
            SettingsRow("Izin screen capture","Kelola screenshot dan perekaman Windows.","Windows",()=>Open("ms-settings:privacy-broadfilesystemaccess"));
            SettingsRow("Boot Camp Control Panel","Kelola pengaturan keyboard Apple.","Boot Camp",()=>Open(@"C:\Windows\System32\AppleControlPanel.exe"));
        }
        void TogglePreference(string title,string desc,string key,bool fallback,Action<bool> changed){
            Panel p=Block(76);p.Controls.Add(Style7.L(title,17,7,490,30,10.5f,true));
            p.Controls.Add(Style7.L(desc,17,36,545,25,9,false,Style7.Sub));
            Switch7 sw=new Switch7(Prefs7.Get(key,fallback)){Location=new Point(Math.Max(470,p.Width-69),25),Anchor=AnchorStyles.Top|AnchorStyles.Right};
            sw.Changed+=(s,e)=>{Prefs7.Set(key,sw.State);if(changed!=null)changed(sw.State);};p.Controls.Add(sw);
        }
        void Specific(string key){
            var item=items.FirstOrDefault(x=>x.Item2==key);
            int idx=item==null?2:item.Item4;
            Intro(key,"Pengaturan terkait "+key+" untuk Windows. Fungsi tersedia melalui panel Space7 dan utilitas sistem.");
            SectionLabel("Tindakan");
            SettingsRow("Buka panel "+key,"Akses bagian terkait di panel menu Space7.","Buka",()=>Jump(idx));
            switch(key){
                case "Tata letak jendela":
                case "Switcher":
                case "Tutup aplikasi":
                    SettingsRow("Task View","Kelola seluruh jendela dan desktop Windows.","Windows",()=>Open("explorer.exe"));
                    break;
                case "Clipboard":
                    SettingsRow("Riwayat clipboard Windows","Pengaturan Win+V dan clipboard.","Windows",()=>Open("ms-settings:clipboard"));break;
                case "Media":
                    SettingsRow("Tangkapan layar","Gunakan Snipping Tool Windows.","Windows",()=>Open("ms-screenclip:"));break;
                case "Paket aplikasi":
                    SettingsRow("Aplikasi terpasang","Buka pengelola aplikasi.","Windows",()=>Open("ms-settings:appsfeatures"));break;
                case "Energi":
                    SettingsRow("Baterai dan tidur","Buka pengaturan daya Windows.","Windows",()=>Open("ms-settings:powersleep"));break;
                case "Audio":
                    SettingsRow("Output dan input suara","Pengaturan volume dan perangkat.","Windows",()=>Open("ms-settings:sound"));break;
                default:break;
            }
            SectionLabel("Status kompatibilitas");
            Panel p=Block(105);
            p.Controls.Add(Style7.L("Implementasi Windows",15,11,570,29,11,true,Style7.Accent));
            p.Controls.Add(Style7.L("Fitur khusus AppKit/SwiftUI memerlukan pengembangan Windows tersendiri. Item yang belum didukung tidak dijalankan.",15,44,680,45,9.4f,false,Style7.Sub));
        }
        void About(){
            Intro("Space7 — Windows Edition","Dibuat dari referensi source Space7 macOS dan dijalankan secara native di Windows.");
            SectionLabel("Identitas");
            SettingsRow("Edisi Windows 0.2","Panel dan Settings dirancang ulang mendekati aplikasi macOS.","Preview");
            SettingsRow("Lisensi GPL-3.0-or-later","Atribusi proyek sumber dipertahankan.","Open source");
            SettingsRow("Repository GitHub","Source C# Windows Edition dalam folder Windows/ repo Space7.","GitHub",()=>Open("https://github.com/dahlah77/space7"));
            SettingsRow("Modul belum setara 1:1","Beberapa fitur bergantung pada API khusus macOS.","Bertahap");
        }
        void Jump(int tab){owner.SelectTab(tab);Close();}
        void Open(string file){try{Process.Start(new ProcessStartInfo(file){UseShellExecute=true});}catch(Exception e){MessageBox.Show(e.Message,"Space7");}}
    }
    public sealed class Layout7:Form {
        [DllImport("user32.dll")]static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int w,int h2,uint f);
        readonly ComboBox target=new ComboBox();
        readonly List<Tuple<string,IntPtr>> targets=new List<Tuple<string,IntPtr>>();
        readonly Rectangle screen=Screen.PrimaryScreen.WorkingArea;
        public Layout7(){
            Text="Space7 • Tata letak jendela";Size=new Size(490,320);StartPosition=FormStartPosition.CenterScreen;
            BackColor=Style7.Back;ForeColor=Style7.Text;Font=Style7.Font(10);
            Controls.Add(Style7.L("Pilih jendela untuk diatur",18,14,420,35,14,true));
            target.Location=new Point(18,56);target.Width=439;target.DropDownStyle=ComboBoxStyle.DropDownList;target.BackColor=Style7.Card;target.ForeColor=Style7.Text;Controls.Add(target);
            foreach(Process p in Process.GetProcesses())try{
                if(p.MainWindowHandle!=IntPtr.Zero&&!String.IsNullOrWhiteSpace(p.MainWindowTitle)&&p.ProcessName!="Space7"){
                    targets.Add(Tuple.Create(p.ProcessName+"  —  "+p.MainWindowTitle,p.MainWindowHandle)); target.Items.Add(targets.Last().Item1);
                }
            }catch{}finally{p.Dispose();}
            if(target.Items.Count>0)target.SelectedIndex=0;
            string[] actions={"Kiri 50%","Kanan 50%","Atas 50%","Bawah 50%","Tengah","Maksimalkan","Kiri 33%","Kanan 33%","Kanan atas","Kiri atas","Kanan bawah","Kiri bawah"};
            for(int i=0;i<actions.Length;i++){int ix=i;Button b=Style7.B(actions[i],139,34,(s,e)=>Arrange(ix),i<2);
                b.Location=new Point(18+(i%3)*146,104+(i/3)*43);Controls.Add(b);}
        }
        void Arrange(int index){
            if(target.SelectedIndex<0){MessageBox.Show("Pilih jendela terlebih dahulu.");return;}
            var r=screen;int x=r.Left,y=r.Top,w=r.Width,h=r.Height;
            switch(index){
                case 0:w/=2;break;case 1:x+=w/2;w-=w/2;break;
                case 2:h/=2;break;case 3:y+=h/2;h-=h/2;break;
                case 4:w=(int)(w*.70);h=(int)(h*.75);x=r.Left+(r.Width-w)/2;y=r.Top+(r.Height-h)/2;break;
                case 5:break;case 6:w/=3;break;case 7:x+=w*2/3;w-=w*2/3;break;
                case 8:x+=w/2;w-=w/2;h/=2;break;case 9:w/=2;h/=2;break;
                case 10:x+=w/2;y+=h/2;w-=w/2;h-=h/2;break;case 11:y+=h/2;w/=2;h-=h/2;break;
            }
            IntPtr hwnd=targets[target.SelectedIndex].Item2;
            if(!SetWindowPos(hwnd,IntPtr.Zero,x,y,w,h,0x0004|0x0010))MessageBox.Show("Perlu izin untuk mengatur jendela ini.","Space7");
        }
    }
    public sealed class UrlCleaner7:Form {
        TextBox input,output;
        public UrlCleaner7() {
            Text="Space7 • Bersihkan URL";StartPosition=FormStartPosition.CenterScreen;Size=new Size(620,415);
            BackColor=Style7.Back;ForeColor=Style7.Text;Font=Style7.Font(10);
            Controls.Add(Style7.L("Bersihkan URL pelacakan",18,17,555,39,17,true));
            Controls.Add(Style7.L("Mempertahankan domain, path, anchor dan parameter yang bukan pelacak.",18,52,555,29,9.8f,false,Style7.Sub));
            input=new TextBox{Location=new Point(18,91),Size=new Size(564,70),Multiline=true,BackColor=Style7.Card,ForeColor=Style7.Text,ScrollBars=ScrollBars.Vertical};
            output=new TextBox{Location=new Point(18,215),Size=new Size(564,80),Multiline=true,BackColor=Style7.Card,ForeColor=Style7.Text,ScrollBars=ScrollBars.Vertical};
            Controls.Add(input);Controls.Add(output);
            var a=Style7.B("Baca Clipboard",162,35,(s,e)=>{if(Clipboard.ContainsText())input.Text=Clipboard.GetText();});a.Location=new Point(18,171);Controls.Add(a);
            var b=Style7.B("Bersihkan URL",168,35,(s,e)=>Clean(),true);b.Location=new Point(190,171);Controls.Add(b);
            var c=Style7.B("Salin hasil",145,35,(s,e)=>{if(!String.IsNullOrWhiteSpace(output.Text))Clipboard.SetText(output.Text);});c.Location=new Point(18,306);Controls.Add(c);
        }
        void Clean(){
            try {
                Uri uri;if(!Uri.TryCreate(input.Text.Trim(),UriKind.Absolute,out uri)||!(uri.Scheme=="http"||uri.Scheme=="https")){MessageBox.Show("Masukkan URL http/https yang valid.","Space7");return;}
                UriBuilder b=new UriBuilder(uri);
                string[] banned={"fbclid","gclid","dclid","msclkid","igshid","mc_cid","mc_eid","ref_src","utm_source","utm_medium","utm_campaign","utm_term","utm_content","utm_id"};
                var keep=(uri.Query.TrimStart('?').Split('&')).Where(v=>!String.IsNullOrWhiteSpace(v)).Where(v=>{
                    string name=v.Split('=')[0];return !name.StartsWith("utm_",StringComparison.OrdinalIgnoreCase)&&!banned.Contains(name,StringComparer.OrdinalIgnoreCase);
                });
                b.Query=String.Join("&",keep.ToArray());output.Text=b.Uri.AbsoluteUri;
            } catch(Exception ex){MessageBox.Show(ex.Message,"Space7");}
        }
    }
}
