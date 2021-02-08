using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace SfcOpServer
{
    public static class TimerHelper
    {
        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtQueryTimerResolution(out uint MinimumResolution, out uint MaximumResolution, out uint CurrentResolution);

        private static readonly double LowestSleepThreshold;

        static TimerHelper()
        {
            LowestSleepThreshold = GetLowestSleepThreshold();
        }

        public static double GetLowestSleepThreshold()
        {
            _ = NtQueryTimerResolution(out uint _, out uint max, out uint _);

            return 1.0 + (max / 10000.0);
        }

        public static double GetCurrentResolution()
        {
            _ = NtQueryTimerResolution(out uint _, out uint _, out uint current);

            return current / 10000.0;
        }

        public static void SleepForNoMoreThan(double milliseconds)
        {
            // assumption is that Thread.Sleep(t) will sleep for at least (t), and at most (t + timerResolution)

            if (milliseconds < LowestSleepThreshold)
                return;

            var sleepTime = (int)(milliseconds - GetCurrentResolution());

            if (sleepTime > 0)
                Thread.Sleep(sleepTime);
        }

        public static void SleepForNoMoreThanCurrentResolution()
        {
            Thread.Sleep((int)Math.Round(GetCurrentResolution(), MidpointRounding.AwayFromZero));
        }
    }
}
