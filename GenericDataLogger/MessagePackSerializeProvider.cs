using MessagePack;
using MessagePack.Resolvers;
using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace AYLib.GenericDataLogger
{
    /// <summary>
    /// The serializer provider for the MessagePack library. This is the default used by the library.
    /// </summary>
    internal class MessagePackSerializeProvider : ISerializeProvider
    {
        private static readonly MessagePackSerializerOptions lz4Options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);
        private static readonly MessagePackSerializerOptions lz4ContractlessOptions = ContractlessStandardResolver.Options.WithCompression(MessagePackCompression.Lz4BlockArray);

        /// <summary>
        /// Encode ISerializeData into a byte array for writing.
        /// </summary>
        /// <param name="typed">If the data is typed (attribute on properties) or untyped</param>
        /// <param name="encode">If the data is to be encoded or not</param>
        /// <param name="dataType">The data type of the data block</param>
        /// <param name="data">The data to serialize</param>
        /// <returns>Serialized data</returns>
        public byte[] Encode(bool typed, bool encode, Type dataType, ISerializeData data)
        {
            return typed ?
                encode ?
                    MessagePackSerializer.Serialize(dataType, data, lz4Options) :
                    MessagePackSerializer.Serialize(dataType, data, MessagePackSerializerOptions.Standard)
                        :
                encode ?
                    MessagePackSerializer.Serialize(dataType, data, lz4ContractlessOptions) :
                    MessagePackSerializer.Serialize(dataType, data, MessagePack.Resolvers.ContractlessStandardResolver.Options);
        }

        /// <summary>
        /// Decode a byte array from a read.
        /// </summary>
        /// <param name="typed">If the data is typed (attribute on properties) or untyped</param>
        /// <param name="encode">If the data is to be encoded or not</param>
        /// <param name="dataType">The data type of the data block</param>
        /// <param name="dataBlock">The byte array to deserialize</param>
        /// <returns>Deserialized object</returns>
        public object Decode(bool typed, bool encoded, Type dataType, byte[] dataBlock)
        {
            return typed ?
                encoded ?
                    MessagePackSerializer.Deserialize(dataType, dataBlock, lz4Options) :
                    MessagePackSerializer.Deserialize(dataType, dataBlock, MessagePackSerializerOptions.Standard)
                        :
                encoded ?
                    MessagePackSerializer.Deserialize(dataType, dataBlock, lz4ContractlessOptions) :
                    MessagePackSerializer.Deserialize(dataType, dataBlock, MessagePack.Resolvers.ContractlessStandardResolver.Options);
        }

        /// <summary>
        /// Check if a type is able to be serialized by the provider, contains either the [MessagePackObject] or [DataContract]
        /// attribute tags.
        /// </summary>
        /// <param name="typeToCheck">Type to check</param>
        /// <returns>True if it can be used, false otherwise</returns>
        public bool IsTypeValid(Type typeToCheck)
        {
            if (typeToCheck.GetCustomAttribute<MessagePackObjectAttribute>(true) == null &&
                typeToCheck.GetCustomAttribute<DataContractAttribute>(true) == null)
                return false;
            return true;
        }
    }
}
