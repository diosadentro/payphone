using MongoDB.Driver;
using BCD.Payphone.Lib;

namespace BCD.Payphone.Data
{
    public class MongoClientFactory : IDatabaseClientFactory
    {
        private readonly BCDConfiguration Configuration;
        private Dictionary<string, IMongoCollection<object>> MongoDatabaseClientDictionary;


        public MongoClientFactory(BCDConfiguration configuration)
        {
            Configuration = configuration;
            MongoDatabaseClientDictionary = new Dictionary<string, IMongoCollection<object>>();
        }

        private MongoClient GenerateMongoClient()
        {
            var settings = new MongoClientSettings
            {
                Server = new MongoServerAddress(Configuration.Database!.Server, Configuration.Database!.Port),
                Credential = MongoCredential.CreateCredential("admin", Configuration.Database!.Username, Configuration.Database!.Password),
                ConnectTimeout = TimeSpan.FromSeconds(30),
                RetryWrites = true,
                UseTls = false
            };

            return new MongoClient(settings);
        }

        public IMongoCollection<T>? GetClient<T>()
        {
            var collectionName = typeof(T).Name.ToLower();
            if (MongoDatabaseClientDictionary.ContainsKey($"{Configuration.Database!.Database}:{collectionName}"))
            {
                var collection = MongoDatabaseClientDictionary[$"{Configuration.Database!.Database}:{collectionName}"];
                return collection as IMongoCollection<T>;
            }
            else
            {
                var client = GenerateMongoClient();
                var database = client.GetDatabase(Configuration.Database!.Database);
                var collection = database.GetCollection<T>(collectionName);

                if (collection == null)
                {
                    throw new Exception($"Failed to generate collection object from database {Configuration.Database!.Database} and collection {collectionName}");
                }

                var collectionGeneric = collection as IMongoCollection<object>;
                if (collectionGeneric != null)
                {
                    MongoDatabaseClientDictionary.Add($"{Configuration.Database!.Database}:{collectionName}", collectionGeneric);
                }

                return collection;
            }
        }
    }
}
