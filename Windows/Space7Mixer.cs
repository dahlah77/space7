// Space7 Windows Edition: native Windows per-application volume mixer
// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Space7Windows {
    public sealed partial class MacPanel {
        void PageMixerNative(){
            RoundedPanel master=Card(119);
            master.Controls.Add(Style7.L("Suara sistem",14,7,270,30,11.7f,true));
            master.Controls.Add(Style7.L("Output utama Windows",14,34,282,22,9,false,Style7.Sub));
            TrackBar bar=new TrackBar{Location=new Point(13,59),Size=new Size(247,35),Maximum=100,Minimum=0,TickStyle=TickStyle.None,Value=Math.Max(0,Math.Min(100,Audio7.GetVolume())),BackColor=Style7.Card};
            Label number=Style7.L(bar.Value+"%",263,57,54,35,11,true);number.TextAlign=ContentAlignment.MiddleRight;
            master.Controls.Add(bar);master.Controls.Add(number);
            bar.Scroll+=(s,e)=>{Audio7.SetVolume(bar.Value);number.Text=bar.Value+"%";};
            var mute=Style7.B("Bisu / Suarakan",294,24,(s,e)=>{Audio7.ToggleMute();});
            mute.Location=new Point(15,91);master.Controls.Add(mute);
            Heading("Aplikasi");
            List<NativeAudioSessions.Session> sessions=new List<NativeAudioSessions.Session>();
            try{sessions=NativeAudioSessions.List();}
            catch(Exception ex){Item("Mixer audio","Tidak dapat membaca sesi: "+ex.Message,"☷",()=>Open("sndvol.exe"),86);}
            if(sessions.Count==0)Item("Belum ada sesi audio","Putar suara dari aplikasi untuk menampilkan slider.","♫",()=>Open("sndvol.exe"),92);
            int shown=0;foreach(var session in sessions){
                if(shown++>=15)break;var current=session;
                RoundedPanel c=Card(108);c.Controls.Add(Style7.L("♫",13,17,30,34,19,true,Style7.Accent));
                string name=String.IsNullOrWhiteSpace(session.Name)?"Aplikasi "+session.ProcessId:session.Name;
                Label title=Style7.L(name,48,9,229,29,11,true);c.Controls.Add(title);
                var slider=new TrackBar{Location=new Point(47,43),Size=new Size(191,31),Minimum=0,Maximum=100,Value=session.Volume,
                    TickStyle=TickStyle.None,BackColor=Style7.Card};c.Controls.Add(slider);
                var pc=Style7.L(session.Volume+"%",244,44,62,28,10,true);pc.TextAlign=ContentAlignment.MiddleRight;c.Controls.Add(pc);
                slider.Scroll+=(s,e)=>{
                    bool ok=NativeAudioSessions.SetVolume(current,slider.Value);
                    pc.Text=ok?slider.Value+"%":"Gagal";
                };
                Switch7 muteSwitch=new Switch7(!session.Muted){Location=new Point(263,75),Size=new Size(42,24)};c.Controls.Add(muteSwitch);
                muteSwitch.Changed+=(s,e)=>NativeAudioSessions.SetMute(current,!muteSwitch.State);
                c.Controls.Add(Style7.L("Aktif",49,76,116,22,8.7f,false,Style7.Sub));
            }
            Item("Perbarui daftar aplikasi","Pindai ulang sesi audio yang sedang aktif.","⟳",()=>Render(1),76);
            Item("Mixer Windows","Pengaturan lanjut output dan tiap aplikasi.","☷",()=>Open("ms-settings:apps-volume"),75);
        }
    }
    internal static class NativeAudioSessions {
        public class Session { public int ProcessId; public string Name; public int Volume; public bool Muted; internal ISimpleAudioVolume Vol; }
        [ComImport,Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]sealed class EnumeratorCom{}
        [ComImport,Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IEnum {
            [PreserveSig]int EnumAudioEndpoints(int d,uint mask,out IntPtr devices);
            [PreserveSig]int GetDefaultAudioEndpoint(int dataFlow,int role,out IDevice device);
            [PreserveSig]int GetDevice([MarshalAs(UnmanagedType.LPWStr)]string id,out IDevice device);
            [PreserveSig]int RegisterEndpointNotificationCallback(IntPtr cb);
            [PreserveSig]int UnregisterEndpointNotificationCallback(IntPtr cb);
        }
        [ComImport,Guid("D666063F-1587-4E43-81F1-B948E807363F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IDevice {
            [PreserveSig]int Activate([MarshalAs(UnmanagedType.LPStruct)]Guid iid,int ctx,IntPtr p,[MarshalAs(UnmanagedType.IUnknown)]out object iface);
            [PreserveSig]int OpenPropertyStore(int a,out IntPtr store);
            [PreserveSig]int GetId([MarshalAs(UnmanagedType.LPWStr)]out string id);
            [PreserveSig]int GetState(out int state);
        }
        [ComImport,Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface ISessionManager {
            [PreserveSig]int GetAudioSessionControl(ref Guid guid,int flags,out IntPtr result);
            [PreserveSig]int GetSimpleAudioVolume(ref Guid guid,int flags,out IntPtr result);
            [PreserveSig]int GetSessionEnumerator(out ISessionEnum en);
            [PreserveSig]int RegisterSessionNotification(IntPtr notification);
            [PreserveSig]int UnregisterSessionNotification(IntPtr notification);
            [PreserveSig]int RegisterDuckNotification(IntPtr sessionId,IntPtr notification);
            [PreserveSig]int UnregisterDuckNotification(IntPtr notification);
        }
        [ComImport,Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface ISessionEnum {
            [PreserveSig]int GetCount(out int count);
            [PreserveSig]int GetSession(int index,out ISessionControl ctl);
        }
        [ComImport,Guid("F4B1A599-7266-4319-A8CA-E70ACB11E8CD"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface ISessionControl {
            [PreserveSig]int GetState(out int state);
            [PreserveSig]int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)]out string name);
            [PreserveSig]int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)]string name,ref Guid ctx);
            [PreserveSig]int GetIconPath([MarshalAs(UnmanagedType.LPWStr)]out string path);
            [PreserveSig]int SetIconPath([MarshalAs(UnmanagedType.LPWStr)]string path,ref Guid ctx);
            [PreserveSig]int GetGroupingParam(out Guid guid);
            [PreserveSig]int SetGroupingParam(ref Guid guid,ref Guid ctx);
            [PreserveSig]int RegisterAudioSessionNotification(IntPtr events);
            [PreserveSig]int UnregisterAudioSessionNotification(IntPtr events);
        }
        [ComImport,Guid("BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface ISessionControl2 {
            [PreserveSig]int GetState(out int state);
            [PreserveSig]int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)]out string name);
            [PreserveSig]int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)]string name,ref Guid ctx);
            [PreserveSig]int GetIconPath([MarshalAs(UnmanagedType.LPWStr)]out string path);
            [PreserveSig]int SetIconPath([MarshalAs(UnmanagedType.LPWStr)]string path,ref Guid ctx);
            [PreserveSig]int GetGroupingParam(out Guid guid);
            [PreserveSig]int SetGroupingParam(ref Guid guid,ref Guid ctx);
            [PreserveSig]int RegisterAudioSessionNotification(IntPtr events);
            [PreserveSig]int UnregisterAudioSessionNotification(IntPtr events);
            [PreserveSig]int GetSessionIdentifier(out IntPtr id);
            [PreserveSig]int GetSessionInstanceIdentifier(out IntPtr id);
            [PreserveSig]int GetProcessId(out uint pid);
            [PreserveSig]int IsSystemSoundsSession();
            [PreserveSig]int SetDuckingPreference([MarshalAs(UnmanagedType.Bool)]bool val);
        }
        [ComImport,Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface ISimpleAudioVolume {
            [PreserveSig]int SetMasterVolume(float vol,ref Guid ctx);
            [PreserveSig]int GetMasterVolume(out float volume);
            [PreserveSig]int SetMute([MarshalAs(UnmanagedType.Bool)]bool mute,ref Guid ctx);
            [PreserveSig]int GetMute([MarshalAs(UnmanagedType.Bool)]out bool muted);
        }
        public static List<Session> List(){
            List<Session> found=new List<Session>();
            IEnum en=(IEnum)(object)new EnumeratorCom();
            IDevice device;int hr=en.GetDefaultAudioEndpoint(0,1,out device);if(hr!=0)Marshal.ThrowExceptionForHR(hr);
            object obj;Guid iid=new Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F");
            hr=device.Activate(iid,23,IntPtr.Zero,out obj);if(hr!=0)Marshal.ThrowExceptionForHR(hr);
            ISessionManager manager=(ISessionManager)obj;ISessionEnum sesEnum;
            hr=manager.GetSessionEnumerator(out sesEnum);if(hr!=0)Marshal.ThrowExceptionForHR(hr);
            int count;sesEnum.GetCount(out count);
            for(int i=0;i<Math.Min(120,count);i++){
                ISessionControl control;hr=sesEnum.GetSession(i,out control);if(hr!=0||control==null)continue;
                try{
                    int state;control.GetState(out state);if(state==2)continue;
                    ISimpleAudioVolume volume=control as ISimpleAudioVolume;if(volume==null)continue;
                    float v=0;bool mute=false;volume.GetMasterVolume(out v);volume.GetMute(out mute);
                    int pid=0;try {var c2=(ISessionControl2)control;uint p;c2.GetProcessId(out p);pid=(int)p;}catch{}
                    string name="";try{control.GetDisplayName(out name);}catch{}
                    if(String.IsNullOrWhiteSpace(name)&&pid>0)try{
                        using(Process p=Process.GetProcessById(pid))name=p.ProcessName;
                    }catch{}
                    if(String.IsNullOrWhiteSpace(name))name="Suara sistem";
                    found.Add(new Session{ProcessId=pid,Name=name,Volume=Math.Max(0,Math.Min(100,(int)Math.Round(v*100))),Muted=mute,Vol=volume});
                }catch{}
            }
            return found;
        }
        public static bool SetVolume(Session s,int val){try{Guid g=Guid.Empty;return s.Vol!=null&&s.Vol.SetMasterVolume(Math.Max(0,Math.Min(100,val))/100f,ref g)==0;}catch{return false;}}
        public static bool SetMute(Session s,bool value){try{Guid g=Guid.Empty;return s.Vol!=null&&s.Vol.SetMute(value,ref g)==0;}catch{return false;}}
    }
}
