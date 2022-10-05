using MongoDB.Bson;

namespace MigrationExampleTests.MigrationModels;

public interface IBaseModel
{
    int SchemaVersion { get; set; }
    BsonDocument CatchAll { get; set; }
    bool HasVersionBeenUpgraded { get; }
    bool HasVersionBeenDowngraded { get; }
}