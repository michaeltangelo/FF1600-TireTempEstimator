using GameReaderCommon;
using System;

namespace FF1600TireEstimator.Plugin.Recording
{
    internal abstract class RecorderRecord
    {
        protected RecorderRecord(string recordType, Guid sessionId, Guid captureRunId, long sequence, DateTime capturedAtUtc)
        {
            RecordType = recordType;
            SessionId = sessionId;
            CaptureRunId = captureRunId;
            Sequence = sequence;
            CapturedAtUtc = capturedAtUtc;
        }

        public string RecordType { get; }
        public Guid SessionId { get; }
        public Guid CaptureRunId { get; }
        public long Sequence { get; }
        public DateTime CapturedAtUtc { get; }
    }

    internal sealed class TelemetryRecord : RecorderRecord
    {
        public TelemetryRecord(Guid sessionId, Guid captureRunId, long sequence, DateTime capturedAtUtc, StatusDataBase status, RawIRacingFrame raw)
            : base("telemetry", sessionId, captureRunId, sequence, capturedAtUtc)
        {
            CarId = status.CarId;
            CarModel = status.CarModel;
            TrackId = status.TrackId;
            TrackConfig = status.TrackConfig;
            SessionType = status.SessionTypeName;
            CurrentLap = status.CurrentLap;
            CompletedLaps = status.CompletedLaps;
            TrackPositionPercent = status.TrackPositionPercent;
            TrackPositionMeters = status.TrackPositionMeters;
            SpeedKmh = status.SpeedKmh;
            Throttle = status.Throttle;
            Brake = status.Brake;
            Clutch = status.Clutch;
            Gear = status.Gear;
            Rpm = status.Rpms;
            AccelerationSurge = status.AccelerationSurge;
            AccelerationSway = status.AccelerationSway;
            AccelerationHeave = status.AccelerationHeave;
            YawRate = status.YawChangeVelocity;
            PitchRate = status.PitchChangeVelocity;
            RollRate = status.RollChangeVelocity;
            AirTemperature = status.AirTemperature;
            RoadTemperature = status.RoadTemperature;
            TemperatureUnit = status.TemperatureUnit;
            TirePressureUnit = status.TyrePressureUnit;
            BrakeTemperatures = new[]
            {
                status.BrakeTemperatureFrontLeft, status.BrakeTemperatureFrontRight,
                status.BrakeTemperatureRearLeft, status.BrakeTemperatureRearRight
            };
            NormalizedTireTemperatures = new[]
            {
                status.TyreTemperatureFrontLeftInner, status.TyreTemperatureFrontLeftMiddle, status.TyreTemperatureFrontLeftOuter,
                status.TyreTemperatureFrontRightInner, status.TyreTemperatureFrontRightMiddle, status.TyreTemperatureFrontRightOuter,
                status.TyreTemperatureRearLeftInner, status.TyreTemperatureRearLeftMiddle, status.TyreTemperatureRearLeftOuter,
                status.TyreTemperatureRearRightInner, status.TyreTemperatureRearRightMiddle, status.TyreTemperatureRearRightOuter
            };
            TirePressures = new[]
            {
                status.TyrePressureFrontLeft, status.TyrePressureFrontRight,
                status.TyrePressureRearLeft, status.TyrePressureRearRight
            };
            TireWear = new[]
            {
                status.TyreWearFrontLeft, status.TyreWearFrontRight,
                status.TyreWearRearLeft, status.TyreWearRearRight
            };
            IsInPit = status.IsInPit;
            IsInPitLane = status.IsInPitLane;
            Raw = raw;
        }

        public string CarId { get; }
        public string CarModel { get; }
        public string TrackId { get; }
        public string TrackConfig { get; }
        public string SessionType { get; }
        public int CurrentLap { get; }
        public int CompletedLaps { get; }
        public double TrackPositionPercent { get; }
        public double TrackPositionMeters { get; }
        public double SpeedKmh { get; }
        public double Throttle { get; }
        public double Brake { get; }
        public double Clutch { get; }
        public string Gear { get; }
        public double Rpm { get; }
        public double? AccelerationSurge { get; }
        public double? AccelerationSway { get; }
        public double? AccelerationHeave { get; }
        public double? YawRate { get; }
        public double? PitchRate { get; }
        public double? RollRate { get; }
        public double AirTemperature { get; }
        public double RoadTemperature { get; }
        public string TemperatureUnit { get; }
        public string TirePressureUnit { get; }
        public double[] BrakeTemperatures { get; }
        public double[] NormalizedTireTemperatures { get; }
        public double[] TirePressures { get; }
        public double[] TireWear { get; }
        public int IsInPit { get; }
        public int IsInPitLane { get; }
        public RawIRacingFrame Raw { get; }
    }

    internal sealed class CheckpointRecord : RecorderRecord
    {
        public CheckpointRecord(
            Guid sessionId,
            Guid captureRunId,
            long sequence,
            DateTime capturedAtUtc,
            RawIRacingFrame raw,
            double[] previousTemperatures,
            int isInPit,
            int isInPitLane,
            string detectionReason,
            Guid? previousCaptureRunId)
            : base("carcass_checkpoint", sessionId, captureRunId, sequence, capturedAtUtc)
        {
            Raw = raw;
            PreviousTemperatures = previousTemperatures;
            IsInPit = isInPit;
            IsInPitLane = isInPitLane;
            DetectionReason = detectionReason;
            PreviousCaptureRunId = previousCaptureRunId;
        }

        public RawIRacingFrame Raw { get; }
        public double[] PreviousTemperatures { get; }
        public int IsInPit { get; }
        public int IsInPitLane { get; }
        public string DetectionReason { get; }
        public Guid? PreviousCaptureRunId { get; }
    }
}
