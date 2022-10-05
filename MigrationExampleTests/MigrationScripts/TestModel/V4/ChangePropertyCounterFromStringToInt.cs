using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace MigrationExampleTests.MigrationScripts.TestModel.V4;

public class ChangePropertyCounterFromStringToInt : IBsonSerializer
{
    public Type ValueType { get; } = typeof(string);

    public object Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        if (context.Reader.CurrentBsonType == BsonType.Int32)
            return GetNumberValue(context);
        return context.Reader.ReadString();
    }

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
    {
        context.Writer.WriteString(value as string);
    }

    private static object GetNumberValue(BsonDeserializationContext context)
    {
        var value = context.Reader.ReadInt32();
        return value.ToString();
    }
}