using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using SharpCompress.Archives.GZip;
using SharpCompress.Archives.Rar;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Archives.Tar;
using SharpCompress.Archives.Zip;
using SharpCompress.Compressors.Xz;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CJCMCG_Tool
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            Width = 300;
            Height = 110;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        static string filein, fileout;

        async private void SelectFile(object s, RoutedEventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            FileDialogFilter filter = new FileDialogFilter();
            filter.Name = "Midi file or zipped file";
            filter.Extensions.Add("mid");
            filter.Extensions.Add("zip");
            filter.Extensions.Add("7z");
            filter.Extensions.Add("xz");
            filter.Extensions.Add("gz");
            filter.Extensions.Add("tar");
            filter.Extensions.Add("rar");
            open.Filters.Add(filter);
            open.AllowMultiple = false;
            var show = await open.ShowAsync(this);
            if (show.Length > 0)
            {
                filein = show[0];
                Button button = (Button)s;
                if (filein.Length < 30) button.Content = filein;
                else button.Content = "..." + filein.Substring(filein.Length - 30);
                Button butt = this.FindControl<Button>("progress");
                butt.IsEnabled = true;
            }
        }

        async private void Outputfile(object s, RoutedEventArgs e)
        {
            SaveFileDialog save = new SaveFileDialog();
            FileDialogFilter filter = new FileDialogFilter();
            filter.Name = "CJCMCG file";
            filter.Extensions.Add("cjcmcg");
            save.Filters.Add(filter);
            var show = await save.ShowAsync(this);
            if (show == null) return;
            fileout = show;
            progress = this.FindControl<Button>("progress");
            Thread thread = new Thread(Render);
            thread.Start();
        }

        struct pairli
        {
            public long x;
            public int y;
            public int trk, cnt;
            public pairli(long a, int b, int c, int d)
            {
                x = a;
                y = b;
                trk = c;
                cnt = d;
            }
            public static bool operator <(pairli fx, pairli fy)
            {
                if (fx.x != fy.x)
                {
                    return fx.x < fy.x;
                }
                else if (fx.trk != fy.trk)
                {
                    return fx.trk < fy.trk;
                }
                else if (fx.cnt != fy.cnt)
                {
                    return fx.cnt < fy.cnt;
                }
                else
                {
                    return false;
                }
            }
            public static bool operator >(pairli fx, pairli fy)
            {
                if (fx.x != fy.x)
                {
                    return fx.x > fy.x;
                }
                else if (fx.trk != fy.trk)
                {
                    return fx.trk > fy.trk;
                }
                else if (fx.cnt != fy.cnt)
                {
                    return fx.cnt > fy.cnt;
                }
                else
                {
                    return false;
                }
            }
        }
        struct pairls
        {
            public long x;
            public String y;
            public int trk, cnt;
            public pairls(long a, String b, int c, int d)
            {
                x = a;
                y = b;
                trk = c;
                cnt = d;
            }
            public static bool operator <(pairls fx, pairls fy)
            {
                if (fx.x != fy.x)
                {
                    return fx.x < fy.x;
                }
                else if (fx.trk != fy.trk)
                {
                    return fx.trk < fy.trk;
                }
                else if (fx.cnt != fy.cnt)
                {
                    return fx.cnt < fy.cnt;
                }
                else
                {
                    return false;
                }
            }
            public static bool operator >(pairls fx, pairls fy)
            {
                if (fx.x != fy.x)
                {
                    return fx.x > fy.x;
                }
                else if (fx.trk != fy.trk)
                {
                    return fx.trk > fy.trk;
                }
                else if (fx.cnt != fy.cnt)
                {
                    return fx.cnt > fy.cnt;
                }
                else
                {
                    return false;
                }
            }
        }

        static bool CanDec(string s)
        {
            return s.EndsWith(".mid") || s.EndsWith(".xz") || s.EndsWith(".zip") || s.EndsWith(".7z") || s.EndsWith(".rar") || s.EndsWith(".tar") || s.EndsWith(".gz");
        }
        static Stream AddXZLayer(Stream input)
        {
            try
            {
                Process xz = new Process();
                xz.StartInfo = new ProcessStartInfo("xz", "-dc --threads=0")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                xz.Start();
                Task.Run(() =>
                {
                    input.CopyTo(xz.StandardInput.BaseStream);
                    xz.StandardInput.Close();
                });
                return xz.StandardOutput.BaseStream;
            }
            catch (Exception)
            {
                Console.WriteLine("xz not found, trying internal decompress with lower speed and lower compatibility...");
                return new XZStream(input);
            }
        }
        static Stream AddZipLayer(Stream input)
        {
            var zip = ZipArchive.Open(input);
            foreach (var entry in zip.Entries)
            {
                if (CanDec(entry.Key))
                {
                    filein = entry.Key;
                    return entry.OpenEntryStream();
                }
            }
            throw new Exception("No compatible file found in the .zip");
        }
        static Stream AddRarLayer(Stream input)
        {
            var zip = RarArchive.Open(input);
            foreach (var entry in zip.Entries)
            {
                if (CanDec(entry.Key))
                {
                    filein = entry.Key;
                    return entry.OpenEntryStream();
                }
            }
            throw new Exception("No compatible file found in the .rar");
        }
        static Stream Add7zLayer(Stream input)
        {
            var zip = SevenZipArchive.Open(input);
            foreach (var entry in zip.Entries)
            {
                if (CanDec(entry.Key))
                {
                    filein = entry.Key;
                    return entry.OpenEntryStream();
                }
            }
            throw new Exception("No compatible file found in the .7z");
        }
        static Stream AddTarLayer(Stream input)
        {
            var zip = TarArchive.Open(input);
            foreach (var entry in zip.Entries)
            {
                if (CanDec(entry.Key))
                {
                    filein = entry.Key;
                    return entry.OpenEntryStream();
                }
            }
            throw new Exception("No compatible file found in the .tar");
        }
        static Stream AddGZLayer(Stream input)
        {
            var zip = GZipArchive.Open(input);
            foreach (var entry in zip.Entries)
            {
                if (CanDec(entry.Key))
                {
                    filein = entry.Key;
                    return entry.OpenEntryStream();
                }
            }
            throw new Exception("No compatible file found in the .gz");
        }

        private class AhhShitPairliCompare : System.Collections.IComparer
        {
            public int Compare(object x, object y)
            {
                pairli X = (pairli)x, Y = (pairli)y;
                if (X < Y)
                {
                    return -1;
                }
                else if (X > Y)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }
        private class AhhShitPairlsCompare : System.Collections.IComparer
        {
            public int Compare(object x, object y)
            {
                pairls X = (pairls)x, Y = (pairls)y;
                if (X < Y)
                {
                    return -1;
                }
                else if (X > Y)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }
        static int toint(int x)
        {
            return x < 0 ? x + 256 : x;
        }

        Button progress;

        async private void Render()
        {
            try
            {
                if (!fileout.EndsWith(".cjcmcg")) fileout += ".cjcmcg";
                Stream inppp = File.Open(filein, FileMode.Open, FileAccess.Read, FileShare.Read);
                while (!filein.EndsWith(".mid"))
                {
                    if (filein.EndsWith(".xz"))
                    {
                        inppp = AddXZLayer(inppp);
                        filein = filein.Substring(0, filein.Length - 3);
                    }
                    else if (filein.EndsWith(".zip"))
                    {
                        inppp = AddZipLayer(inppp);
                    }
                    else if (filein.EndsWith(".rar"))
                    {
                        inppp = AddRarLayer(inppp);
                    }
                    else if (filein.EndsWith(".7z"))
                    {
                        inppp = Add7zLayer(inppp);
                    }
                    else if (filein.EndsWith(".tar"))
                    {
                        inppp = AddTarLayer(inppp);
                    }
                    else if (filein.EndsWith(".gz"))
                    {
                        inppp = AddGZLayer(inppp);
                    }
                    else if (!filein.EndsWith(".mid"))
                    {
                        throw new Exception("No compatible file found");
                    }
                }
                await Dispatcher.UIThread.InvokeAsync(new Action(() =>
                {
                    progress.IsEnabled = false;
                }));
                BufferedStream inpp = new BufferedStream(inppp, 16777216);
                int ReadByte()
                {
                    int b = inpp.ReadByte();
                    if (b == -1) throw new Exception("Unexpected file end");
                    return b;
                }
                for (int i = 0; i < 4; ++i)
                {
                    ReadByte();
                }
                for (int i = 0; i < 4; ++i)
                {
                    ReadByte();
                }
                ReadByte();
                ReadByte();
                int trkcnt, resol;
                trkcnt = (toint(ReadByte()) * 256) + toint(ReadByte());
                resol = (toint(ReadByte()) * 256) + toint(ReadByte());
                ArrayList bpm = new ArrayList();
                bpm.Add(new pairli(0, 500000, -1, 0));
                long noteall = 0;
                int nowtrk = 1;
                int alltic = 0;
                int allticreal = 0;
                List<long> nts = new List<long>(), nto = new List<long>();
                ArrayList lrcs = new ArrayList();
                lrcs.Add(new pairls(0, "", -1, -1));
                for (int trk = 0; trk < trkcnt; trk++)
                {
                    int bpmcnt = 0;
                    int lrccnt = 0;
                    long notes = 0;
                    long leng = 0;
                    ReadByte();
                    ReadByte();
                    ReadByte();
                    ReadByte();
                    for (int i = 0; i < 4; i++)
                    {
                        leng = leng * 256 + toint(ReadByte());
                    }
                    int lstcmd = 256;
                    string str = "Parsing track {trackcount}, size {tracksize}.";
                    str = str.Replace("{trackcount}", (trk + 1).ToString() + "/" + trkcnt.ToString()).Replace("{tracksize}", leng.ToString("N0"));
                    await Dispatcher.UIThread.InvokeAsync(new Action(() =>
                    {
                        progress.Content = str;
                    }));
                    int getnum()
                    {
                        int ans = 0;
                        int ch = 256;
                        while (ch >= 128)
                        {
                            ch = toint(ReadByte());
                            leng--;
                            ans = ans * 128 + (ch & 0b01111111);
                        }
                        return ans;
                    }
                    int get()
                    {
                        if (lstcmd != 256)
                        {
                            int lstcmd2 = lstcmd;
                            lstcmd = 256;
                            return lstcmd2;
                        }
                        leng--;
                        return toint(ReadByte());
                    }
                    int TM = 0;
                    int prvcmd = 256;
                    while (true)
                    {
                        int added = getnum();
                        TM += added;
                        int cmd = ReadByte();
                        leng--;
                        if (cmd < 128)
                        {
                            lstcmd = cmd;
                            cmd = prvcmd;
                        }
                        prvcmd = cmd;
                        int cm = cmd & 0b11110000;
                        if (cm == 0b10010000)
                        {
                            get();
                            ReadByte();
                            leng--;
                            while (nts.Count <= TM)
                            {
                                nts.Add(0L);
                            }
                            while (nto.Count <= TM)
                            {
                                nto.Add(0L);
                            }
                            nts[TM] = (Convert.ToInt64(nts[TM]) + 1L);
                            notes++;
                        }
                        else if (cm == 0b10000000)
                        {
                            get();
                            ReadByte();
                            leng--;
                            while (nts.Count <= TM)
                            {
                                nts.Add(0L);
                            }
                            while (nto.Count <= TM)
                            {
                                nto.Add(0L);
                            }
                            nto[TM] = (Convert.ToInt64(nto[TM]) + 1L);
                        }
                        else if (cm == 0b11000000 || cm == 0b11010000 || cmd == 0b11110011)
                        {
                            get();
                        }
                        else if (cm == 0b11100000 || cm == 0b10110000 || cmd == 0b11110010 || cm == 0b10100000)
                        {
                            get();
                            ReadByte();
                            leng--;
                        }
                        else if (cmd == 0b11110000)
                        {
                            if (get() == 0b11110111)
                            {
                                continue;
                            }
                            do
                            {
                                leng--;
                            } while (ReadByte() != 0b11110111);
                        }
                        else if (cmd == 0b11110100 || cmd == 0b11110001 || cmd == 0b11110101 || cmd == 0b11111001 || cmd == 0b11111101 || cmd == 0b11110110 || cmd == 0b11110111 || cmd == 0b11111000 || cmd == 0b11111010 || cmd == 0b11111100 || cmd == 0b11111110)
                        {
                        }
                        else if (cmd == 0b11111111)
                        {
                            cmd = get();
                            if (cmd == 0)
                            {
                                ReadByte(); ReadByte(); ReadByte();
                                leng -= 3;
                            }
                            else if (cmd >= 1 && cmd <= 10 && cmd != 5 || cmd == 0x7f)
                            {
                                long ff = getnum();
                                while (ff-- > 0)
                                {
                                    ReadByte();
                                    leng--;
                                }
                            }
                            else if (cmd == 0x20 || cmd == 0x21)
                            {
                                ReadByte(); ReadByte(); leng -= 2;
                            }
                            else if (cmd == 0x2f)
                            {
                                ReadByte();
                                leng--;
                                if (TM > allticreal)
                                {
                                    allticreal = TM;
                                }
                                TM -= added;
                                break;
                            }
                            else if (cmd == 0x51)
                            {
                                bpmcnt++;
                                ReadByte();
                                leng--;
                                int BPM = get();
                                BPM = BPM * 256 + get();
                                BPM = BPM * 256 + get();
                                bpm.Add(new pairli(TM, BPM, trk, bpmcnt));
                            }
                            else if (cmd == 5)
                            {
                                Encoding gb2312 = Encoding.GetEncoding("GBK");
                                Encoding def = Encoding.GetEncoding("UTF-8");
                                lrccnt++;
                                int ff = (int)getnum();
                                byte[] S = new byte[ff];
                                int cnt = 0;
                                while (ff-- > 0)
                                {
                                    S[cnt++] = Convert.ToByte(ReadByte());
                                    leng--;
                                }
                                S = Encoding.Convert(gb2312, def, S);
                                lrcs.Add(new pairls(TM, def.GetString(S), trk, lrccnt));
                            }
                            else if (cmd == 0x54 || cmd == 0x58)
                            {
                                ReadByte(); ReadByte(); ReadByte(); ReadByte(); ReadByte();
                                leng -= 5;
                            }
                            else if (cmd == 0x59)
                            {
                                ReadByte(); ReadByte(); ReadByte();
                                leng -= 3;
                            }
                            else if (cmd == 0x0a)
                            {
                                int ss = get();
                                while (ss-- > 0)
                                {
                                    ReadByte();
                                    leng--;
                                }
                            }
                        }
                    }
                    while (leng > 0)
                    {
                        ReadByte();
                        leng--;
                    }
                    noteall += notes;
                    nowtrk++;
                }
                alltic = nto.Count;
                string strs = "{notecnt} notes. Writing data...";
                strs = strs.Replace("{notecnt}", noteall.ToString("N0"));
                await Dispatcher.UIThread.InvokeAsync(new Action(() =>
                {
                    progress.Content = strs;
                }));
                bpm.Sort(new AhhShitPairliCompare());
                lrcs.Sort(new AhhShitPairlsCompare());
                BufferedStream outs = new BufferedStream(File.Open(fileout, FileMode.Create, FileAccess.Write, FileShare.Write), 16777216);
                outs.WriteByte((byte)(resol / 256 / 256 / 256));
                outs.WriteByte((byte)(resol / 256 / 256 % 256));
                outs.WriteByte((byte)(resol / 256 % 256));
                outs.WriteByte((byte)(resol % 256));
                int bpms = bpm.Count;
                outs.WriteByte((byte)(bpms / 256 / 256 / 256));
                outs.WriteByte((byte)(bpms / 256 / 256 % 256));
                outs.WriteByte((byte)(bpms / 256 % 256));
                outs.WriteByte((byte)(bpms % 256));
                for (int i = 0; i < bpms; i++)
                {
                    pairli pr = (pairli)bpm[i];
                    outs.WriteByte((byte)(pr.x / 256 / 256 / 256));
                    outs.WriteByte((byte)(pr.x / 256 / 256 % 256));
                    outs.WriteByte((byte)(pr.x / 256 % 256));
                    outs.WriteByte((byte)(pr.x % 256));
                    outs.WriteByte((byte)(pr.y / 256 / 256 % 256));
                    outs.WriteByte((byte)(pr.y / 256 % 256));
                    outs.WriteByte((byte)(pr.y % 256));
                }
                int lrcc = lrcs.Count;
                outs.WriteByte((byte)(lrcc / 256 / 256 / 256));
                outs.WriteByte((byte)(lrcc / 256 / 256 % 256));
                outs.WriteByte((byte)(lrcc / 256 % 256));
                outs.WriteByte((byte)(lrcc % 256));
                for (int i = 0; i < lrcc; i++)
                {
                    pairls pr = (pairls)lrcs[i];
                    outs.WriteByte((byte)(pr.x / 256 / 256 / 256));
                    outs.WriteByte((byte)(pr.x / 256 / 256 % 256));
                    outs.WriteByte((byte)(pr.x / 256 % 256));
                    outs.WriteByte((byte)(pr.x % 256));
                    byte[] byteArray = Encoding.UTF8.GetBytes(pr.y);
                    int ys = byteArray.Length;
                    outs.WriteByte((byte)(ys / 256 / 256 / 256));
                    outs.WriteByte((byte)(ys / 256 / 256 % 256));
                    outs.WriteByte((byte)(ys / 256 % 256));
                    outs.WriteByte((byte)(ys % 256));
                    outs.Write(byteArray, 0, ys);
                }
                long xi = noteall;
                List<byte> list = new List<byte>();
                bool started = false;
                if (xi == 0) list.Add(0);
                while (xi > 0)
                {
                    byte nx = (byte)(xi % 128);
                    xi /= 128;
                    if (started) nx += 128;
                    started = true;
                    list.Add(nx);
                }
                list.Reverse();
                for (int j = 0; j < list.Count; j++) outs.WriteByte(list[j]);
                int mids = nts.Count;
                outs.WriteByte((byte)(mids / 256 / 256 / 256));
                outs.WriteByte((byte)(mids / 256 / 256 % 256));
                outs.WriteByte((byte)(mids / 256 % 256));
                outs.WriteByte((byte)(mids % 256));
                for (int i = 0; i < mids; i++)
                {
                    long x = nts[i], y = nto[i];
                    list = new List<byte>();
                    started = false;
                    if (x == 0) list.Add(0);
                    while (x > 0)
                    {
                        byte nx = (byte)(x % 128);
                        x /= 128;
                        if (started) nx += 128;
                        started = true;
                        list.Add(nx);
                    }
                    list.Reverse();
                    for (int j = 0; j < list.Count; j++) outs.WriteByte(list[j]);
                    started = false;
                    list.Clear();
                    if (y == 0) list.Add(0);
                    while (y > 0)
                    {
                        byte ny = (byte)(y % 128);
                        y /= 128;
                        if (started) ny += 128;
                        started = true;
                        list.Add(ny);
                    }
                    list.Reverse();
                    for (int j = 0; j < list.Count; j++) outs.WriteByte(list[j]);
                }
                outs.Flush();
                outs.Close();
                await Dispatcher.UIThread.InvokeAsync(new Action(() =>
                {
                    progress.IsEnabled = true;
                    progress.Content = "Completed.";
                }));
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
                await Dispatcher.UIThread.InvokeAsync(new Action(() =>
                {
                    progress.IsEnabled = true;
                    progress.Content = "An error occured.";
                }));
            }
        }
    }
}
