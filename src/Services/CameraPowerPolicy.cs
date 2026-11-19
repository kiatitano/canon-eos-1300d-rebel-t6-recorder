namespace RecorderStudio
{
    public static class CameraPowerPolicy
    {
        public const long IdleRestMilliseconds = 60000;
        public static bool ShouldRest(bool enabled, bool recording, bool preview, bool transferring, bool focusing, bool recovering, long idleMilliseconds)
        {
            return enabled && !recording && !preview && !transferring && !focusing && !recovering && idleMilliseconds >= IdleRestMilliseconds;
        }
        public static uint Output(bool enabled, bool preview, bool recording)
        {
            // The tested 1300D requires live movie mode for recording; never remove it mid-clip.
            return enabled && !preview && !recording ? 0u : 3u;
        }
    }
}
