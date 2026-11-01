using System.Collections.Generic;
using WASender.Models;

namespace WASender.Model
{
    /// <summary>
    /// Satu slot akun, pengganti duplikasi ...1..10 di WaSenderForm.
    /// Migrasi bertahap: isi dari pollModelListN / buttonsModelListN dipindah ke sini.
    /// </summary>
    public class AccountState
    {
        public int Slot { get; set; }
        public string SessionId { get; set; } = "";
        public string AccountDigits { get; set; } = "";
        public List<PollModel> Polls { get; set; } = new List<PollModel>();
        public List<ButtonHolderModel> Buttons { get; set; } = new List<ButtonHolderModel>();
        public bool Connected { get; set; }

        public static List<AccountState> CreateSlots(int n = 10)
        {
            var list = new List<AccountState>();
            for (int i = 0; i < n; i++) list.Add(new AccountState { Slot = i });
            return list;
        }
    }
}
