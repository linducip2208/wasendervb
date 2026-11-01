using System;

namespace WASender
{
    /// <summary>
    /// Handover AI -&gt; manusia. Jika pesan mengandung kata kunci, jangan auto-reply AI.
    /// Dipakai di WaAutoReplyForm sebelum AiResponder.ReplyAsync.
    /// </summary>
    public static class AiHandover
    {
        private static readonly string[] DefaultKeywords = new string[]
        {
            "admin", "manusia", "operator", "cs", "call", "telpon", "telepon",
            "komplain", "refund", "batal", "human", "agent"
        };

        public static bool NeedsHuman(string incomingText)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(incomingText)) return false;
                string t = incomingText.ToLowerInvariant();
                foreach (var k in DefaultKeywords)
                    if (t.Contains(k)) return true;
                // repeat frustrasi: "???" atau CAPS panjang
                if (t.Contains("???")) return true;
                return false;
            }
            catch (Exception) { return false; }
        }
    }
}
