using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace FF1600TireEstimator.Plugin.Recording
{
    internal static class JsonLinesEncoder
    {
        public static string Encode(RecorderRecord record)
        {
            var builder = new StringBuilder(2048);
            builder.Append('{');
            Field(builder, "schema_version", 1);
            Field(builder, "record_type", record.RecordType);
            Field(builder, "session_id", record.SessionId.ToString("D"));
            Field(builder, "capture_run_id", record.CaptureRunId.ToString("D"));
            Field(builder, "sequence", record.Sequence);
            Field(builder, "captured_at_utc", record.CapturedAtUtc.ToString("O", CultureInfo.InvariantCulture));

            var telemetry = record as TelemetryRecord;
            if (telemetry != null)
            {
                EncodeTelemetry(builder, telemetry);
            }
            else
            {
                EncodeCheckpoint(builder, (CheckpointRecord)record);
            }

            builder.Append('}');
            return builder.ToString();
        }

        private static void EncodeTelemetry(StringBuilder builder, TelemetryRecord record)
        {
            Field(builder, "car_id", record.CarId);
            Field(builder, "car_model", record.CarModel);
            Field(builder, "track_id", record.TrackId);
            Field(builder, "track_config", record.TrackConfig);
            Field(builder, "session_type", record.SessionType);
            Field(builder, "current_lap", record.CurrentLap);
            Field(builder, "completed_laps", record.CompletedLaps);
            Field(builder, "track_position_percent", record.TrackPositionPercent);
            Field(builder, "track_position_m", record.TrackPositionMeters);
            Field(builder, "speed_kmh", record.SpeedKmh);
            Field(builder, "throttle", record.Throttle);
            Field(builder, "brake", record.Brake);
            Field(builder, "clutch", record.Clutch);
            Field(builder, "gear", record.Gear);
            Field(builder, "rpm", record.Rpm);
            Field(builder, "acceleration_surge", record.AccelerationSurge);
            Field(builder, "acceleration_sway", record.AccelerationSway);
            Field(builder, "acceleration_heave", record.AccelerationHeave);
            Field(builder, "yaw_rate", record.YawRate);
            Field(builder, "pitch_rate", record.PitchRate);
            Field(builder, "roll_rate", record.RollRate);
            Field(builder, "air_temperature", record.AirTemperature);
            Field(builder, "road_temperature", record.RoadTemperature);
            Field(builder, "normalized_temperature_unit", record.TemperatureUnit);
            Field(builder, "normalized_tire_pressure_unit", record.TirePressureUnit);
            ArrayField(builder, "brake_temperatures", record.BrakeTemperatures);
            ArrayField(builder, "normalized_tire_temperatures", record.NormalizedTireTemperatures);
            ArrayField(builder, "tire_pressures", record.TirePressures);
            ArrayField(builder, "tire_wear", record.TireWear);
            Field(builder, "is_in_pit", record.IsInPit);
            Field(builder, "is_in_pit_lane", record.IsInPitLane);
            EncodeRaw(builder, record.Raw);
        }

        private static void EncodeCheckpoint(StringBuilder builder, CheckpointRecord record)
        {
            Field(builder, "is_in_pit", record.IsInPit);
            Field(builder, "is_in_pit_lane", record.IsInPitLane);
            ArrayField(builder, "previous_carcass_temperatures", record.PreviousTemperatures);
            EncodeRaw(builder, record.Raw);
        }

        private static void EncodeRaw(StringBuilder builder, RawIRacingFrame raw)
        {
            Field(builder, "raw_tick_count", raw.TickCount);
            Field(builder, "raw_session_time_s", raw.SessionTime);
            Field(builder, "raw_on_pit_road", raw.OnPitRoad);
            Field(builder, "raw_is_in_garage", raw.IsInGarage);
            Field(builder, "raw_is_replay_playing", raw.IsReplayPlaying);
            ArrayField(builder, "raw_carcass_temperatures_c", raw.CarcassTemperatures);
        }

        private static void Field(StringBuilder builder, string name, string value)
        {
            Prefix(builder, name);
            if (value == null)
            {
                builder.Append("null");
                return;
            }

            builder.Append('"').Append(Escape(value)).Append('"');
        }

        private static void Field(StringBuilder builder, string name, bool value)
        {
            Prefix(builder, name);
            builder.Append(value ? "true" : "false");
        }

        private static void Field(StringBuilder builder, string name, int value)
        {
            Prefix(builder, name);
            builder.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static void Field(StringBuilder builder, string name, long value)
        {
            Prefix(builder, name);
            builder.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static void Field(StringBuilder builder, string name, double value)
        {
            Prefix(builder, name);
            builder.Append(FormatNumber(value));
        }

        private static void Field(StringBuilder builder, string name, double? value)
        {
            Prefix(builder, name);
            builder.Append(value.HasValue ? FormatNumber(value.Value) : "null");
        }

        private static void ArrayField(StringBuilder builder, string name, double[] values)
        {
            Prefix(builder, name);
            builder.Append('[');
            builder.Append(string.Join(",", values.Select(FormatNumber)));
            builder.Append(']');
        }

        private static void Prefix(StringBuilder builder, string name)
        {
            if (builder.Length > 1)
            {
                builder.Append(',');
            }

            builder.Append('"').Append(name).Append("\":");
        }

        private static string FormatNumber(double value)
        {
            return double.IsNaN(value) || double.IsInfinity(value)
                ? "null"
                : value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string Escape(string value)
        {
            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }
    }
}
