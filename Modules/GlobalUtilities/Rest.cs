using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaaiVan.Modules.GlobalUtilities
{
    public class RestAPI
    {
        private static Dictionary<string, Guid> _Tickets = new Dictionary<string, Guid>();

        public static void new_ticket(string ticket, Guid userId)
        {
            _Tickets[ticket] = userId;
        }

        public static Guid? get_user_id(string ticket)
        {
            if (string.IsNullOrEmpty(ticket) || !_Tickets.ContainsKey(ticket)) return null;
            else return _Tickets[ticket];
        }

        public static string get_ticket(Guid userId) {
            return _Tickets.Keys.Where(u => _Tickets[u] == userId).FirstOrDefault();
        }
    }
}
