using Microsoft.Azure.Cosmos;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ReferencesDbLibrary.Data
{
    public class CosmosData : IDatabaseData
    {
        private readonly IConfiguration _config;
        private readonly CosmosClient _cosmosClient;
        private readonly ConcurrentDictionary<string, ConversationReference> _conversationReferencesCache;
        private readonly string _databaseId;
        private readonly string _containerId;

        public CosmosData(IConfiguration config)
        {
            _config = config;
            _cosmosClient = new CosmosClient(
                accountEndpoint: _config.GetSection("ReferencesDb")["AccountEndpoint"],
                authKeyOrResourceToken: _config.GetSection("ReferencesDb")["AuthKey"]
            );
            _conversationReferencesCache = new ConcurrentDictionary<string, ConversationReference>();
            _databaseId = _config.GetSection("ReferencesDb")["DatabaseId"];
            _containerId = _config.GetSection("ReferencesDb")["ContainerId"];
        }

        public async Task AddConversationReference(string email, ConversationReference conversationReference)
        {
            var container = _cosmosClient.GetContainer(_databaseId, _containerId);

            var conversationReferenceModel = new ConversationReferenceModel
            {
                Id = email,
                PartitionKey = email,
                UserConversationReference = conversationReference
            };

            try
            {
                var response = await container.ReadItemAsync<ConversationReferenceModel>(
                    conversationReferenceModel.Id,
                    new PartitionKey(conversationReferenceModel.PartitionKey)
                );
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                var response = await container.CreateItemAsync<ConversationReferenceModel>(
                    conversationReferenceModel,
                    new PartitionKey(conversationReferenceModel.PartitionKey)
                );
                _conversationReferencesCache.TryAdd(email, conversationReference);
            }
            
        }

        public async Task<ConversationReference> GetConversationReference(string email)
        {
            if (_conversationReferencesCache.ContainsKey(email))
            {
                return _conversationReferencesCache[email];
            }

            var container = _cosmosClient.GetContainer(_databaseId, _containerId);
            QueryDefinition query = new QueryDefinition("SELECT * FROM References r WHERE r.id = @id")
                .WithParameter("@id", email);

            List<ConversationReferenceModel> results = new List<ConversationReferenceModel>();

            FeedIterator<ConversationReferenceModel> resultSetIterator = container.GetItemQueryIterator<ConversationReferenceModel>(query);
            while (resultSetIterator.HasMoreResults)
            {
                FeedResponse<ConversationReferenceModel> response = await resultSetIterator.ReadNextAsync();
                results.AddRange(response);
            }

            var conversationReference = results.FirstOrDefault()?.UserConversationReference;

            if (conversationReference != null)
            {
                _conversationReferencesCache.TryAdd(email, conversationReference);
            }

            return conversationReference; 
        }
    }
}
