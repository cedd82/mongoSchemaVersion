using MongoDB.Driver;

namespace MigrationExampleTests.Repository;

public interface IHasMongoId
{
    string Id { get; set; }
}

public class TestRepository
{
    private readonly string _collectionName = "testCollection";
    private readonly IMongoDatabase _mongoDatabase;

    public TestRepository(string dbConnectionString, string mongoDbConnectionString)
    {
        var connectionString = "mongodb://localhost:27017";
        var mongoClient = new MongoClient(connectionString);
        _mongoDatabase = mongoClient.GetDatabase("testDb");
    }

    public void DropDatabase()
    {
        _mongoDatabase.Client.DropDatabase("testDb");
    }

    public T GetFirst<T>(string id) where T : IHasMongoId
    {
        return _mongoDatabase.GetCollection<T>(_collectionName).Find(x => x.Id == id).First();
    }

    public void Insert<T>(T document) where T : IHasMongoId
    {
        var collection = _mongoDatabase.GetCollection<T>(_collectionName);
        collection.InsertOne(document);
    }

    public void Upsert<T>(T testData) where T : IHasMongoId
    {
        _mongoDatabase.GetCollection<T>(_collectionName).ReplaceOne(x => x.Id == testData.Id, testData, new ReplaceOptions { IsUpsert = true });
    }
}