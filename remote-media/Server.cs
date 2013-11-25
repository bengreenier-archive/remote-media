using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Diagnostics;

namespace remote_media
{

    public partial class Server : Form
    {
        public const int DEFAULT_PORT = 3000;

        private bool dirtyPort = false;
        private HttpListener httpd;

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        public const int KEYEVENTF_EXTENDEDKEY = 0x0001; //Key down flag
        public const int KEYEVENTF_KEYUP = 0x0002; //Key up flag
        public const int VK_MEDIA_PLAY_PAUSE = 0xB3;
        public const int VK_MEDIA_PREV_TRACK = 0xB1;
        public const int VK_MEDIA_NEXT_TRACK = 0xB0;
        public const int VK_VOLUME_UP = 0xAF;
        public const int VK_VOLUME_DOWN = 0xAE;


        public Server()
        {
            InitializeComponent();
            httpd = new HttpListener();
            backgroundWorker1.DoWork += backgroundWorker1_DoWork;
            backgroundWorker1.RunWorkerAsync();
            Restart();
        }

        void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!backgroundWorker1.CancellationPending)
            {
                if (httpd.IsListening)
                {
                    HttpListenerContext c = httpd.GetContext();
                    Stream output = c.Response.OutputStream;

                    if (c.Request.Url.Segments.Contains<string>("toggle"))
                    {
                        keybd_event(VK_MEDIA_PLAY_PAUSE, 0x45, KEYEVENTF_EXTENDEDKEY, 0);
                        keybd_event(VK_MEDIA_PLAY_PAUSE, 0x45, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
                    }
                    else if (c.Request.Url.Segments.Contains<string>("prev"))
                    {
                        keybd_event(VK_MEDIA_PREV_TRACK, 0x45, KEYEVENTF_EXTENDEDKEY, 0);
                        keybd_event(VK_MEDIA_PREV_TRACK, 0x45, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
                    }
                    else if (c.Request.Url.Segments.Contains<string>("next"))
                    {
                        keybd_event(VK_MEDIA_NEXT_TRACK, 0x45, KEYEVENTF_EXTENDEDKEY, 0);
                        keybd_event(VK_MEDIA_NEXT_TRACK, 0x45, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
                    }
                    else if (c.Request.Url.Segments.Contains<string>("up"))
                    {
                        keybd_event(VK_VOLUME_UP, 0x45, KEYEVENTF_EXTENDEDKEY, 0);
                        keybd_event(VK_VOLUME_UP, 0x45, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
                    }
                    else if (c.Request.Url.Segments.Contains<string>("down"))
                    {
                        keybd_event(VK_VOLUME_DOWN, 0x45, KEYEVENTF_EXTENDEDKEY, 0);
                        keybd_event(VK_VOLUME_DOWN, 0x45, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
                    }
                    Byte[] buffer = System.Text.Encoding.UTF8.GetBytes("<html><head><title>remote-media</title></head><body>"+
                    "<table><tr><td><a href='/prev'>Prev</a></td><td><a href='/toggle'>Play/Pause</a></td><td><a href='/next'>Next</a></td></tr>"+
                    "<tr><td><a href='/up'>up</a></td><td><a href='/down'>down</a></td></tr>"+
                    "</body></html>");
                    output.Write(buffer, 0, buffer.Length);
                    output.Close();
                }
            }
        }

        private void Server_FormClosing(object sender, FormClosingEventArgs e)
        {
            backgroundWorker1.CancelAsync();
            notifyIcon1.Visible = false;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            dirtyPort = true;
        }

        private void textBox1_Leave(object sender, EventArgs e)
        {
            if (dirtyPort)
            {
                int newPort = -1;
                if (int.TryParse(textBox1.Text, out newPort))
                {
                    if (newPort > 0 && newPort < 65535)
                    {
                        Restart(newPort);
                    }
                }
            }
        }

        private void Restart(int port = DEFAULT_PORT)
        {
            textBox1.Text = "" + port;
            if (httpd.IsListening)
            {
                httpd.Stop();
            }
            httpd.Prefixes.Clear();
            httpd.Prefixes.Add("http://localhost:" + port + "/");
            httpd.Prefixes.Add("http://localhost:" + port + "/toggle/");
            httpd.Prefixes.Add("http://localhost:" + port + "/next/");
            httpd.Prefixes.Add("http://localhost:" + port + "/prev/");
            httpd.Prefixes.Add("http://localhost:" + port + "/up/");
            httpd.Prefixes.Add("http://localhost:" + port + "/down/");

            
            httpd.Prefixes.Add("http://"+Dns.GetHostName()+":" + port + "/");
            httpd.Prefixes.Add("http://" + Dns.GetHostName() + ":" + port + "/toggle/");
            httpd.Prefixes.Add("http://" + Dns.GetHostName() + ":" + port + "/next/");
            httpd.Prefixes.Add("http://" + Dns.GetHostName() + ":" + port + "/prev/");
            httpd.Prefixes.Add("http://" + Dns.GetHostName() + ":" + port + "/up/");
            httpd.Prefixes.Add("http://" + Dns.GetHostName() + ":" + port + "/down/");

            httpd.Prefixes.Add("http://" + GetListenerIP() + ":" + port + "/");
            httpd.Prefixes.Add("http://" + GetListenerIP() + ":" + port + "/toggle/");
            httpd.Prefixes.Add("http://" + GetListenerIP() + ":" + port + "/next/");
            httpd.Prefixes.Add("http://" + GetListenerIP() + ":" + port + "/prev/");
            httpd.Prefixes.Add("http://" + GetListenerIP() + ":" + port + "/up/");
            httpd.Prefixes.Add("http://" + GetListenerIP() + ":" + port + "/down/");

            try
            {
                httpd.Start();
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show("You need to restart the app as admin");
                Environment.Exit(-1);
                //RestartAsAdmin(); //this is a terrible assumption to be made.. HA i find it funny
            }
        }

        static string GetListenerIP()
        {
            IPHostEntry host;
            string localIP = "?";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily.ToString() == "InterNetwork")
                {
                    localIP = ip.ToString();
                }
            }
            return localIP;
        }

        //quick fix for using Dns.GetHostname() to bind to. see also : using netsh (but tldr for now)
        static void RestartAsAdmin()
        {
            var startInfo = new ProcessStartInfo("remote-media.exe") { Verb = "runas" };
            startInfo.UseShellExecute = false;
            Process.Start(startInfo);
            Environment.Exit(0);
        }
    }
}
