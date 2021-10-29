using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JiraClientLibrary.Models
{
    public class Approval
    {
        public string IssueKey { get; set; }
        public string ApprovalId { get; set; }
        public string AccountId { get; set; }
        public string Email { get; set; }
    }
}
