using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApprovalBotAPI.Models
{
    public class JiraIssue
    {
        public string Key { get; set; }
        public string Summary { get; set; }
        public string Description { get; set; }
        public string Approver { get; set; }
    }
}
