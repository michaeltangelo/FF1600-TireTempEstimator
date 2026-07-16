using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace FF1600TireEstimator.Plugin
{
    public partial class SettingsControlDemo : UserControl
    {
        private readonly DispatcherTimer refreshTimer;

        public SettingsControlDemo()
        {
            InitializeComponent();
        }

        public SettingsControlDemo(FF1600Tires plugin) : this()
        {
            Plugin = plugin;
            EvidencePathValue.Text = plugin.EvidenceRoot;
            refreshTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            refreshTimer.Tick += RefreshTimer_Tick;
            Loaded += SettingsControlDemo_Loaded;
            Unloaded += SettingsControlDemo_Unloaded;
            RefreshStatus();
        }

        public FF1600Tires Plugin { get; }

        private void SettingsControlDemo_Loaded(object sender, RoutedEventArgs e)
        {
            refreshTimer?.Start();
            RefreshStatus();
        }

        private void SettingsControlDemo_Unloaded(object sender, RoutedEventArgs e)
        {
            refreshTimer?.Stop();
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            RefreshStatus();
        }

        private void StartCapture_Click(object sender, RoutedEventArgs e)
        {
            Plugin.StartTelemetryCapture();
            RefreshStatus();
        }

        private void StopCapture_Click(object sender, RoutedEventArgs e)
        {
            Plugin.StopTelemetryCapture();
            RefreshStatus();
        }

        private void ToggleCapture_Click(object sender, RoutedEventArgs e)
        {
            Plugin.ToggleTelemetryCapture();
            RefreshStatus();
        }

        private void RefreshStatus()
        {
            if (Plugin == null)
            {
                return;
            }

            var status = Plugin.RecorderStatus;
            StateValue.Text = status.State;
            RequestedValue.Text = status.Requested.ToString();
            RunIdValue.Text = status.RunId == Guid.Empty ? "<none>" : status.RunId.ToString("D");
            AcceptedValue.Text = status.Accepted.ToString(CultureInfo.InvariantCulture);
            RejectedValue.Text = status.Rejected.ToString(CultureInfo.InvariantCulture);
            FaultValue.Text = string.IsNullOrEmpty(status.Fault) ? "<none>" : status.Fault;
            DiagnosticsValue.Text = Plugin.GetDiscoverySummary();
        }
    }
}
