using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReferencesDbLibrary
{
    public interface IDatabaseData
    {
        Task AddConversationReference(string email, ConversationReference conversationReference);
        Task<ConversationReference> GetConversationReference(string email);
    }
}
