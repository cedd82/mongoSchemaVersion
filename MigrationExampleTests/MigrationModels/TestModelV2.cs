using MigrationExampleTests.Repository;
using MongoDB.Bson;

namespace MigrationExampleTests.MigrationModels;

public class TestModelV2 : BaseModel, IHasMongoId
{
    public DateTime? TestDate { get; set; }
    public string Id { get; set; }
    public string FullName { get; set; }

    public override void EndInit()
    {
        while (SchemaVersion < MongoSchemaVersions.CurrentTestModelSchemaVersion)
        {
            switch (SchemaVersion)
            {
                case 1:
                    CatchAll.Remove("BoolPropertyToRemove");
                    HasVersionBeenUpgraded = true;
                    break;
                default:
                    DefaultUpgradeMigration(MongoSchemaVersions.CurrentTestModelSchemaVersion);
                    break;
            }

            SchemaVersion++;
        }

        while (SchemaVersion > MongoSchemaVersions.CurrentTestModelSchemaVersion)
        {
            switch (SchemaVersion)
            {
                case 3:
                    var success = CatchAll.TryGetValue("FirstName", out BsonValue firstName);
                    if (!success)
                        throw new MongoSchemaDowngradeFailedException(GetType().Name, SchemaVersion, "FirstName does not exist");
                    success = CatchAll.TryGetValue("LastName", out BsonValue lastName);
                    if (!success)
                        throw new MongoSchemaDowngradeFailedException(GetType().Name, SchemaVersion, "lastName does not exist");
                    var fullName = $"{firstName} {lastName}";
                    FullName = fullName;
                    CatchAll.Remove("LastName");
                    CatchAll.Remove("FirstName");
                    CatchAll.Remove("Counter");
                    HasVersionBeenDowngraded = true;
                    break;
                default:
                    DefaultDowngradeMigration(MongoSchemaVersions.CurrentTestModelSchemaVersion);
                    break;
            }

            SchemaVersion--;
        }
    }
}