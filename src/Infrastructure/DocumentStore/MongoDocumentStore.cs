using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EverythingMessages.Infrastructure.DocumentStore
{
    public class MongoDocumentStore : IDocumentStore
    {
        public class MongoDocumentStoreOptions
        {
            public string Url { get; set; }
            public string Database { get; set; }
            public string Collection { get; set; }
        }

        private readonly MongoClient _dbClient;
        private readonly MongoDocumentStoreOptions _options;

        private IMongoCollection<BsonDocument> GetCollection()
        {
            var database = _dbClient.GetDatabase(_options.Database);
            return database.GetCollection<BsonDocument>(_options.Collection);
        }

        private BsonDocument GetIdFilter(string id)
        {
            return new BsonDocument { { "_id", new BsonObjectId(ObjectId.Parse(id)) } };
        }

        public MongoDocumentStore(MongoDocumentStoreOptions options)
        {
            _options = options;
            _dbClient = new MongoClient(options.Url);
        }

        public async Task<string> StoreAsync(byte[] document, CancellationToken cancellationToken)
        {

            var doc = new BsonDocument { { "document", document } };

            await GetCollection().InsertOneAsync(doc, cancellationToken: cancellationToken).ConfigureAwait(false);
            return doc["_id"].AsObjectId.ToString();
        }
        public async Task<byte[]> GetAsync(string id, CancellationToken cancellationToken)
        {
            var doc = await GetCollection().Find(GetIdFilter(id))
                .SingleAsync(cancellationToken)
                .ConfigureAwait(false);

            return doc["document"].AsByteArray;
        }

        public async Task RemoveAsync(string id, CancellationToken cancellationToken)
        {
            await GetCollection()
                .DeleteOneAsync(GetIdFilter(id), cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<string[]> ListAsync(CancellationToken cancellationToken)
        {
            var ids = await GetCollection()
                .Find(_ => true)
                .Project(new BsonDocument { { "_id", 1 } })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return ids.Select(s => s["_id"].AsObjectId.ToString()).ToArray();
        }
    }
}
