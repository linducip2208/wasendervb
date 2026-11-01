using System;
using System.Threading.Tasks;

namespace WASender
{
    /// <summary>
    /// Jeda ala manusia + simulasi typing. Terpusat agar anti-ban konsisten.
    /// </summary>
    public static class HumanDelay
    {
        private static readonly Random _rnd = new Random();

        public static int NextSeconds(int min, int max)
        {
            if (max < min) max = min;
            return _rnd.Next(min, max + 1);
        }

        public static async Task WaitAsync(int minSec, int maxSec)
        {
            int s = NextSeconds(minSec, maxSec);
            // jitter +/-20% agar tidak kaku
            int jitter = (int)(s * 0.2 * (_rnd.NextDouble() - 0.5) * 2);
            int ms = Math.Max(500, (s * 1000) + jitter * 100);
            await Task.Delay(ms);
        }

        public static async Task WaitDefaultAsync()
        {
            try
            {
                var lim = SendGovernor.LoadLimits();
                await WaitAsync(lim.MinDelaySec, lim.MaxDelaySec);
            }
            catch (Exception) { await Task.Delay(5000); }
        }
    }
}
