using GameReaderCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace FF1600TireEstimator.Plugin.TelemetryDiscovery
{
    internal sealed class CarcassTransitionProbe
    {
        private static readonly string[] ChannelNames =
        {
            "LF.CL", "LF.CM", "LF.CR",
            "RF.CL", "RF.CM", "RF.CR",
            "LR.CL", "LR.CM", "LR.CR",
            "RR.CL", "RR.CM", "RR.CR"
        };

        private static readonly string[] TelemetryPropertyNames =
        {
            "LFtempCL", "LFtempCM", "LFtempCR",
            "RFtempCL", "RFtempCM", "RFtempCR",
            "LRtempCL", "LRtempCM", "LRtempCR",
            "RRtempCL", "RRtempCM", "RRtempCR"
        };

        private PropertyInfo telemetryProperty;
        private PropertyInfo tickCountProperty;
        private PropertyInfo sessionTimeProperty;
        private PropertyInfo onPitRoadProperty;
        private PropertyInfo[] temperatureProperties;
        private Type wrapperType;
        private Type telemetryType;
        private Guid? activeSessionId;
        private double[] previousTemperatures;
        private bool? previousOnPitRoad;
        private long samples;
        private long pitEntries;
        private long changeEvents;
        private long totalCalls;
        private long measuredCalls;
        private long totalElapsedTicks;
        private long maxElapsedTicks;
        private CarcassTransitionSnapshot snapshot = CarcassTransitionSnapshot.Empty;

        public CarcassTransitionSnapshot Snapshot => Volatile.Read(ref snapshot);

        public void Update(GameData data)
        {
            var startedAt = Stopwatch.GetTimestamp();
            try
            {
                UpdateCore(data);
            }
            catch (Exception ex)
            {
                Publish("Error: " + ex.GetType().Name + ": " + ex.Message, null, null);
            }
            finally
            {
                var elapsed = Stopwatch.GetTimestamp() - startedAt;
                totalCalls++;
                if (totalCalls > 120)
                {
                    measuredCalls++;
                    totalElapsedTicks += elapsed;
                    if (elapsed > maxElapsedTicks)
                    {
                        maxElapsedTicks = elapsed;
                    }
                }
            }
        }

        private void UpdateCore(GameData data)
        {
            var status = data.NewData;
            if (!data.GameRunning || status == null || !string.Equals(status.CarId, "raygr22", StringComparison.Ordinal))
            {
                ResetFrameState();
                Publish("Waiting for active FF1600 telemetry", null, null);
                return;
            }

            if (activeSessionId != data.SessionId)
            {
                ResetSessionState(data.SessionId);
            }

            var wrapper = status.GetRawDataObject();
            if (wrapper == null)
            {
                ResetFrameState();
                Publish("Raw wrapper unavailable", null, null);
                return;
            }

            EnsureAccessors(wrapper);
            var telemetry = telemetryProperty.GetValue(wrapper, null);
            if (telemetry == null)
            {
                ResetFrameState();
                Publish("Telemetry payload unavailable", null, null);
                return;
            }

            var current = temperatureProperties
                .Select(property => Convert.ToDouble(property.GetValue(telemetry, null), CultureInfo.InvariantCulture))
                .ToArray();
            var tick = Convert.ToInt32(tickCountProperty.GetValue(telemetry, null), CultureInfo.InvariantCulture);
            var sessionTime = Convert.ToDouble(sessionTimeProperty.GetValue(telemetry, null), CultureInfo.InvariantCulture);
            var onPitRoad = Convert.ToBoolean(onPitRoadProperty.GetValue(telemetry, null), CultureInfo.InvariantCulture);
            samples++;

            string pitEntrySummary = null;
            string temperatureChangeSummary = null;
            if (previousOnPitRoad == false && onPitRoad)
            {
                pitEntries++;
                pitEntrySummary = "Pit entry at tick " + tick + ", session " + Format(sessionTime);
            }

            if (previousTemperatures != null)
            {
                var changed = new List<string>();
                for (var index = 0; index < current.Length; index++)
                {
                    if (Math.Abs(current[index] - previousTemperatures[index]) > 0.000001)
                    {
                        changed.Add(ChannelNames[index]);
                    }
                }

                if (changed.Count > 0)
                {
                    changeEvents++;
                    temperatureChangeSummary = string.Join(
                        Environment.NewLine,
                        "Temperature change at tick " + tick + ", session " + Format(sessionTime),
                        "Changed: " + changed.Count + "/12 (" + string.Join(", ", changed) + ")",
                        "AllChangedSameSample: " + (changed.Count == 12),
                        "Before: " + FormatTemperatures(previousTemperatures),
                        "After:  " + FormatTemperatures(current));
                }
            }

            previousTemperatures = current;
            previousOnPitRoad = onPitRoad;
            Publish(
                "Active; OnPitRoad=" + onPitRoad + ", Tick=" + tick + ", Session=" + Format(sessionTime),
                pitEntrySummary,
                temperatureChangeSummary);
        }

        private void EnsureAccessors(object wrapper)
        {
            if (wrapperType == wrapper.GetType() && telemetryProperty != null)
            {
                return;
            }

            wrapperType = wrapper.GetType();
            telemetryProperty = RequireProperty(wrapperType, "Telemetry");
            telemetryType = telemetryProperty.PropertyType;
            tickCountProperty = RequireProperty(telemetryType, "TickCount");
            sessionTimeProperty = RequireProperty(telemetryType, "SessionTime");
            onPitRoadProperty = RequireProperty(telemetryType, "OnPitRoad");
            temperatureProperties = TelemetryPropertyNames
                .Select(name => RequireProperty(telemetryType, name))
                .ToArray();
        }

        private static PropertyInfo RequireProperty(Type type, string name)
        {
            var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (property == null)
            {
                throw new MissingMemberException(type.FullName, name);
            }

            return property;
        }

        private void ResetFrameState()
        {
            previousTemperatures = null;
            previousOnPitRoad = null;
        }

        private void ResetSessionState(Guid sessionId)
        {
            activeSessionId = sessionId;
            ResetFrameState();
            samples = 0;
            pitEntries = 0;
            changeEvents = 0;
            totalCalls = 0;
            measuredCalls = 0;
            totalElapsedTicks = 0;
            maxElapsedTicks = 0;
            Volatile.Write(ref snapshot, CarcassTransitionSnapshot.Empty);
        }

        private void Publish(string status, string pitEntrySummary, string temperatureChangeSummary)
        {
            var averageMicroseconds = measuredCalls == 0
                ? 0.0
                : totalElapsedTicks * 1000000.0 / Stopwatch.Frequency / measuredCalls;
            var maxMicroseconds = maxElapsedTicks * 1000000.0 / Stopwatch.Frequency;
            var previous = Snapshot;

            Volatile.Write(
                ref snapshot,
                new CarcassTransitionSnapshot(
                    status,
                    samples,
                    pitEntries,
                    changeEvents,
                    measuredCalls,
                    averageMicroseconds,
                    maxMicroseconds,
                    pitEntrySummary ?? previous.LastPitEntry,
                    temperatureChangeSummary ?? previous.LastTemperatureChange));
        }

        private static string Format(double value)
        {
            return value.ToString("0.#####", CultureInfo.InvariantCulture);
        }

        private static string FormatTemperatures(IEnumerable<double> values)
        {
            return string.Join(", ", ChannelNames.Zip(values, (name, value) => name + "=" + Format(value)));
        }
    }

    internal sealed class CarcassTransitionSnapshot
    {
        public static readonly CarcassTransitionSnapshot Empty =
            new CarcassTransitionSnapshot("Not sampled", 0, 0, 0, 0, 0.0, 0.0, null, null);

        public CarcassTransitionSnapshot(
            string status,
            long samples,
            long pitEntries,
            long changeEvents,
            long timingSamples,
            double averageMicroseconds,
            double maxMicroseconds,
            string lastPitEntry,
            string lastTemperatureChange)
        {
            Status = status;
            Samples = samples;
            PitEntries = pitEntries;
            ChangeEvents = changeEvents;
            TimingSamples = timingSamples;
            AverageMicroseconds = averageMicroseconds;
            MaxMicroseconds = maxMicroseconds;
            LastPitEntry = lastPitEntry;
            LastTemperatureChange = lastTemperatureChange;
        }

        public string Status { get; }
        public long Samples { get; }
        public long PitEntries { get; }
        public long ChangeEvents { get; }
        public long TimingSamples { get; }
        public double AverageMicroseconds { get; }
        public double MaxMicroseconds { get; }
        public string LastPitEntry { get; }
        public string LastTemperatureChange { get; }

        public string GetSummary()
        {
            return string.Join(
                Environment.NewLine,
                "--- Carcass transition probe (full cadence) ---",
                "Status: " + Status,
                "Samples: " + Samples,
                "PitEntries: " + PitEntries,
                "ChangeEvents: " + ChangeEvents,
                "TimingSamplesAfterWarmup: " + TimingSamples,
                "AverageProbeUs: " + AverageMicroseconds.ToString("F2", CultureInfo.InvariantCulture),
                "MaxProbeUs: " + MaxMicroseconds.ToString("F2", CultureInfo.InvariantCulture),
                "LastPitEntry:",
                string.IsNullOrEmpty(LastPitEntry) ? "<none>" : LastPitEntry,
                "LastTemperatureChange:",
                string.IsNullOrEmpty(LastTemperatureChange) ? "<none>" : LastTemperatureChange);
        }
    }
}
