using System;

namespace RecorderStudio
{
    /// <summary>Camera-reported warning state, never a Celsius measurement.
    /// A warning latches a session lock; a later normal/unavailable sample cannot restart recording.
    /// </summary>
    public sealed class ThermalProtection
    {
        public const uint TemperatureProperty = 0x01000415;
        public const uint LegacyTemperatureProperty = 0x00000415;
        public uint? Reading { get; private set; }
        public bool CoolingLock { get; private set; }
        public bool Available { get { return Reading.HasValue; } }
        public string Summary { get { return !Available ? "Unavailable" : Reading.Value == 0 ? "No warning reported" : "Warning reported"; } }

        public void Update(uint? reading)
        {
            Reading = reading == uint.MaxValue ? null : reading;
            // Stop conservatively on any reported nonzero warning, including future warning codes.
            if (Reading.HasValue && Reading.Value != 0) CoolingLock = true;
        }

        public static uint? Probe(Func<uint, uint?> read)
        {
            foreach (uint property in new[] { TemperatureProperty, LegacyTemperatureProperty })
            {
                try
                {
                    uint? value = read(property);
                    if (value.HasValue && value.Value != uint.MaxValue) return value;
                }
                catch { /* Unsupported telemetry must not disconnect an otherwise usable camera. */ }
            }
            return null;
        }
    }
}
