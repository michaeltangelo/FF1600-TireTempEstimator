using GameReaderCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Threading;

namespace FF1600TireEstimator.Plugin.TelemetryDiscovery
{
    internal sealed class RawIRacingCompatibilityProbe
    {
        private static readonly string[] PropertyPaths =
        {
            "SessionUniqueID",
            "SessionTime",
            "TickCount",
            "IsInGarage",
            "OnPitRoad",
            "IsReplayPlaying",
            "SessionData.WeekendInfo.SessionID",
            "SessionData.WeekendInfo.SubSessionID",
            "LFtempCL",
            "LFtempCM",
            "LFtempCR",
            "RFtempCL",
            "RFtempCM",
            "RFtempCR",
            "LRtempCL",
            "LRtempCM",
            "LRtempCR",
            "RRtempCL",
            "RRtempCM",
            "RRtempCR"
        };

        private long nextSampleAt;
        private RawIRacingCompatibilitySnapshot snapshot = RawIRacingCompatibilitySnapshot.Empty;

        public RawIRacingCompatibilitySnapshot Snapshot => Volatile.Read(ref snapshot);

        public void Update(StatusDataBase status)
        {
            var now = Stopwatch.GetTimestamp();
            if (now < Interlocked.Read(ref nextSampleAt))
            {
                return;
            }

            Interlocked.Exchange(ref nextSampleAt, now + Stopwatch.Frequency);

            if (status == null)
            {
                Volatile.Write(ref snapshot, RawIRacingCompatibilitySnapshot.Empty);
                return;
            }

            try
            {
                var raw = status.GetRawDataObject();
                if (raw == null)
                {
                    Volatile.Write(
                        ref snapshot,
                        new RawIRacingCompatibilitySnapshot("<null>", "Raw object unavailable", new Dictionary<string, string>()));
                    return;
                }

                var values = new Dictionary<string, string>();
                foreach (var path in PropertyPaths)
                {
                    values[path] = ReadPath(raw, path);
                }

                Volatile.Write(
                    ref snapshot,
                    new RawIRacingCompatibilitySnapshot(raw.GetType().AssemblyQualifiedName, null, values));
            }
            catch (Exception ex)
            {
                Volatile.Write(
                    ref snapshot,
                    new RawIRacingCompatibilitySnapshot("<error>", ex.GetType().Name + ": " + ex.Message, new Dictionary<string, string>()));
            }
        }

        public string GetSummary()
        {
            var current = Snapshot;
            var lines = new List<string>
            {
                "--- Raw iRacing compatibility (1 Hz) ---",
                "RawType: " + current.RawType,
                "RawError: " + Display(current.Error)
            };

            foreach (var path in PropertyPaths)
            {
                string value;
                lines.Add("Raw." + path + ": " + (current.Values.TryGetValue(path, out value) ? value : "<unavailable>"));
            }

            return string.Join(Environment.NewLine, lines);
        }

        private static string ReadPath(object root, string path)
        {
            object current = root;
            foreach (var segment in path.Split('.'))
            {
                if (current == null)
                {
                    return "<null>";
                }

                var property = current.GetType().GetProperty(segment, BindingFlags.Public | BindingFlags.Instance);
                if (property == null)
                {
                    return "<missing>";
                }

                current = property.GetValue(current, null);
            }

            if (current == null)
            {
                return "<null>";
            }

            var formattable = current as IFormattable;
            return formattable == null
                ? current.ToString()
                : formattable.ToString(null, CultureInfo.InvariantCulture);
        }

        private static string Display(string value)
        {
            return string.IsNullOrEmpty(value) ? "<none>" : value;
        }
    }

    internal sealed class RawIRacingCompatibilitySnapshot
    {
        public static readonly RawIRacingCompatibilitySnapshot Empty =
            new RawIRacingCompatibilitySnapshot("<unavailable>", null, new Dictionary<string, string>());

        public RawIRacingCompatibilitySnapshot(string rawType, string error, IDictionary<string, string> values)
        {
            RawType = rawType;
            Error = error;
            Values = new Dictionary<string, string>(values);
        }

        public string RawType { get; }
        public string Error { get; }
        public IDictionary<string, string> Values { get; }
    }
}
