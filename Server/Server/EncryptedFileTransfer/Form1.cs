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
            notifyicon.Text = "EncryptedFileTransfer";
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
        }

        void FEvents_connectbutton(ChangedEvent e)
        {
        }

        private void btnstop_Click(object sender, EventArgs e)
        {
            f.Close();

            btnstart.Enabled = true;
            btnstop.Enabled = false;
            textBox2.ReadOnly = false;
            label1.Font = new Font("Work Sans", 18);
            label1.Text = "Encrypted File Transfer ";
            th.Abort();
        }

        private void btnstart_Click(object sender, EventArgs e)
        {
            f = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            f.Bind(new IPEndPoint(IPAddress.Any, int.Parse(textBox2.Text)));
            f.Listen(50000);
            label1.Font = new Font("Work Sans", 10);
            label1.Text = string.Format("Server started IP : {0}...", LocalIPAddress());
            btnstart.Enabled = false;
            btnstop.Enabled = true;
            textBox2.ReadOnly = true;

            th = new Thread(new ThreadStart(delegate ()
            {
                while (true)
                    try
                    {
                        new Thread(new ParameterizedThreadStart(delegate (object _socket)
                        {
                            Client cli = new Client((Socket)_socket, true, this, true);
                            if (MessageBox.Show("Client is connecting from " + cli.RemoteIp + ". Allow this connection?", "Incoming Connection", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == System.Windows.Forms.DialogResult.Yes)
                            {
                                cli.Accepted = true;

                                Console.WriteLine("Client connected from " + cli.RemoteIp);

                                Utilities.clientx.Add(cli);
                                if (listBox1.InvokeRequired)
                                {
                                    listBox1.Invoke((MethodInvoker)(() => { listBox1.Items.Add(cli.RemoteIp); }));
                                }
                            }
                            else
                            {
                                cli.Disconnect();
                            }
                        })).Start(f.Accept());
                    }
                    catch { }
            }));
            th.Start();
        }

        private void metroButton1_Click(object sender, EventArgs e)
        {
            if (label5.Visible == true)
            {
                label2.Visible = false;
                label5.Visible = false;
                progressBar1.Visible = false;
            }
            else
            {
                label2.Visible = true;
                label5.Visible = true;
                progressBar1.Visible = true;
            }

        }
    }
}