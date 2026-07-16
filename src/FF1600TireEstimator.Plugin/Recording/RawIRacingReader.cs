using GameReaderCommon;
using System;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace FF1600TireEstimator.Plugin.Recording
{
    internal sealed class RawIRacingReader
    {
        private static readonly string[] TemperatureNames =
        {
            "LFtempCL", "LFtempCM", "LFtempCR",
            "RFtempCL", "RFtempCM", "RFtempCR",
            "LRtempCL", "LRtempCM", "LRtempCR",
            "RRtempCL", "RRtempCM", "RRtempCR"
        };

        private Type wrapperType;
        private PropertyInfo telemetryProperty;
        private PropertyInfo tickCountProperty;
        private PropertyInfo sessionTimeProperty;
        private PropertyInfo onPitRoadProperty;
        private PropertyInfo isInGarageProperty;
        private PropertyInfo isReplayPlayingProperty;
        private PropertyInfo[] temperatureProperties;

        public bool TryRead(StatusDataBase status, out RawIRacingFrame frame)
        {
            frame = null;
            var wrapper = status?.GetRawDataObject();
            if (wrapper == null)
            {
                return false;
            }

            EnsureAccessors(wrapper);
            var telemetry = telemetryProperty.GetValue(wrapper, null);
            if (telemetry == null)
            {
                return false;
            }

            frame = new RawIRacingFrame(
                Convert.ToInt32(tickCountProperty.GetValue(telemetry, null), CultureInfo.InvariantCulture),
                Convert.ToDouble(sessionTimeProperty.GetValue(telemetry, null), CultureInfo.InvariantCulture),
                Convert.ToBoolean(onPitRoadProperty.GetValue(telemetry, null), CultureInfo.InvariantCulture),
                Convert.ToBoolean(isInGarageProperty.GetValue(telemetry, null), CultureInfo.InvariantCulture),
                Convert.ToBoolean(isReplayPlayingProperty.GetValue(telemetry, null), CultureInfo.InvariantCulture),
                temperatureProperties
                    .Select(property => Convert.ToDouble(property.GetValue(telemetry, null), CultureInfo.InvariantCulture))
                    .ToArray());
            return true;
        }

        private void EnsureAccessors(object wrapper)
        {
            if (wrapperType == wrapper.GetType() && telemetryProperty != null)
            {
                return;
            }

            wrapperType = wrapper.GetType();
            telemetryProperty = RequireProperty(wrapperType, "Telemetry");
            var telemetryType = telemetryProperty.PropertyType;
            tickCountProperty = RequireProperty(telemetryType, "TickCount");
            sessionTimeProperty = RequireProperty(telemetryType, "SessionTime");
            onPitRoadProperty = RequireProperty(telemetryType, "OnPitRoad");
            isInGarageProperty = RequireProperty(telemetryType, "IsInGarage");
            isReplayPlayingProperty = RequireProperty(telemetryType, "IsReplayPlaying");
            temperatureProperties = TemperatureNames
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
    }

    internal sealed class RawIRacingFrame
    {
        public RawIRacingFrame(
            int tickCount,
            double sessionTime,
            bool onPitRoad,
            bool isInGarage,
            bool isReplayPlaying,
            double[] carcassTemperatures)
        {
            TickCount = tickCount;
            SessionTime = sessionTime;
            OnPitRoad = onPitRoad;
            IsInGarage = isInGarage;
            IsReplayPlaying = isReplayPlaying;
            CarcassTemperatures = carcassTemperatures;
        }

        public int TickCount { get; }
        public double SessionTime { get; }
        public bool OnPitRoad { get; }
        public bool IsInGarage { get; }
        public bool IsReplayPlaying { get; }
        public double[] CarcassTemperatures { get; }
    }
}
