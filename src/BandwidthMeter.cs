using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace BandwidthMeter
{
    public class BandwidthMeter : Form
    {
        //===================================================================== CONSTANTS
        private readonly static Font FONT_TITLE = new Font("Segoe UI", 12);

        private readonly static string LOG_NAME = string.Format(@"bmlog{0}", Environment.MachineName);
        private readonly static string LOG_PATH = string.Format(@"{0}\{1}.txt", Application.StartupPath, LOG_NAME);
        private readonly static string BACKUP_PATH = string.Format(@"{0}\{1} bak.txt", Application.StartupPath, LOG_NAME);

        private const int DOWNLOAD_LIMIT = 150; // download limit in GB
        private const int START_DAY = 6; // first day of billing cycle

        //===================================================================== CONTROLS
        private IContainer _components = new Container();
        private ContextMenu _contextMenu = new ContextMenu();
        private NotifyIcon _notifyIcon;

        //===================================================================== VARIABLES
        private BandwidthTracker _tracker;
        private bool _isClosing = false; // closing flag
        private int _saveTick = 0; // save tick (every 30 ticks -> 1 minute)

        //===================================================================== INITIALIZE
        public BandwidthMeter()
        {
            this.ClientSize = new Size(284, 292);

            _contextMenu.MenuItems.Add("About", menuAbout_Click);
            _contextMenu.MenuItems.Add(new MenuItem("Always on Top", menuAlwaysOnTop_Click) { Checked = true });
            _contextMenu.MenuItems.Add("Close", menuClose_Click);
            _notifyIcon = new NotifyIcon(_components);
            _notifyIcon.ContextMenu = _contextMenu;
            _notifyIcon.Icon = new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream("BandwidthMeter.icon.ico"));
            _notifyIcon.Visible = true;
            _notifyIcon.DoubleClick += notifyIcon_DoubleClick;

            this.BackgroundImage = new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream("BandwidthMeter.background.png"));
            this.DoubleBuffered = true;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.Icon = _notifyIcon.Icon;
            this.Location = new Point(SystemInformation.PrimaryMonitorSize.Width - this.Width, SystemInformation.WorkingArea.Bottom - this.Height);
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.Manual;
            this.Text = "Bandwidth Meter";
            this.TopMost = true;
            this.WindowState = FormWindowState.Minimized;

            InitializeTracker();
        }
        private void InitializeTracker()
        {
            // if log file exists, load it
            if (File.Exists(LOG_PATH))
                _tracker = new BandwidthTracker(DOWNLOAD_LIMIT, START_DAY, 2, 10, File.ReadAllText(LOG_PATH));
            else
                _tracker = new BandwidthTracker(DOWNLOAD_LIMIT, START_DAY, 2, 10);

            _tracker.Tick += tracker_Tick;

            SaveLog(LOG_PATH);
            SaveLog(BACKUP_PATH);
        }

        //===================================================================== TERMINATE
        protected override void Dispose(bool disposing)
        {
            FONT_TITLE.Dispose();

            _tracker.Dispose();
            _contextMenu.Dispose();

            if (disposing && _components != null) _components.Dispose();
            base.Dispose(disposing);
        }

        //===================================================================== FUNCTIONS
        private void SaveLog(string path)
        {
            File.WriteAllText(path, _tracker.GetSaveString());
        }

        private void UpdateNotifyText()
        {
            string notifyText = _tracker.NotifyString;
            _notifyIcon.Text = (notifyText.Length >= 64 ? "Unable to show" : notifyText);
        }

        //===================================================================== EVENTS
        protected override void OnPaint(PaintEventArgs e)
        {
            TextRenderer.DrawText(e.Graphics, _tracker.Name, FONT_TITLE, new Rectangle(0, 10, ClientSize.Width, 23), Color.Black, TextFormatFlags.HorizontalCenter | TextFormatFlags.Bottom);
            string[] labels = { "Today Downloaded", "Today Uploaded", "Month Downloaded", "Month Uploaded", "Monthly Limit", "Download Speed", "Upload Speed", "Interface Type" };
            string[] data = _tracker.DataString;

            // draw data
            for (int i = 0; i < labels.Length; i++)
            {
                TextRenderer.DrawText(e.Graphics, labels[i] + ":", this.Font, new Point(12, 50 + 30 * i), Color.Black);
                TextRenderer.DrawText(e.Graphics, data[i], this.Font, new Point(136, 50 + 30 * i), Color.Black);
            }

            // draw separators
            foreach (int i in new int[] { 0, 2, 5, 7, 8 })
            {
                e.Graphics.DrawLine(Pens.Black, 10, 41 + i * 30, this.Width - 16, 41 + i * 30);
                e.Graphics.DrawLine(Pens.Beige, 10, 41 + i * 30 + 1, this.Width - 16, 41 + i * 30 + 1);
            }

            base.OnPaint(e);
        }

        protected override void OnShown(EventArgs e)
        {
            this.Hide();
            base.OnShown(e);
        }
        protected override void OnResize(EventArgs e)
        {
            // hide form if minimized
            if (this.WindowState == FormWindowState.Minimized) this.Hide();
            base.OnResize(e);
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_isClosing)
                SaveLog(LOG_PATH); // save log on close
            else
            {
                // make closing form act as minimize
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
            }
            base.OnFormClosing(e);
        }

        private void notifyIcon_DoubleClick(object sender, EventArgs e)
        {
            // show form
            this.Show();
            this.WindowState = FormWindowState.Normal;

            // update number of days until bandwidth is reset
            this.Text = string.Format("Bandwidth Meter - {0} days", _tracker.DaysRemaining());
        }

        private void menuAbout_Click(object sender, EventArgs e)
        {
            string text = Assembly.GetExecutingAssembly().FullName + " " + Assembly.GetExecutingAssembly().GetName().Version.ToString();
            MessageBox.Show(text, "About");
        }
        private void menuAlwaysOnTop_Click(object sender, EventArgs e)
        {
            MenuItem menu = (MenuItem)sender;
            menu.Checked = (menu.Checked != true);
            this.TopMost = menu.Checked;
        }
        private void menuClose_Click(object sender, EventArgs e)
        {
            _isClosing = true;
            this.Close();
        }

        private void tracker_Tick(object sender, EventArgs e)
        {
            this.Invalidate();
            UpdateNotifyText();

            // every 30 ticks, save log
            _saveTick = ++_saveTick % 30;
            if (_saveTick == 0) SaveLog(LOG_PATH);
        }
    }
}
