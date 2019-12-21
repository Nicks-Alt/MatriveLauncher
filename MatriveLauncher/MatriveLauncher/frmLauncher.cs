using DiscordRPC;
using DiscordRPC.Message;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
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
        private DiscordRPC.DiscordRpcClient discord = new DiscordRPC.DiscordRpcClient("657810158273036311");
        #endregion

        #region Handlers
        public frmLauncher()
        {
            InitializeComponent();
            TmrPlayerCountRefresh_Tick(this, new EventArgs()); // Get player count
            byte[] fontData = Properties.Resources.Prototype;
            IntPtr fontPtr = System.Runtime.InteropServices.Marshal.AllocCoTaskMem(fontData.Length);
            System.Runtime.InteropServices.Marshal.Copy(fontData, 0, fontPtr, fontData.Length);
            uint dummy = 0;
            fonts.AddMemoryFont(fontPtr, Properties.Resources.Prototype.Length);
            AddFontMemResourceEx(fontPtr, (uint)Properties.Resources.Prototype.Length, IntPtr.Zero, ref dummy);
            System.Runtime.InteropServices.Marshal.FreeCoTaskMem(fontPtr);
            btnConnect.Font = new Font(fonts.Families[0], btnConnect.Font.Size);
            discord.Initialize();
            discord.RegisterUriScheme("4000", "steam://rungameid/4000");
            TmrCurrentServerQuery_Tick(this, new EventArgs());
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
            lblPlayerCount.Text = GetPlayerCount("74.91.115.113").ToString() + "/" + GetMaxPlayerCount("74.91.115.113").ToString();
            CenterControl(lblPlayerCount, this);
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

                return Convert.ToByte(ms.ReadByte());
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
            string steamID = new SteamBridge().GetSteamId().ToString();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12; // Secure security protocol for querying the steam API
            HttpWebRequest request = WebRequest.CreateHttp("http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key=51F8018514A1DFC912FBED2375F1E9E1&steamids=" + steamID);
            request.UserAgent = "Matrive";
            WebResponse response = null;
            response = request.GetResponse(); // Get Response from webrequest
            StreamReader sr = new StreamReader(response.GetResponseStream()); // Create stream to access web data
            var rawResults = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(sr.ReadToEnd());
            if (!rawResults.response.players.First.ToString().Contains("gameserverip"))
                return false;
            string ip = rawResults.response.players.First.gameserverip.ToString();
            //string playerName = rawResults.response.players.First.personaname.ToString();
            if (ip != "74.91.115.113:27015")
                return false;
            else
                return true;
        }
        #endregion


        #region Discord Shit

        private void TmrCurrentServerQuery_Tick(object sender, EventArgs e)
        {
            //MessageBox.Show(discord.SteamID);
            if (GetPlayerServerStatus())
            {
                discord.SetPresence(new DiscordRPC.RichPresence()
                {
                    Details = "100% Custom",
                    State = "In-Game",
                    Timestamps = DiscordRPC.Timestamps.Now,
                    Assets = new DiscordRPC.Assets()
                    {
                        LargeImageKey = "matrive",
                        LargeImageText = "Matrive.net"
                    }
                });
            }
            else
            {
                discord.SetPresence(new DiscordRPC.RichPresence()
                {
                    Details = "100% Custom",
                    State = "Idle",
                    Timestamps = DiscordRPC.Timestamps.Now,
                    Assets = new DiscordRPC.Assets()
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

#region Extensions
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
#endregion