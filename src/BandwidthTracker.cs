using System;
using System.Net.NetworkInformation;
using System.Text;
using System.Timers;

namespace BandwidthMeter
{
    public class BandwidthTracker : IDisposable
    {
        //===================================================================== EVENTS
        public event EventHandler Tick;

        //===================================================================== VARIABLES
        private Timer _timer = new Timer(2000);

        private DateTime _currentDay;
        private DateTime _currentMonthStart;
        private long _monthLimit;
        private int _monthStart;
        private int _offPeakStart;
        private int _offPeakEnd;

        private NetworkInterface _nic;

        private int _downloadSpeed;
        private int _uploadSpeed;

        private BandwidthUsage _bwignored = new BandwidthUsage();
        private BandwidthUsage _bwCounted = new BandwidthUsage();
        private BandwidthUsage _bwTodayOnPeak = new BandwidthUsage();
        private BandwidthUsage _bwTodayOffPeak = new BandwidthUsage();
        private BandwidthUsage _bwMonthOnPeak = new BandwidthUsage();
        private BandwidthUsage _bwMonthOffPeak = new BandwidthUsage();

        //===================================================================== INITIALIZE
        public BandwidthTracker(int monthLimit, int monthStart, int offPeakStart, int offPeakEnd)
        {
            _monthLimit = monthLimit * (long)Math.Pow(1024, 3);
            _monthStart = monthStart;
            _offPeakStart = offPeakStart;
            _offPeakEnd = offPeakEnd;
            _currentDay = DateTime.Now.Date;
            _currentMonthStart = GetLastMonthStart();

            // initialize nic
            _nic = GetNIC();
            _bwignored = TotalBytes;
            _bwCounted = new BandwidthUsage();

            // start timer
            _timer.Elapsed += timer_Elapsed;
            _timer.Start();
        }
        public BandwidthTracker(int monthLimit, int monthStart, int offPeakStart, int offPeakEnd, string save) : this(monthLimit, monthStart, offPeakStart, offPeakEnd)
        {
            string[] split = save.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            _currentDay = new DateTime(int.Parse(split[2]), int.Parse(split[1]), int.Parse(split[0]));
            _currentMonthStart = new DateTime(int.Parse(split[5]), int.Parse(split[4]), int.Parse(split[3]));

            // load daily usage only if it is not a new day
            if (!IsNewDay(_currentDay))
            {
                _bwTodayOnPeak = new BandwidthUsage(long.Parse(split[6]), long.Parse(split[7]));
                _bwTodayOffPeak = new BandwidthUsage(long.Parse(split[8]), long.Parse(split[9]));
            }
            else _currentDay = DateTime.Now.Date;

            // load monthly usage log only if it is not a new month
            if (!IsNewMonth(_currentMonthStart))
            {
                _bwMonthOnPeak = new BandwidthUsage(long.Parse(split[10]), long.Parse(split[11]));
                _bwMonthOffPeak = new BandwidthUsage(long.Parse(split[12]), long.Parse(split[13]));
            }
            else _currentMonthStart = GetLastMonthStart();
        }
        public DateTime GetLastMonthStart()
        {
            DateTime monthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, _monthStart);
            if (monthStart > DateTime.Now.Date) monthStart = monthStart.AddMonths(-1);
            return monthStart;
        }
        public bool IsNewDay(DateTime lastDate)
        {
            return (DateTime.Today.Date > lastDate);
        }
        public bool IsNewMonth(DateTime lastMonthStart)
        {
            return (DateTime.Today.Date >= lastMonthStart.AddMonths(1));
        }

        //===================================================================== TERMINATE
        public void Dispose()
        {
            _timer.Dispose();
        }

        //===================================================================== FUNCTIONS
        public string GetSaveString()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(_currentDay.Day.ToString());
            builder.AppendLine(_currentDay.Month.ToString());
            builder.AppendLine(_currentDay.Year.ToString());
            builder.AppendLine(_currentMonthStart.Day.ToString());
            builder.AppendLine(_currentMonthStart.Month.ToString());
            builder.AppendLine(_currentMonthStart.Year.ToString());
            builder.AppendLine(_bwTodayOnPeak.Download.ToString());
            builder.AppendLine(_bwTodayOnPeak.Upload.ToString());
            builder.AppendLine(_bwTodayOffPeak.Download.ToString());
            builder.AppendLine(_bwTodayOffPeak.Upload.ToString());
            builder.AppendLine(_bwMonthOnPeak.Download.ToString());
            builder.AppendLine(_bwMonthOnPeak.Upload.ToString());
            builder.AppendLine(_bwMonthOffPeak.Download.ToString());
            builder.AppendLine(_bwMonthOffPeak.Upload.ToString());
            return builder.ToString();
        }
        private NetworkInterface GetNIC()
        {
            // get the active network interface
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up) continue;
                if (nic.Name == "Ethernet" || nic.Name == "Local Area Connection" || nic.Name == "Wireless Network Connection") return nic; // windows 7 & 8 (ethernet)
            }
            return null;
        }

        public int DaysRemaining()
        {
            int day = DateTime.Today.Day; // current day
            if (day < _monthStart) return (_monthStart - day); // if between 1 and 3, take difference
            else return (DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month) + _monthStart - day);
        }
        private bool IsOffPeakTime()
        {
            return false;

            // no longer required, always off-peak
            //// convert timeframe to always starting at 0 hour
            //int start = 0;
            //int end = _offPeakEnd - _offPeakStart;
            //int now = DateTime.Now.Hour - _offPeakStart;
            //if (now < 0) now += 24;
            //if (end < 0) end += 24;
            //return (now >= start && now < end);
        }

        //===================================================================== PROPERTIES
        public string Name
        {
            get
            {
                if (_nic == null) return "No Connection";
                else return _nic.Name;
            }
        }
        public string DownloadedTodayString
        {
            get { return string.Format("{0} MB ({1})", _bwTodayOnPeak.DownloadMB, TotalBytes.DownloadMB); }
            // on-peak text
            //get { return string.Format("{0} MB / {1} MB ({2})", _bwTodayOnPeak.DownloadMB, _bwTodayOffPeak.DownloadMB, TotalBytes.DownloadMB); }
        }
        public string UploadedTodayString
        {
            get { return string.Format("{0} MB ({1})", _bwTodayOnPeak.UploadMB, TotalBytes.UploadMB); }
            // on-peak text
            //get { return string.Format("{0} MB / {1} MB ({2})", _bwTodayOnPeak.UploadMB, _bwTodayOffPeak.UploadMB, TotalBytes.UploadMB); }
        }
        public string DownloadedMonthString
        {
            get { return string.Format("{0} GB", _bwMonthOnPeak.DownloadGB); }
            // on-peak text
            //get { return string.Format("{0} GB / {1} GB", _bwMonthOnPeak.DownloadGB, _bwMonthOffPeak.DownloadGB); }
        }
        public string UploadedMonthString
        {
            get { return string.Format("{0} GB", _bwMonthOnPeak.UploadGB); }
            // on-peak text
            //get { return string.Format("{0} GB / {1} GB", _bwMonthOnPeak.UploadGB, _bwMonthOffPeak.UploadGB); }
        }
        public string RemainingTodayString
        {
            get
            {
                long dailyAllowance = _monthLimit / 31;
                int daysPassed = 31 - DaysRemaining() + 1;
                float currentLimit = daysPassed * dailyAllowance / 1024f / 1024 / 1024;
                return string.Format("{0} GB", (float)Math.Round(currentLimit - _bwMonthOnPeak.TotalGB, 2));
                // on-peak text
                //return string.Format("{0} GB / {1} GB", (float)Math.Round(currentLimit - _bwMonthOnPeak.TotalGB, 2), (float)Math.Round(currentLimit - _bwMonthOffPeak.TotalGB, 2));
            }
        }

        public string DownloadSpeedString
        {
            get { return string.Format("{0} kB/s", _downloadSpeed); }
        }
        public string UploadSpeedString
        {
            get { return string.Format("{0} kB/s", _uploadSpeed); }
        }

        public string InterfaceTypeString
        {
            get
            {
                if (_nic == null) return "NA";
                else return string.Format("{0}, {1} Mbps", _nic.NetworkInterfaceType, _nic.Speed / 1000000);
            }
        }
        public string[] DataString
        {
            get
            {
                return new string[] { DownloadedTodayString, UploadedTodayString, DownloadedMonthString, UploadedMonthString,
                                      RemainingTodayString, DownloadSpeedString, UploadSpeedString, InterfaceTypeString };
            }
        }
        public string NotifyString
        {
            get
            {
                string format = "Today: {0} MB / {1} MB\r\nMonth: {2} GB / {3} GB";
                return string.Format(format, _bwTodayOnPeak.TotalMB, _bwTodayOffPeak.TotalMB, Math.Round(_bwMonthOnPeak.TotalGB, 2), Math.Round(_bwMonthOffPeak.TotalGB, 2));
            }
        }

        private BandwidthUsage Bytes
        {
            get { return TotalBytes - _bwignored; }
        }
        private BandwidthUsage TotalBytes
        {
            get
            {
                if (_nic == null) return new BandwidthUsage();
                else return new BandwidthUsage(_nic.GetIPv4Statistics().BytesReceived, _nic.GetIPv4Statistics().BytesSent);
            }
        }
        private BandwidthUsage RemainingOnPeak
        {
            get
            {
                return _monthLimit - _bwMonthOnPeak;
            }
        }
        private BandwidthUsage RemainingOffPeak
        {
            get
            {
                return _monthLimit - _bwMonthOffPeak;
            }
        }

        //===================================================================== EVENTS
        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_nic == null)
            {
                _nic = GetNIC();
                if (_nic == null) return;
                _bwignored = TotalBytes;
                _bwCounted = new BandwidthUsage();
            }
            else CheckInterfaceReset();

            // calculate downloaded data in last tick and add to counted
            BandwidthUsage added = Bytes - _bwCounted;
            _bwCounted = Bytes;

            /* hack fix: when waking up from sleep mode, TotalBytes momentarily returns 0 which results in CheckInterfaceReset setting ignored to 0
               and therefore Bytes becomes TotalBytes when TotalBytes loads. To prevent this, ignore any download over 10 MB. Find a better fix later */
            if (added.Download < 1024 * 1024 * 10)
            {
                UpdateUsage(added);
                UpdateDownloadSpeed(added);
            }
            else Console.WriteLine("DON'T ADD");

            UpdateNewDay();
            if (Tick != null) Tick(this, new EventArgs());
        }

        private void CheckInterfaceReset()
        {
            if (TotalBytes < _bwignored)
            {
                _bwignored = TotalBytes;
                _bwCounted = new BandwidthUsage();
            }
        }

        private void UpdateUsage(BandwidthUsage bytes)
        {
            if (IsOffPeakTime())
            {
                _bwTodayOffPeak += bytes;
                _bwMonthOffPeak += bytes;
            }
            else
            {
                _bwTodayOnPeak += bytes;
                _bwMonthOnPeak += bytes;
            }
        }
        private void UpdateDownloadSpeed(BandwidthUsage bytes)
        {
            _downloadSpeed = (int)(bytes.Download / (_timer.Interval / 1000) / 1024);
            _uploadSpeed = (int)(bytes.Upload / (_timer.Interval / 1000) / 1024);
        }

        private void UpdateNewDay()
        {
            if (IsNewDay(_currentDay))
            {
                _currentDay = DateTime.Today.Date;
                _bwTodayOnPeak = new BandwidthUsage();
                _bwTodayOffPeak = new BandwidthUsage();

                if (IsNewMonth(_currentMonthStart))
                {
                    _currentMonthStart = DateTime.Today.Date;
                    _bwMonthOnPeak = new BandwidthUsage();
                    _bwMonthOffPeak = new BandwidthUsage();
                }
            }
        }
    }
}
