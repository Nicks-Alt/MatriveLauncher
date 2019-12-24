using DiscordRPC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace MatriveLauncher
{
    public partial class frmLauncher : Form
    {
        #region Global Vars
        bool isTopPanelDragged = false;
        bool isWindowMaximized = false;
        Point offset;
        Size _normalWindowSize;
        Point _normalWindowLocation = Point.Empty;
        private PrivateFontCollection fonts = new PrivateFontCollection();
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern IntPtr AddFontMemResourceEx(IntPtr pbFont, uint cbFont, IntPtr pdv, [System.Runtime.InteropServices.In] ref uint pcFonts);
        private DiscordRpcClient discord = new DiscordRpcClient("657810158273036311");
        private SteamBridge steam = new SteamBridge();
        #endregion

        #region Handlers
        public frmLauncher()
        {
            InitializeComponent();
            //TmrPlayerCountRefresh_Tick(this, new EventArgs()); // Get player count
            byte[] fontData = Properties.Resources.Prototype;
            IntPtr fontPtr = System.Runtime.InteropServices.Marshal.AllocCoTaskMem(fontData.Length);
            System.Runtime.InteropServices.Marshal.Copy(fontData, 0, fontPtr, fontData.Length);
            uint dummy = 0;
            fonts.AddMemoryFont(fontPtr, Properties.Resources.Prototype.Length);
            AddFontMemResourceEx(fontPtr, (uint)Properties.Resources.Prototype.Length, IntPtr.Zero, ref dummy);
            System.Runtime.InteropServices.Marshal.FreeCoTaskMem(fontPtr);
            btnConnect.Font = new Font(fonts.Families[0], btnConnect.Font.Size);
            discord.Initialize();
            //TmrCurrentServerQuery_Tick(this, new EventArgs());
            new Thread(() =>
            {
                while (true)
                {
                    TmrCurrentServerQuery_Tick(this, new EventArgs());
                    Thread.Sleep(60000);
                }
            }).Start();
            new Thread(() =>
            {
                while (true)
                {
                    TmrPlayerCountRefresh_Tick(this, new EventArgs());
                    Thread.Sleep(5000);
                }
            }).Start();

        }
        public static bool IsNumeric(string str)
        {
            double dummy = 0.0;
            return double.TryParse(str, out dummy);
        }
        public static bool isStringOnlyAlphabet(String str)
        {
            return ((str != null)
                    && (str.Trim() != "")
                    && (str.IsNormalized()));
        }

        List<string> GetPlayersOnServer(string ip)
        {
            //  SERVERS:
            // Rockford: 74.91.115.113
            // More to come...hopefully.

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            byte[] challengeResponse = new byte[9];
            socket.Connect(ip, 27015);
            byte[] challengeRequest = { 0xFF, 0xFF, 0xFF, 0xFF, 0x55, 0xFF, 0xFF, 0xFF, 0xFF };
            socket.Send(challengeRequest);
            socket.Receive(challengeResponse);

            byte[] A2S_PLAYER_REQUEST = new byte[9];
            for (int i = 0; i < 4; i++)
            {
                A2S_PLAYER_REQUEST[i] = 0xFF;
            }
            A2S_PLAYER_REQUEST[4] = 0x55;
            for (int i = 5; i <= 8; i++)
            {
                A2S_PLAYER_REQUEST[i] = challengeResponse[i];
            }
            byte[] A2S_PLAYER_RESPONSE = new byte[512];
            socket.Send(A2S_PLAYER_REQUEST);

            socket.Receive(A2S_PLAYER_RESPONSE);

            using (var ms = new MemoryStream(A2S_PLAYER_RESPONSE))
            {
                ms.ReadByte(); // Read the null bytes
                ms.ReadByte();
                ms.ReadByte();
                ms.ReadByte();
                ms.ReadByte(); // Read Header & first zero
                ms.ReadByte();
                List<string> players = new List<string>();
                while (ms.Position != 512)
                {
                    string name = ms.ReadTerminatedString();
                    if (isStringOnlyAlphabet(name))
                    {
                        players.Add(name);
                        ms.Position += 9;
                    }
                    else
                        continue;
                }
                return players;
            }
        }
        private void TopBar_MouseUp(object sender, MouseEventArgs e)
        {
            isTopPanelDragged = false;
            if (this.Location.Y <= 5)
            {
                if (!isWindowMaximized)
                {
                    _normalWindowSize = this.Size;
                    _normalWindowLocation = this.Location;

                    Rectangle rect = Screen.PrimaryScreen.WorkingArea;
                    this.Location = new Point(0, 0);
                    this.Size = new System.Drawing.Size(rect.Width, rect.Height);

                    isWindowMaximized = true;
                }
            }
        }

        private void TopBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (isTopPanelDragged)
            {
                Point newPoint = topBar.PointToScreen(new Point(e.X, e.Y));
                newPoint.Offset(offset);
                this.Location = newPoint;

                if (this.Location.X > 2 || this.Location.Y > 2)
                {
                    if (this.WindowState == FormWindowState.Maximized)
                    {
                        this.Location = _normalWindowLocation;
                        this.Size = _normalWindowSize;

                        isWindowMaximized = false;
                    }
                }
            }
        }

        private void TopBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isTopPanelDragged = true;
                Point pointStartPosition = this.PointToScreen(new Point(e.X, e.Y));
                offset = new Point
                {
                    X = this.Location.X - pointStartPosition.X,
                    Y = this.Location.Y - pointStartPosition.Y
                };
            }
            else
            {
                isTopPanelDragged = false;
            }
            if (e.Clicks == 2)
            {
                isTopPanelDragged = false;

            }
        }
        private void TmrPlayerCountRefresh_Tick(object sender, EventArgs e)
        {
            ThreadHelperClass.SetText(this, lblPlayerCount, GetPlayerCount("74.91.115.113").ToString() + "/" + GetMaxPlayerCount("74.91.115.113").ToString());
            ThreadHelperClass.CenterControlToParent(this, lblPlayerCount, this);
        }
        private void PictureBox2_Click(object sender, EventArgs e)
        {
            Process.Start("https://discord.gg/n7gNUWg");
        }

        private void PictureBox3_Click(object sender, EventArgs e)
        {
            Process.Start("https://matrive.net/forums/");
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            Process.GetCurrentProcess().Kill();
        }

        private void BtnConnect_Click(object sender, EventArgs e)
        {
            Process.Start("steam://connect/74.91.115.113:27015");
        }
        #endregion

        #region Cool Methods and shit
        /// <summary>
        /// Aligns a specified control to the center of the parent control.
        /// </summary>
        /// <param name="ctrl">The control to center.</param>
        /// <param name="parent">The control containing the control.</param>
        private void CenterControl(Control ctrl, Control parent)
        {
            ctrl.Left = (parent.Size.Width / 2) - (ctrl.Width / 2); // Center Horizontally
            //ctrl.Top = (parent.Size.Height / 2) - (ctrl.Height / 2); // Center vertically
        }
        private byte GetPlayerCount(string ip)
        {
            //  SERVERS:
            // Rockford: 74.91.115.113
            // More to come...hopefully.

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            byte[] rawData = new byte[512];
            socket.Connect(ip, 27015);
            byte[] sendBytes = { 0xFF, 0xFF, 0xFF, 0xFF, 0x54, 0x53, 0x6F, 0x75, 0x72, 0x63, 0x65, 0x20, 0x45, 0x6E, 0x67, 0x69, 0x6E, 0x65, 0x20, 0x51, 0x75, 0x65, 0x72, 0x79, 0x00 };
            socket.Send(sendBytes);

            socket.Receive(rawData);
            using (var ms = new MemoryStream(rawData))
            {
                ms.ReadByte(); // Read the null bytes
                ms.ReadByte();
                ms.ReadByte();
                ms.ReadByte();

                ms.ReadByte(); // Read Header
                ms.ReadByte(); // Read Protocol

                ms.ReadTerminatedString(); // Read Name
                ms.ReadTerminatedString(); // Read Map
                ms.ReadTerminatedString(); // Read Folder
                ms.ReadTerminatedString(); // Read Game

                ms.ReadByte(); // Read ID
                ms.ReadByte(); // Read Players

                return Convert.ToByte(ms.ReadByte()); // Read Max Players & Return it
            }
        }
        private byte GetMaxPlayerCount(string ip)
        {
            //  SERVERS:
            // Rockford: 74.91.115.113
            // More to come...hopefully.

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            byte[] rawData = new byte[512];
            socket.Connect(ip, 27015);
            byte[] sendBytes = { 0xFF, 0xFF, 0xFF, 0xFF, 0x54, 0x53, 0x6F, 0x75, 0x72, 0x63, 0x65, 0x20, 0x45, 0x6E, 0x67, 0x69, 0x6E, 0x65, 0x20, 0x51, 0x75, 0x65, 0x72, 0x79, 0x00 };
            socket.Send(sendBytes);

            socket.Receive(rawData);
            using (var ms = new MemoryStream(rawData))
            {
                ms.ReadByte();
                ms.ReadByte();
                ms.ReadByte();
                ms.ReadByte();

                ms.ReadByte();
                ms.ReadByte();

                ms.ReadTerminatedString();
                ms.ReadTerminatedString();
                ms.ReadTerminatedString();
                ms.ReadTerminatedString();

                ms.ReadByte();
                ms.ReadByte();
                ms.ReadByte();

                return Convert.ToByte(ms.ReadByte());
            }
        }
        private bool GetPlayerServerStatus()
        {
            string steamID = steam.GetSteamId().ToString();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12; // Secure security protocol for querying the steam API
            HttpWebRequest request = WebRequest.CreateHttp("http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key=51F8018514A1DFC912FBED2375F1E9E1&steamids=" + steamID);
            request.UserAgent = "Matrive";
            WebResponse response = null;
            response = request.GetResponse(); // Get Response from webrequest
            StreamReader sr = new StreamReader(response.GetResponseStream()); // Create stream to access web data
            var rawResults = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(sr.ReadToEnd());
            //string ip = rawResults.response.players.First.gameserverip.ToString();
            string playerName = rawResults.response.players.First.personaname.ToString();
            List<string> ServerPlayers = GetPlayersOnServer("74.91.115.113");
            foreach (var player in ServerPlayers)
            {
                if (player.Contains(playerName))
                    return true;
                else
                    continue;
            }
            return false;
        }
        #endregion


        #region Discord Shit

        private void TmrCurrentServerQuery_Tick(object sender, EventArgs e)
        {
            //MessageBox.Show(discord.SteamID);
            if (GetPlayerServerStatus())
            {
                if (discord.CurrentPresence != null)
                    if (discord.CurrentPresence.State.ToString() == "In-Game")
                        return;
                discord.SetPresence(new RichPresence()
                {
                    Details = "100% Custom",
                    State = "In-Game",
                    Timestamps = Timestamps.Now,
                    Assets = new Assets()
                    {
                        LargeImageKey = "matrive",
                        LargeImageText = "Matrive.net"
                    }
                });
            }
            else
            {
                //if (discord.CurrentPresence)
                if (discord.CurrentPresence != null)
                    if (discord.CurrentPresence.State.ToString() == "Idle")
                        return;
                discord.SetPresence(new RichPresence()
                {
                    Details = "100% Custom",
                    State = "Idle",
                    Timestamps = Timestamps.Now,
                    Assets = new Assets()
                    {
                        LargeImageKey = "matrive",
                        LargeImageText = "Matrive.net"
                    }
                });
            }
        }
        #endregion
    }
}

#region Extensions & Other Classes
public static class MemoryStreamExtensions
{
    public static string ReadTerminatedString(this MemoryStream ms)
    {
        List<byte> res = new List<byte>();

        byte last;
        while ((last = (byte)ms.ReadByte()) != 0x00)
        {
            res.Add(last);
        }

        return System.Text.Encoding.ASCII.GetString(res.ToArray());
    }
}
public static class ThreadHelperClass // Because fuck threads and me not allowing to just access properties on the main form like a normal person
{
    delegate void SetTextCallback(Form f, Control ctrl, string text);
    delegate void CenterControlToParentCallback(Form f, Control ctrl, Control parent);
    /// <summary>
    /// Set text property of various controls
    /// </summary>
    /// <param name="form">The calling form</param>
    /// <param name="ctrl">The control being modified</param>
    /// <param name="text">The text to set</param>
    public static void SetText(Form form, Control ctrl, string text)
    {
        // InvokeRequired required compares the thread ID of the 
        // calling thread to the thread ID of the creating thread. 
        // If these threads are different, it returns true. 
        if (ctrl.InvokeRequired)
        {
            SetTextCallback d = new SetTextCallback(SetText);
            form.Invoke(d, new object[] { form, ctrl, text });
        }
        else
        {
            ctrl.Text = text;
        }
    }
    /// <summary>
    /// Set left property of various controls
    /// </summary>
    /// <param name="form">The calling form</param>
    /// <param name="ctrl">The control being centered</param>
    /// <param name="parent">The parent control to center the object to</param>
    public static void CenterControlToParent(Form form, Control ctrl, Control parent)
    {
        // InvokeRequired required compares the thread ID of the 
        // calling thread to the thread ID of the creating thread. 
        // If these threads are different, it returns true. 
        if (ctrl.InvokeRequired)
        {
            CenterControlToParentCallback d = new CenterControlToParentCallback(CenterControlToParent);
            form.Invoke(d, new object[] { form, ctrl, parent });
        }
        else
        {
            ctrl.Left = (parent.Size.Width / 2) - (ctrl.Width / 2);
        }
    }
}
#endregion