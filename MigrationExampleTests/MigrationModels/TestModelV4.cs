using MigrationExampleTests.MigrationScripts.TestModel.V4;
using MigrationExampleTests.Repository;
using MongoDB.Bson.Serialization.Attributes;

namespace MigrationExampleTests.MigrationModels;

public class TestModelV4 : BaseModel, IHasMongoId
{
    public DateTime? TestDate { get; set; }
    public string Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }

    [BsonSerializer(typeof(ChangePropertyCounterFromStringToInt))]
    public string Counter { get; set; }

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
                case 2:
                    var success = CatchAll.TryGetValue("FullName", out var bsonFullName);
                    if (!success)
                        throw new MongoSchemaUpgradeFailedException(GetType().Name, SchemaVersion, "FullName does not exist");
                    var fullName = bsonFullName.AsString;
                    var names = fullName.Split(" ");
                    FirstName = names[0];
                    LastName = names[1];
                    HasVersionBeenUpgraded = true;
                    break;
                case 3:
                    // property Counter was changed from int to string
                    HasVersionBeenUpgraded = true;
                    break;
                default:
                    DefaultUpgradeMigration(MongoSchemaVersions.CurrentTestModelSchemaVersion);
                    break;
            }

            SchemaVersion++;
        }

        DefaultDowngradeMigration(MongoSchemaVersions.CurrentTestModelSchemaVersion);
    }
}