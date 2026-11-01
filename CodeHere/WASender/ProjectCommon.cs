using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WASender.enums;
using WASender.Models;

namespace WASender
{
    public static class ProjectCommon
    {
        public static void injectWapi(IWebDriver driver)
        {
            WAPIHelper.injectWapi(driver, Config.WAPIFolderFolder());
            Thread.Sleep(1000);
        }

        public static string sanitiseNumber(string number)
        {
            number = number.Replace(" ", "").Replace("+", "").Replace("\\", "").Replace("/", "").Replace("-", "");
            return number;
        }

        public static string ReplaceKeyMarker(string text, List<ParameterModel> parameterModelList = null)
        {
            var messages = text.Split('\n');
            string NewMessage = "";
            int _counter = 1;
            foreach (var m in messages)
            {
                if (m != "")
                {
                    string MsgLine = m;

                    // Check For KeyMarker
                    if (m.Contains("{{ KEY :"))
                    {
                        string str = Utils.ExtractBetweenTwoStrings(m, "{{ KEY :", "}}", false, false);
                        var Keysplitter = str.Split('|');
                        string randomKey = Keysplitter[Utils.getRandom(0, Keysplitter.Length - 1)];
                        MsgLine = m.Replace("{{ KEY :" + str + "}}", randomKey);
                    }
                    // Check {{ RANDOM }}
                    if (MsgLine.Contains("{{ RANDOM }}"))
                    {
                        string rand = Utils.getRandom(10000, 50000).ToString();
                        MsgLine = MsgLine.Replace("{{ RANDOM }}", rand);
                    }
                    if (MsgLine.Contains("{{sys.date}}"))
                    {
                        string rand = Utils.getRandom(10000, 50000).ToString();
                        MsgLine = MsgLine.Replace("{{sys.date}}", Utils.exactDatetime());
                    }

                    if (parameterModelList != null)
                    {
                        foreach (var param in parameterModelList)
                        {
                            if (MsgLine.ToLower().Contains("{{" + param.ParameterName.ToLower() + "}}"))
                            {
                                MsgLine = MsgLine.Replace("{{" + param.ParameterName + "}}", param.ParameterValue);
                            }
                        }
                    }

                    if (_counter < messages.Count())
                    {
                        MsgLine = MsgLine + "\n";
                        NewMessage = NewMessage + MsgLine;
                    }
                    else
                    {
                        NewMessage = NewMessage + MsgLine;
                    }


                    _counter++;
                }
            }
            NewMessage = SpinText(NewMessage);
            NewMessage = NewMessage.Replace("`", "");
            return NewMessage;
        }

        private static readonly Random _spinRnd = new Random();

        /// <summary>
        /// Spintax {opsi1|opsi2|...} dgn dukungan nested. Hanya kurung yg berisi
        /// pipe yg diproses, jadi {{param}} / teks biasa aman tidak tersentuh.
        /// Dipakai semua jalur kirim (single/group/broadcast/auto-reply) via ReplaceKeyMarker.
        /// </summary>
        public static string SpinText(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0 || text.IndexOf('|') < 0) return text;
            try
            {
                var re = new System.Text.RegularExpressions.Regex(@"\{([^{}]*\|[^{}]*)\}");
                int guard = 0;
                while (guard++ < 50 && re.IsMatch(text))
                {
                    text = re.Replace(text, m =>
                    {
                        try
                        {
                            string[] opts = m.Groups[1].Value.Split('|');
                            if (opts == null || opts.Length == 0) return m.Value;
                            lock (_spinRnd) { return opts[_spinRnd.Next(0, opts.Length)]; }
                        }
                        catch (Exception) { return m.Value; }
                    });
                }
            }
            catch (Exception) { }
            return text;
        }


        public static void initWABrowser(WaSenderBrowser browser, InitStatusEnum _initStatus, InitStatusEnum initStatusEnum, Label lblInitStatus)
        {
            if (Utils.waSenderBrowser != null)
            {
                browser = Utils.waSenderBrowser;
            }
            else
            {
                browser = new WaSenderBrowser();
                Utils.waSenderBrowser = browser;
                browser.Show();
            }
        }

        //private void ChangeInitStatus(InitStatusEnum _initStatus, InitStatusEnum initStatusEnum, Label lblInitStatus)
        //{
        //    initStatusEnum = _initStatus;
        //    AutomationCommon.ChangeInitStatus(_initStatus, lblInitStatus);
        //}
    }

}
