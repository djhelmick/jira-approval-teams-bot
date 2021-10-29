using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApprovalBotAPI.Models
{
    public class NotifyRequest
    {
        public dynamic Transition { get; set; }
        public string Comment { get; set; }
        public dynamic User { get; set; }
        public dynamic Issue { get; set; }
        public long Timestamp { get; set; }
    }
}
