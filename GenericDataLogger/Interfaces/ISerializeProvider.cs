using System;
using System.Collections.Generic;
using System.Text;

namespace AYLib.GenericDataLogger
{
    /// <summary>
    /// Enables a class to be used as a serialization provider in the library.
    /// </summary>
    public interface ISerializeProvider
    {
        /// <summary>
        /// Encode ISerializeData into a byte array for writing.
        /// </summary>
        /// <param name="typed">If the data is typed (attribute on properties) or untyped</param>
        /// <param name="encode">If the data is to be encoded or not</param>
        /// <param name="dataType">The data type of the data block</param>
        /// <param name="data">The data to serialize</param>
        /// <returns>Serialized data</returns>
        byte[] Encode(bool typed, bool encode, Type dataType, ISerializeData data);

        /// <summary>
        /// Decode a byte array from a read.
        /// </summary>
        /// <param name="typed">If the data is typed (attribute on properties) or untyped</param>
        /// <param name="encode">If the data is to be encoded or not</param>
        /// <param name="dataType">The data type of the data block</param>
        /// <param name="dataBlock">The byte array to deserialize</param>
        /// <returns>Deserialized object</returns>
        object Decode(bool typed, bool encoded, Type dataType, byte[] dataBlock);

        /// <summary>
        /// Check if a type is able to be serialized by the provider (i.e. check attribute tags).
        /// </summary>
        /// <param name="typeToCheck">Type to check</param>
        /// <returns>True if it can be used, false otherwise</returns>
        bool IsTypeValid(Type typeToCheck);
    }
}
