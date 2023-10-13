using MongoDB.Driver;

namespace BCD.Payphone.Data
{
    public interface IDatabaseClientFactory
    {
        IMongoCollection<T>? GetClient<T>();
    }
}

