using GameReaderCommon;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace FF1600TireEstimator.Plugin.TelemetryDiscovery
{
    internal sealed class NormalizedIdentityProbe
    {
        private long cadenceWindowStartedAt = Stopwatch.GetTimestamp();
        private int cadenceWindowFrames;
        private double dataUpdateHz;
        private NormalizedIdentitySnapshot snapshot = NormalizedIdentitySnapshot.Empty;

        public NormalizedIdentitySnapshot Snapshot => Volatile.Read(ref snapshot);

        public string GetSummary()
        {
            var current = Snapshot;
            return string.Join(
                Environment.NewLine,
                "Game: " + Display(current.GameName),
                "CarId: " + Display(current.CarId),
                "CarModel: " + Display(current.CarModel),
                "SessionId: " + Display(current.SessionId),
                "GameRunning: " + current.GameRunning,
                "GamePaused: " + current.GamePaused,
                "GameReplay: " + current.GameReplay,
                "Spectating: " + current.Spectating,
                "IsInPit: " + Display(current.IsInPit),
                "IsInPitLane: " + Display(current.IsInPitLane),
                "FrameTimeUtc: " + Display(current.FrameTimeUtc),
                "DataUpdateHz: " + current.DataUpdateHz.ToString("F2", CultureInfo.InvariantCulture));
        }

        private static string Display(object value)
        {
            return value == null || string.IsNullOrEmpty(value.ToString()) ? "<unavailable>" : value.ToString();
        }

        public void Update(GameData data)
        {
            cadenceWindowFrames++;

            var now = Stopwatch.GetTimestamp();
            var elapsedSeconds = (now - cadenceWindowStartedAt) / (double)Stopwatch.Frequency;
            if (elapsedSeconds >= 1.0)
            {
                dataUpdateHz = cadenceWindowFrames / elapsedSeconds;
                cadenceWindowFrames = 0;
                cadenceWindowStartedAt = now;
            }

            var status = data.NewData;
            Volatile.Write(
                ref snapshot,
                new NormalizedIdentitySnapshot(
                    data.GameName,
                    status?.CarId,
                    status?.CarModel,
                    data.SessionId.ToString("D"),
                    data.GameRunning,
                    data.GamePaused,
                    data.GameReplay,
                    status?.Spectating ?? false,
                    status?.IsInPit,
                    status?.IsInPitLane,
                    data.FrameTimeUTC.ToString("O"),
                    dataUpdateHz));
        }
    }

    internal sealed class NormalizedIdentitySnapshot
    {
        public static readonly NormalizedIdentitySnapshot Empty =
            new NormalizedIdentitySnapshot(null, null, null, Guid.Empty.ToString("D"), false, false, false, false, null, null, null, 0.0);

        public NormalizedIdentitySnapshot(
            string gameName,
            string carId,
            string carModel,
            string sessionId,
            bool gameRunning,
            bool gamePaused,
            bool gameReplay,
            bool spectating,
            int? isInPit,
            int? isInPitLane,
            string frameTimeUtc,
            double dataUpdateHz)
        {
            GameName = gameName;
            CarId = carId;
            CarModel = carModel;
            SessionId = sessionId;
            GameRunning = gameRunning;
            GamePaused = gamePaused;
            GameReplay = gameReplay;
            Spectating = spectating;
            IsInPit = isInPit;
            IsInPitLane = isInPitLane;
            FrameTimeUtc = frameTimeUtc;
            DataUpdateHz = dataUpdateHz;
        }

        public string GameName { get; }
        public string CarId { get; }
        public string CarModel { get; }
        public string SessionId { get; }
        public bool GameRunning { get; }
        public bool GamePaused { get; }
        public bool GameReplay { get; }
        public bool Spectating { get; }
        public int? IsInPit { get; }
        public int? IsInPitLane { get; }
        public string FrameTimeUtc { get; }
        public double DataUpdateHz { get; }
    }
}
