using FluentAssertions;
using NUnit.Framework;

namespace MigrationExampleTests;

internal static class MongoSchemaVersions
{
    internal static int CurrentTestModelSchemaVersion = 1;
}

[TestFixture]
public class SchemaMigrationTest
{
    private TestRepository _testRepository;

    [SetUp]
    public void SetUp()
    {
        _testRepository = new TestRepository(Configuration.MongoDbConnectionString, Configuration.MongoDatabaseName);
    }

    private TestModelV1 GetTestDataV1()
    {
        var testDataV1 = new TestModelV1
        {
            Id = "testId",
            BoolPropertyToRemove = true,
            TestDate = new DateTime(2022, 01, 01, 10, 30, 30, DateTimeKind.Utc)
        };
        return testDataV1;
    }

    [Test]
    [Order(1)]
    public void If_There_Is_No_Migration_There_Should_Not_Be_An_Upgrade()
    {
        var id = "testId";
        var expected = GetTestDataV1();
        var testDataV1 = GetTestDataV1();
        _testRepository.Upsert(testDataV1);
        var actual = _testRepository.GetFirst<TestModelV1>(id);
        actual.HasVersionBeenUpgraded.Should().Be(false);
        actual.HasVersionBeenDowngraded.Should().Be(false);
        actual.Should().BeEquivalentTo(expected);
    }

    [Test]
    [Order(2)]
    public void Remove_Property_From_V1_Should_Not_Be_In_V2()
    {
        var id = "testId";
        var expected = new TestModelV2
        {
            Id = id,
            TestDate = new DateTime(2022, 01, 01, 10, 30, 30, DateTimeKind.Utc)
        };

        var testDataV1 = GetTestDataV1();
        _testRepository.Upsert(testDataV1);

        // upgrade to v2
        MongoSchemaVersions.CurrentTestModelSchemaVersion = 2;
        var upgradedTestData = _testRepository.GetFirst<TestModelV2>(id);
        upgradedTestData.HasVersionBeenUpgraded.Should().Be(true);
        upgradedTestData.HasVersionBeenDowngraded.Should().Be(false);
        upgradedTestData.SchemaVersion.Should().Be(2);
        _testRepository.Upsert(upgradedTestData);

        // get V2 again to check no further migrations have occurred
        var actual = _testRepository.GetFirst<TestModelV2>(id);
        actual.HasVersionBeenUpgraded.Should().Be(false);
        actual.HasVersionBeenDowngraded.Should().Be(false);
        actual.SchemaVersion.Should().Be(2);
        expected.Should().BeEquivalentTo(expected);
    }

    [Test]
    [Order(3)]
    public void Remove_Property_In_V2_Compose_It_Into_Two_New_Properties_On_V3_Should_Succeed()
    {
        var id = "testId";
        var testDataV2 = new TestModelV2
        {
            Id = id,
            TestDate = new DateTime(2022, 01, 01, 10, 30, 30, DateTimeKind.Utc),
            SchemaVersion = 2,
            FullName = "Donnie Darko"
        };

        _testRepository.Upsert(testDataV2);
        MongoSchemaVersions.CurrentTestModelSchemaVersion = 3;

        var testModelV3 = _testRepository.GetFirst<TestModelV3>(id);
        testModelV3.HasVersionBeenUpgraded.Should().Be(true);
        testModelV3.FirstName.Should().Be("Donnie");
        testModelV3.LastName.Should().Be("Darko");
    }

    [Test]
    [Order(4)]
    // Change a property type from int to string
    // Ideally avoid changing a type in a migration as it requires a custom seraliser to handle the conversion.
    // If it makes sense, add a new property and handle the value to be migrated from the CatchAll property
    public void Change_Type_Of_Property_In_V3_From_Int_To_String_Should_Succeed()
    {
        var id = "testId";
        var testDataV3 = new TestModelV3
        {
            Id = id,
            TestDate = new DateTime(2022, 01, 01, 10, 30, 30, DateTimeKind.Utc),
            SchemaVersion = 3,
            Counter = 123,
            FirstName = "Donnie",
            LastName = "Darko"
        };
        _testRepository.Upsert(testDataV3);
        MongoSchemaVersions.CurrentTestModelSchemaVersion = 4;
        var testModelV4 = _testRepository.GetFirst<TestModelV4>(id);
        testModelV4.HasVersionBeenUpgraded.Should().Be(true);
        testModelV4.Counter.Should().Be("123");
        testModelV4.SchemaVersion.Should().Be(4);
    }

    [Test]
    [Order(5)]
    public void Downgrade_From_V3_To_V2_Should_Succeed()
    {
        var id = "testId";
        var testDataV3 = new TestModelV3
        {
            Id = id,
            TestDate = new DateTime(2022, 01, 01, 10, 30, 30, DateTimeKind.Utc),
            SchemaVersion = 3,
            Counter = 123,
            FirstName = "Donnie",
            LastName = "Darko"
        };
        _testRepository.Upsert(testDataV3);
        MongoSchemaVersions.CurrentTestModelSchemaVersion = 2;
        var testModelV2 = _testRepository.GetFirst<TestModelV2>(id);
        testModelV2.HasVersionBeenDowngraded.Should().Be(true);
        testModelV2.HasVersionBeenUpgraded.Should().Be(false);
        testModelV2.CatchAll.Should().BeEmpty();
        testModelV2.FullName.Should().Be("Donnie Darko");
        testModelV2.SchemaVersion.Should().Be(2);
        _testRepository.Upsert<TestModelV2>(testModelV2);

        //get again and check no migration had occurred
        testModelV2 = _testRepository.GetFirst<TestModelV2>(id);
        testModelV2.HasVersionBeenUpgraded.Should().Be(false);
        testModelV2.HasVersionBeenUpgraded.Should().Be(false);
    }

    [Test]
    [Order(6)]
    public void Migration_From_V4_To_V5_With_No_Migration_Should_Throw_Exception()
    {
        var id = "testId";
        var testDataV4 = new TestModelV4
        {
            Id = id,
            TestDate = new DateTime(2022, 01, 01, 10, 30, 30, DateTimeKind.Utc),
            SchemaVersion = 3,
            Counter = "123",
            FirstName = "Donnie",
            LastName = "Darko"
        };

        _testRepository.Upsert(testDataV4);
        MongoSchemaVersions.CurrentTestModelSchemaVersion = 5;
        Action act = () => _testRepository.GetFirst<TestModelV4>(id);
        act.Should().Throw<MongoSchemaMigrationMissingException>()
            .Where(e => e.Message.Equals("No migration defined for Model:TestModelV4 Version:4"));
    }

    [Test]
    [Order(7)]
    public void Downgrade_From_V2_To_V1_With_No_Migration_Should_Throw_Exception()
    {
        MongoSchemaVersions.CurrentTestModelSchemaVersion = 2;
        var id = "testId";
        var testDataV2 = new TestModelV2
        {
            Id = id,
            TestDate = new DateTime(2022, 01, 01, 10, 30, 30, DateTimeKind.Utc),
            SchemaVersion = 2,
            FullName = "Donnie Darko"
        };

        _testRepository.Upsert(testDataV2);
        
        MongoSchemaVersions.CurrentTestModelSchemaVersion = 1;
        Action act = () => _testRepository.GetFirst<TestModelV1>(id);
        act.Should().Throw<MongoSchemaMigrationMissingException>().Where(e => 
            e.Message.Equals("No migration defined for Model:TestModelV1 Version:2"));
    }


    [TearDown]
    public void TearDown()
    {
        _testRepository.DropDatabase();
    }
}