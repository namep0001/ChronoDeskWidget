using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.NetworkInformation;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Linq;

namespace ChronoDeskWidget
{
    internal sealed class NetworkUsageDay
    {
        public DateTime Date;
        public long DownloadBytes;
        public long UploadBytes;
    }

    internal sealed class NetworkUsageMonitor : IDisposable
    {
        private readonly string _path = Path.Combine(LocalStore.Folder, "network-month.csv");
        private readonly Dictionary<DateTime, NetworkUsageDay> _days = new Dictionary<DateTime, NetworkUsageDay>();
        private readonly object _sync = new object();
        private Dictionary<string, long[]> _lastCounters = new Dictionary<string, long[]>();
        private long _previousReceived;
        private long _previousSent;
        public double DownloadBytesPerSecond { get; private set; }
        public double UploadBytesPerSecond { get; private set; }
        public long TotalReceived { get; private set; }
        public long TotalSent { get; private set; }
        public bool Connected { get; private set; }
        public bool InternetConnected { get; private set; }
        private DateTime _nextInternetCheckUtc = DateTime.MinValue;
        private DateTime _nextUsageSampleUtc = DateTime.MinValue;
        private bool _internetCheckInProgress;

        public NetworkUsageMonitor()
        {
            Load();
            NetworkChange.NetworkAddressChanged += OnNetworkChanged;
            NetworkChange.NetworkAvailabilityChanged += OnAvailabilityChanged;
            SampleMonthlyUsage();
            UpdateSpeedBaseline();
        }

        private void OnNetworkChanged(object sender, EventArgs e) { SampleMonthlyUsage(); UpdateSpeedBaseline(); }
        private void OnAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e) { SampleMonthlyUsage(); UpdateSpeedBaseline(); }

        private Dictionary<string, long[]> ReadCounters()
        {
            Dictionary<string, long[]> values = new Dictionary<string, long[]>();
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback || nic.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;
                try
                {
                    IPv4InterfaceStatistics stats = nic.GetIPv4Statistics();
                    values[nic.Id] = new long[] { Math.Max(0, stats.BytesReceived), Math.Max(0, stats.BytesSent) };
                }
                catch { }
            }
            return values;
        }

        private void SampleMonthlyUsage()
        {
            lock (_sync)
            {
            Dictionary<string, long[]> current = ReadCounters();
            DateTime today = DateTime.Today;
            NetworkUsageDay day;
            if (!_days.TryGetValue(today, out day))
                _days[today] = day = new NetworkUsageDay { Date = today };

            if (_lastCounters.Count > 0)
            {
                foreach (KeyValuePair<string, long[]> item in current)
                {
                    long[] previous;
                    if (!_lastCounters.TryGetValue(item.Key, out previous)) continue;
                    long down = item.Value[0] >= previous[0] ? item.Value[0] - previous[0] : 0;
                    long up = item.Value[1] >= previous[1] ? item.Value[1] - previous[1] : 0;
                    day.DownloadBytes += down;
                    day.UploadBytes += up;
                }
            }
            _lastCounters = current;
            Connected = NetworkInterface.GetIsNetworkAvailable();
            TrimMonth();
            Save();
            UpdateTotals();
            }
        }

        public void Tick()
        {
            lock (_sync)
            {
            long received, sent;
            ReadTotals(out received, out sent);
            if (_previousReceived > 0 && received >= _previousReceived)
                DownloadBytesPerSecond = received - _previousReceived;
            else DownloadBytesPerSecond = 0;
            if (_previousSent > 0 && sent >= _previousSent)
                UploadBytesPerSecond = sent - _previousSent;
            else UploadBytesPerSecond = 0;
            _previousReceived = received;
            _previousSent = sent;
            Connected = NetworkInterface.GetIsNetworkAvailable();
            if (!_internetCheckInProgress && DateTime.UtcNow >= _nextInternetCheckUtc)
            {
                _internetCheckInProgress = true;
                _nextInternetCheckUtc = DateTime.UtcNow.AddSeconds(5);
                ThreadPool.QueueUserWorkItem(delegate
                {
                    bool internetAvailable = false;
                    string[] probes = { "1.1.1.1", "8.8.8.8" };
                    foreach (string address in probes)
                    {
                        try
                        {
                            using (Ping ping = new Ping())
                            {
                                PingReply reply = ping.Send(address, 1000);
                                if (reply.Status == IPStatus.Success) { internetAvailable = true; break; }
                            }
                        }
                        catch { }
                    }
                    lock (_sync)
                    {
                        InternetConnected = internetAvailable;
                        _internetCheckInProgress = false;
                    }
                });
            }

            if (DateTime.UtcNow >= _nextUsageSampleUtc)
            {
                _nextUsageSampleUtc = DateTime.UtcNow.AddSeconds(5);
                SampleMonthlyUsage();
            }
            }
        }

        private void UpdateSpeedBaseline() { lock (_sync) { ReadTotals(out _previousReceived, out _previousSent); DownloadBytesPerSecond = 0; UploadBytesPerSecond = 0; } }
        private void ReadTotals(out long received, out long sent)
        {
            received = 0; sent = 0;
            foreach (long[] pair in ReadCounters().Values) { received += pair[0]; sent += pair[1]; }
            TotalReceived = received; TotalSent = sent;
        }

        private void UpdateTotals()
        {
            TotalReceived = 0; TotalSent = 0;
            foreach (NetworkUsageDay day in _days.Values) { TotalReceived += day.DownloadBytes; TotalSent += day.UploadBytes; }
        }

        private void TrimMonth()
        {
            DateTime first = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            List<DateTime> remove = new List<DateTime>();
            foreach (DateTime date in _days.Keys) if (date < first || date > DateTime.Today) remove.Add(date);
            foreach (DateTime date in remove) _days.Remove(date);
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(_path)) return;
                foreach (string line in File.ReadAllLines(_path))
                {
                    string[] p = line.Split(',');
                    DateTime date; long down, up;
                    if (p.Length == 3 && DateTime.TryParseExact(p[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date) &&
                        long.TryParse(p[1], out down) && long.TryParse(p[2], out up))
                        _days[date.Date] = new NetworkUsageDay { Date = date.Date, DownloadBytes = Math.Max(0, down), UploadBytes = Math.Max(0, up) };
                }
            }
            catch { }
            TrimMonth(); UpdateTotals();
        }

        private void Save()
        {
            try
            {
                LocalStore.EnsureFolder();
                using (StreamWriter writer = new StreamWriter(_path, false))
                    foreach (NetworkUsageDay day in _days.Values)
                        writer.WriteLine(day.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "," +
                            day.DownloadBytes.ToString(CultureInfo.InvariantCulture) + "," + day.UploadBytes.ToString(CultureInfo.InvariantCulture));
            }
            catch { }
        }

        public IList<NetworkUsageDay> GetDays()
        {
            lock (_sync)
            {
            List<NetworkUsageDay> result = new List<NetworkUsageDay>(_days.Values);
            result.Sort(delegate(NetworkUsageDay a, NetworkUsageDay b) { return a.Date.CompareTo(b.Date); });
            return result;
            }
        }

        public void Dispose()
        {
            NetworkChange.NetworkAddressChanged -= OnNetworkChanged;
            NetworkChange.NetworkAvailabilityChanged -= OnAvailabilityChanged;
            SampleMonthlyUsage();
        }
    }

    internal sealed class SystemMetricsMonitor : IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct FileTime { public uint Low; public uint High; public ulong Value { get { return ((ulong)High << 32) | Low; } } }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MemoryStatus
        {
            public uint Length = (uint)Marshal.SizeOf(typeof(MemoryStatus));
            public uint MemoryLoad; public ulong TotalPhysical; public ulong AvailablePhysical;
            public ulong TotalPageFile; public ulong AvailablePageFile; public ulong TotalVirtual; public ulong AvailableVirtual; public ulong AvailableExtendedVirtual;
        }
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GetSystemTimes(out FileTime idle, out FileTime kernel, out FileTime user);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GlobalMemoryStatusEx([In, Out] MemoryStatus status);
        [DllImport("kernel32.dll")] private static extern ulong GetTickCount64();

        private ulong _idle, _kernel, _user;
        private readonly List<PerformanceCounter> _gpuCounters = new List<PerformanceCounter>();
        private DateTime _nextGpuCounterRefreshUtc = DateTime.MinValue;
        public float CpuPercent { get; private set; }
        public float RamPercent { get; private set; }
        public float GpuPercent { get; private set; }
        public long UsedRamBytes { get; private set; }
        public long TotalRamBytes { get; private set; }
        public string DiskText { get; private set; }
        public TimeSpan Uptime { get { return TimeSpan.FromMilliseconds(GetTickCount64()); } }

        public SystemMetricsMonitor()
        {
            RefreshGpuCounters();
            Update();
        }

        public void Update()
        {
            FileTime idle, kernel, user;
            if (GetSystemTimes(out idle, out kernel, out user))
            {
                ulong i = idle.Value, k = kernel.Value, u = user.Value;
                if (_kernel != 0 || _user != 0)
                {
                    ulong total = (k - _kernel) + (u - _user);
                    ulong idleDelta = i - _idle;
                    CpuPercent = total == 0 ? 0 : (float)Math.Max(0, Math.Min(100, (double)(total - Math.Min(total, idleDelta)) * 100.0 / total));
                }
                _idle = i; _kernel = k; _user = u;
            }

            MemoryStatus memory = new MemoryStatus();
            if (GlobalMemoryStatusEx(memory) && memory.TotalPhysical > 0)
            {
                TotalRamBytes = (long)memory.TotalPhysical;
                UsedRamBytes = (long)(memory.TotalPhysical - memory.AvailablePhysical);
                RamPercent = (float)(UsedRamBytes * 100.0 / TotalRamBytes);
            }

            if (DateTime.UtcNow >= _nextGpuCounterRefreshUtc) RefreshGpuCounters();
            double gpuUsage = 0;
            int gpuReadCount = 0;
            foreach (PerformanceCounter counter in _gpuCounters)
            {
                try
                {
                    // Task Manager's overall GPU percentage follows the busiest engine,
                    // rather than adding utilization percentages from every engine/process.
                    gpuUsage = Math.Max(gpuUsage, Math.Max(0, counter.NextValue()));
                    gpuReadCount++;
                }
                catch { }
            }
            GpuPercent = gpuReadCount == 0 ? -1 : (float)Math.Max(0, Math.Min(100, gpuUsage));

            try
            {
                long total = 0;
                long free = 0;
                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    try
                    {
                        if (!drive.IsReady || drive.DriveType != DriveType.Fixed) continue;
                        total += drive.TotalSize;
                        free += drive.AvailableFreeSpace;
                    }
                    catch { }
                }
                if (total > 0)
                {
                    long used = Math.Max(0, total - free);
                    TotalDiskBytes = total;
                    FreeDiskBytes = free;
                    UsedDiskBytes = used;
                    DiskPercent = (float)(used * 100.0 / total);
                    DiskText = FormatBytes(used) + " used · " + FormatBytes(free) + " free / " + FormatBytes(total);
                }
                else { DiskText = "Unavailable"; DiskPercent = 0; TotalDiskBytes = 0; FreeDiskBytes = 0; UsedDiskBytes = 0; }
            }
            catch { DiskText = "Unavailable"; DiskPercent = 0; TotalDiskBytes = 0; FreeDiskBytes = 0; UsedDiskBytes = 0; }

        }

        private void RefreshGpuCounters()
        {
            _nextGpuCounterRefreshUtc = DateTime.UtcNow.AddSeconds(30);
            foreach (PerformanceCounter counter in _gpuCounters) counter.Dispose();
            _gpuCounters.Clear();
            try
            {
                if (!PerformanceCounterCategory.Exists("GPU Engine")) return;
                PerformanceCounterCategory category = new PerformanceCounterCategory("GPU Engine");
                string[] names = category.GetInstanceNames();
                List<string> preferred = new List<string>();
                List<string> fallback = new List<string>();
                foreach (string name in names)
                {
                    fallback.Add(name);
                    if (name.IndexOf("engtype_3D", StringComparison.OrdinalIgnoreCase) >= 0)
                        preferred.Add(name);
                }
                string[] selected = preferred.Count > 0 ? preferred.ToArray() : fallback.ToArray();
                foreach (string name in selected)
                {
                    try { _gpuCounters.Add(new PerformanceCounter("GPU Engine", "Utilization Percentage", name, true)); }
                    catch { }
                }
            }
            catch { }
        }

        public float DiskPercent { get; private set; }
        public long TotalDiskBytes { get; private set; }
        public long UsedDiskBytes { get; private set; }
        public long FreeDiskBytes { get; private set; }
        public static string FormatBytes(double bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            int i = 0; while (bytes >= 1024 && i < units.Length - 1) { bytes /= 1024; i++; }
            return bytes.ToString(i == 0 ? "0" : "0.0", CultureInfo.InvariantCulture) + " " + units[i];
        }
        public void Dispose() { foreach (PerformanceCounter counter in _gpuCounters) counter.Dispose(); _gpuCounters.Clear(); }
    }
}