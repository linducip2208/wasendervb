using Microsoft.Web.WebView2.WinForms;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace WASender
{
    public class SendJob
    {
        public string ToDigits { get; set; } = "";
        public string Text { get; set; } = "";
        public string MediaPath { get; set; } = "";
        public int Slot { get; set; }
    }

    public class SendResult
    {
        public bool Ok { get; set; }
        public string Info { get; set; } = "";
    }

    /// <summary>
    /// Antrian kirim beneran: 1 worker, jeda manusia, cek Governor, rotasi slot.
    /// UI cukup Enqueue; jangan panggil SendEngine langsung dari button.
    /// </summary>
    public static class SendQueue
    {
        private static readonly ConcurrentQueue<SendJob> _q = new ConcurrentQueue<SendJob>();
        private static int _running;
        private static Func<int, WebView2> _resolver;

        public static int Pending => _q.Count;

        public static void BindResolver(Func<int, WebView2> resolver) { _resolver = resolver; }

        public static void Enqueue(SendJob job) { if (job != null) _q.Enqueue(job); }

        public static void Start()
        {
            if (Interlocked.CompareExchange(ref _running, 1, 0) != 0) return;
            Task.Run(WorkerAsync);
        }

        private static async Task WorkerAsync()
        {
            try
            {
                while (true)
                {
                    SendJob job;
                    if (!_q.TryDequeue(out job))
                    {
                        await Task.Delay(800);
                        continue;
                    }
                    try
                    {
                        var res = await ProcessOneAsync(job);
                        SendEngine.Log("queue slot=" + job.Slot + " to=" + job.ToDigits + " ok=" + res.Ok + " " + res.Info);
                    }
                    catch (Exception ex) { SendEngine.Log("queue EX " + ex.Message); }
                    await HumanDelay.WaitDefaultAsync();
                }
            }
            catch (Exception) { _running = 0; }
        }

        private static async Task<SendResult> ProcessOneAsync(SendJob job)
        {
            try
            {
                if (_resolver == null) return new SendResult { Ok = false, Info = "no-resolver" };
                var wv = _resolver(job.Slot);
                if (wv == null) return new SendResult { Ok = false, Info = "no-webview slot " + job.Slot };

                var gate = await SendGovernor.CanSendAsync(wv, null, 2);
                if (gate != null && !gate.Item1) return new SendResult { Ok = false, Info = gate.Item2 };

                bool sent;
                if (!string.IsNullOrEmpty(job.MediaPath))
                {
                    try
                    {
                        string b64 = System.Convert.ToBase64String(System.IO.File.ReadAllBytes(job.MediaPath));
                        string fn = System.IO.Path.GetFileName(job.MediaPath);
                        sent = await WPPHelper.SendAttachmentsync(wv, job.ToDigits, b64, fn, job.Text ?? "");
                    }
                    catch (Exception ex) { return new SendResult { Ok = false, Info = ex.Message }; }
                }
                else
                    sent = await WPPHelper.SendMessageSync(wv, job.ToDigits, job.Text ?? "", false, false, null);

                if (sent)
                {
                    try
                    {
                        string acct = await SendGovernor.ResolveAccountAsync(wv, null, 2);
                        SendGovernor.RecordSent(acct);
                    }
                    catch (Exception) { }
                    return new SendResult { Ok = true };
                }
                return new SendResult { Ok = false, Info = "send-false" };
            }
            catch (Exception ex) { return new SendResult { Ok = false, Info = ex.Message }; }
        }
    }
}
