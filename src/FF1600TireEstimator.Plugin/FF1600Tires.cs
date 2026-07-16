using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Windows.Media;
using FF1600TireEstimator.Plugin.TelemetryDiscovery;
using FF1600TireEstimator.Plugin.Recording;

namespace FF1600TireEstimator.Plugin
{
    [PluginDescription("Live tire temperature estimator for the iRacing FF1600")]
    [PluginAuthor("Michael Tan")]
    [PluginName("FF1600 Tire Estimator")]
    public class FF1600Tires : IPlugin, IDataPlugin, IWPFSettingsV2
    {
        private readonly NormalizedIdentityProbe normalizedIdentityProbe = new NormalizedIdentityProbe();
        private readonly RawIRacingCompatibilityProbe rawIRacingCompatibilityProbe = new RawIRacingCompatibilityProbe();
        private readonly CarcassTransitionProbe carcassTransitionProbe = new CarcassTransitionProbe();
        private readonly TelemetryRecorder telemetryRecorder = new TelemetryRecorder();

        public FF1600TiresSettings Settings;

        internal RecorderStatusSnapshot RecorderStatus => telemetryRecorder.Status;
        internal string EvidenceRoot => TelemetryRecorder.EvidenceRoot;

        internal void StartTelemetryCapture() => telemetryRecorder.StartCapture();
        internal void StopTelemetryCapture() => telemetryRecorder.StopCapture();
        internal void ToggleTelemetryCapture() => telemetryRecorder.ToggleCapture();

        internal string GetDiscoverySummary()
        {
            return string.Join(
                Environment.NewLine,
                normalizedIdentityProbe.GetSummary(),
                rawIRacingCompatibilityProbe.GetSummary(),
                carcassTransitionProbe.Snapshot.GetSummary(),
                telemetryRecorder.Status.GetSummary());
        }

        /// <summary>
        /// Instance of the current plugin manager
        /// </summary>
        public PluginManager PluginManager { get; set; }

        /// <summary>
        /// Gets the left menu icon. Icon must be 24x24 and compatible with black and white display.
        /// </summary>
        public ImageSource PictureIcon => this.ToIcon(Properties.Resources.sdkmenuicon);

        /// <summary>
        /// Gets a short plugin title to show in left menu. Return null if you want to use the title as defined in PluginName attribute.
        /// </summary>
        public string LeftMenuTitle => "FF1600 Tire Estimator";

        /// <summary>
        /// Called one time per game data update, contains all normalized game data,
        /// raw data are intentionally "hidden" under a generic object type (A plugin SHOULD NOT USE IT)
        ///
        /// This method is on the critical path, it must execute as fast as possible and avoid throwing any error
        ///
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <param name="data">Current game data, including current and previous data frame.</param>
        public void DataUpdate(PluginManager pluginManager, ref GameData data)
        {
            normalizedIdentityProbe.Update(data);
            rawIRacingCompatibilityProbe.Update(data.NewData);
            carcassTransitionProbe.Update(data);
            telemetryRecorder.Update(data);
        }

        /// <summary>
        /// Called at plugin manager stop, close/dispose anything needed here !
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void End(PluginManager pluginManager)
        {
            telemetryRecorder.Dispose();

            // Save settings
            this.SaveCommonSettings("GeneralSettings", Settings);
        }

        /// <summary>
        /// Returns the settings control, return null if no settings control is required
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <returns></returns>
        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager)
        {
            return new SettingsControlDemo(this);
        }

        /// <summary>
        /// Called once after plugins startup
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void Init(PluginManager pluginManager)
        {
            SimHub.Logging.Current.Info("Starting plugin");

            // Load settings
            Settings = this.ReadCommonSettings<FF1600TiresSettings>("GeneralSettings", () => new FF1600TiresSettings());

            // Health properties used to verify that SimHub initialized this plugin.
            this.AttachDelegate(name: "PluginAlive", valueProvider: () => 1);
            this.AttachDelegate(name: "DebugText", valueProvider: () => "FF1600 tire estimator plugin running");

            // Temporary normalized telemetry discovery display.
            this.AttachDelegate(
                name: "DiscoverySummary",
                valueProvider: GetDiscoverySummary);

            // Declare an action which can be called
            this.AddAction(
                actionName: "StartTelemetryCapture",
                actionStart: (a, b) => StartTelemetryCapture());

            this.AddAction(
                actionName: "StopTelemetryCapture",
                actionStart: (a, b) => StopTelemetryCapture());

            this.AddAction(
                actionName: "ToggleTelemetryCapture",
                actionStart: (a, b) => ToggleTelemetryCapture());
        }
    }
}
