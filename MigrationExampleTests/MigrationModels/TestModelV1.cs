using MigrationExampleTests.Repository;
using MongoDB.Bson;

namespace MigrationExampleTests.MigrationModels;

public class TestModelV1 : IBaseModel, IHasMongoId
{
    public string Id { get; set; }
    public bool BoolPropertyToRemove { get; set; }
    public DateTime? TestDate { get; set; }

    public override void EndInit()
    {
        if (SchemaVersion != MongoSchemaVersion.TransactionInformation)
            throw new MongoSchemaMigrationMissingException(GetType().Name, SchemaVersion);
    }

    public int SchemaVersion { get; set; }
    public BsonDocument CatchAll { get; set; }
    public bool HasVersionBeenUpgraded { get; }
    public bool HasVersionBeenDowngraded { get; }
}