using System;

namespace BandwidthMeter
{
    public struct BandwidthUsage
    {
        //===================================================================== VARIABLES
        public readonly long Download;
        public readonly long Upload;

        //===================================================================== INITIALIZE
        public BandwidthUsage(long download, long upload)
        {
            Download = Math.Max(download, 0);
            Upload = Math.Max(upload, 0);
        }

        //===================================================================== FUNCTIONS
        public static BandwidthUsage operator +(BandwidthUsage bw1, BandwidthUsage bw2)
        {
            return new BandwidthUsage(bw1.Download + bw2.Download, bw1.Upload + bw2.Upload);
        }
        public static BandwidthUsage operator +(BandwidthUsage bw, long value)
        {
            return new BandwidthUsage(bw.Download + value, bw.Upload + value);
        }
        public static BandwidthUsage operator -(BandwidthUsage bw1, BandwidthUsage bw2)
        {
            return new BandwidthUsage(bw1.Download - bw2.Download, bw1.Upload - bw2.Upload);
        }
        public static BandwidthUsage operator -(BandwidthUsage bw, long value)
        {
            return new BandwidthUsage(bw.Download - value, bw.Upload - value);
        }
        public static BandwidthUsage operator -(long value, BandwidthUsage bw)
        {
            return new BandwidthUsage(value - bw.Download, value - bw.Upload);
        }
        public static bool operator <(BandwidthUsage bw1, BandwidthUsage bw2)
        {
            return (bw1.Download < bw2.Download || bw1.Upload < bw2.Upload);
        }
        public static bool operator >(BandwidthUsage bw1, BandwidthUsage bw2)
        {
            return (bw1.Download > bw2.Download || bw1.Upload > bw2.Upload);
        }

        //===================================================================== PROPERTIES
        public long TotalBytes
        {
            get { return Download + Upload; }
        }
        public int DownloadMB
        {
            get { return (int)(Download / 1024 / 1024); }
        }
        public int UploadMB
        {
            get { return (int)(Upload / 1024 / 1024); }
        }
        public float TotalMB
        {
            get { return DownloadMB + UploadMB; }
        }
        public float DownloadGB
        {
            get { return (float)Math.Round(Download / 1024f / 1024 / 1024, 2); }
        }
        public float UploadGB
        {
            get { return (float)Math.Round(Upload / 1024f / 1024 / 1024, 2); }
        }
        public float TotalGB
        {
            get { return DownloadGB + UploadGB; }
        }
    }
}
