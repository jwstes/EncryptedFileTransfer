using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Reflection;
using MetroFramework;

namespace LocalFileTransfer
{
    public partial class Form1 : MetroFramework.Forms.MetroForm
    {
        Socket f;
        Client x;
        
        public NotifyIcon notifyicon;
        public Form1()
        {
            InitializeComponent();
        }
        string dir = Environment.CurrentDirectory + "\\FilesReceived\\";
        void notifyicon_BalloonTipClicked(object sender, EventArgs e)
        {
            //open the folder location.
            System.Diagnostics.Process.Start(dir);
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            Directory.CreateDirectory(dir);
            FieldInfo iconField = typeof(Form).GetField("defaultIcon", BindingFlags.NonPublic | BindingFlags.Static);
            Icon myIcon = (Icon)iconField.GetValue(iconField);
            notifyicon = notifyIcon1;
            notifyicon.Text = "LocalFileTransfer";
            notifyicon.Visible = true;
            notifyicon.BalloonTipClicked += notifyicon_BalloonTipClicked;
            notifyicon.Icon = myIcon;
            label1.AllowDrop = true;
            
            FEvents.connectbutton += FEvents_connectbutton;
            FEvents.ip += FEvents_ip;
            FEvents.list += FEvents_list;
            FEvents.progressbar += FEvents_progressbar;
            FEvents.status += FEvents_status;
            FEvents.title += FEvents_title;
            FEvents.drop += FEvents_drop;
            Utilities.Tip += Utilities_Tip;
        }

        void Utilities_Tip(BalloonTipE e)
        {
            this.Invoke((MethodInvoker)(() => { notifyIcon1.BalloonTipText = e.Message; notifyIcon1.BalloonTipTitle = e.Title; notifyIcon1.BalloonTipIcon = e.Icon; notifyIcon1.ShowBalloonTip(10000); }));
        }

        public string LocalIPAddress()
        {
            IPHostEntry host;
            string localIP = "";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    break;
                }
            }
            return localIP;
        }

        private void label2_DragEnter(object sender, DragEventArgs e)
        {
            if ((e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent(DataFormats.Text) || e.Data.GetDataPresent(DataFormats.Html)) && ((x != null && x.Connected) || Utilities.clientx.Count > 0))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }
        private void label2_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                new Thread(new ThreadStart(delegate()
                    {
                        string[] FileList = (string[])e.Data.GetData(DataFormats.FileDrop, false);
                        foreach (string i in FileList)
                        {
                            if (Directory.Exists(i))
                            {
                            }
                            else
                            {
                                if (x != null && x.Connected)
                                {
                                    x.Proto.SendFile(i);
                                }
                                if (Utilities.clientx.Count > 0)
                                {
                                    foreach (Client cli in Utilities.clientx)
                                    {
                                        cli.Proto.SendFile(i);
                                    }
                                }
                            }
                        }
                    })).Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void label2_DragOver(object sender, DragEventArgs e)
        {
            if ((e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent(DataFormats.Text) || e.Data.GetDataPresent(DataFormats.Html)) && ((x != null && x.Connected) || Utilities.clientx.Count > 0))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Environment.Exit(0);
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {

        }

        private void disconnectClientToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItems.Count > 0)
            {
                string ip = listBox1.SelectedItems[0].ToString();
                foreach (Client cl in Utilities.clientx)
                {
                    if (cl.RemoteIp == ip)
                    {
                        listBox1.Items.Remove(cl.RemoteIp);
                        cl.Disconnect();
                        Utilities.clientx.Remove(cl);
                        break;
                    }
                }
            }
        }
        Thread th;
        

       

        void FEvents_drop(ChangedEvent e)
        {
            this.Invoke((MethodInvoker)(() => this.label2.Enabled = (bool)e.Message));
        }

        void FEvents_title(ChangedEvent e)
        {
            this.Invoke((MethodInvoker)(() => this.Text = (string)e.Message)); 
        }

        void FEvents_status(ChangedEvent e)
        {
            this.Invoke((MethodInvoker)(() => this.label5.Text = (string)e.Message));
        }

        void FEvents_progressbar(ChangedEvent e)
        {
            this.Invoke((MethodInvoker)(() => this.progressBar1.Value = (int)e.Message));
        }

        void FEvents_list(ChangedEvent e)
        {
            this.Invoke((MethodInvoker)(() => this.listBox1.Items.Remove(e.Message)));
        }

        void FEvents_ip(ChangedEvent e)
        {
            this.Invoke((MethodInvoker)(() => this.textBox1.ReadOnly = (bool)e.Message));
        }

        void FEvents_connectbutton(ChangedEvent e)
        {
            this.Invoke((MethodInvoker)(() => this.btnconnect.Text = (string)e.Message));
        }

        private void btnconnect_Click(object sender, EventArgs e)
        {
            if (btnconnect.Text == "Disconnect")
            {
                x.Disconnect();
                btnconnect.Text = "Connect";
                textBox1.ReadOnly = false;
            }
            else
            {
                Socket p = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    p.Connect(new IPEndPoint(IPAddress.Parse(textBox1.Text), int.Parse(textBox3.Text)));
                    x = new Client(p, false, this, false);
                    if (x.Connected)
                    {
                        textBox1.ReadOnly = true;
                        btnconnect.Text = "Disconnect";
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
        }
    }
}