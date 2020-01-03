using MessagePack;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace AYLib.GenericDataLogger
{
    /// <summary>
    /// The definition of the header block that identifies the version of the output,
    /// and the type registrations of the data being written to the file.
    /// </summary>
    [MessagePackObject]
    public class Header
    {
        private Dictionary<Type, int> registrationIDs = new Dictionary<Type, int>();

        /// <summary>
        /// List of all types registered into the read/write system. The key is the refID for the type, used later on when writing.
        /// </summary>
        [Key(0)]
        public Dictionary<int, TypeRegistration> TypeRegistrations { get; set; } = new Dictionary<int, TypeRegistration>();

        /// <summary>
        /// Major version of the data block.
        /// </summary>
        [Key(1)]
        public uint MajorVersion { get; set; }

        /// <summary>
        /// Minor version of the data block.
        /// </summary>
        [Key(2)]
        public uint MinorVersion { get; set; }

        /// <summary>
        /// Revision of the data block.
        /// </summary>
        [Key(3)]
        public uint Revision { get; set; }

        /// <summary>
        /// Default constructor, required for deserialization through MessagePack.
        /// </summary>
        public Header()
        {

        }

        /// <summary>
        /// Verifies that the given version information matches or is able to be used
        /// with the read file header.
        /// </summary>
        /// <param name="majorVersion">Major version number</param>
        /// <param name="minorVersion">Minor version number</param>
        /// <param name="revision">Revision number</param>
        /// <returns></returns>
        public bool Verify(uint majorVersion, uint minorVersion, uint revision)
        {
            if (MajorVersion == 0 && MinorVersion == 0 && Revision == 0)
                return true;

            if (majorVersion >= MajorVersion &&
                minorVersion >= MinorVersion &&
                revision >= Revision)
                return true;

            return false;
        }

        /// <summary>
        /// Registers a version to the header.
        /// </summary>
        /// <param name="majorVersion">Major version number</param>
        /// <param name="minorVersion">Minor version number</param>
        /// <param name="revision">Revision number</param>
        public void RegisterVersion(uint majorVersion, uint minorVersion, uint revision)
        {
            MajorVersion = majorVersion;
            MinorVersion = minorVersion;
            Revision = revision;
        }

        /// <summary>
        /// Registers a data type as being serialized/deserialized. The block type identifies if the type
        /// is valid for write during a partial write, full write, or both.
        /// </summary>
        /// <param name="newType">The type to register</param>
        /// <param name="outputType">The output type this is valid for (partial or full writes)</param>
        public void RegisterType(Type newType, BlockDataTypes outputType)
        {
            if (newType.GetCustomAttribute<MessagePackObjectAttribute>(true) == null)
                throw new Exception("Object being registered is not marked for serialization.");

            int id = TypeRegistrations.Count;
            TypeRegistrations.Add(id, new TypeRegistration(id, newType, outputType));
            registrationIDs.Add(newType, id);
        }

        /// <summary>
        /// Resets the internal registration ID list with the TypeRegistrations data. This is used
        /// when the TypeRegistrations list has been filled through a read, and the list needs to be set.
        /// </summary>
        public void ResetRegistrationIDs()
        {
            registrationIDs.Clear();

            try
            {
                if (TypeRegistrations != null)
                {
                    foreach (var reg in TypeRegistrations)
                    {
                        registrationIDs.Add(reg.Value.ClassType, reg.Key);
                    }
                }
            }
            catch (Exception ex)
            {
                registrationIDs.Clear();
                throw ex;
            }
        }

        /// <summary>
        /// Gets the registration ID created for the given type.
        /// </summary>
        /// <param name="findType">The type to find</param>
        /// <returns>The 0 based ID, or -2 if not found</returns>
        public int GetRegistrationID(Type findType)
        {
            if (!registrationIDs.ContainsKey(findType))
                return -2;
            return registrationIDs[findType];
        }

        /// <summary>
        /// Gets the output types for the given type.
        /// </summary>
        /// <param name="findType">The type to find</param>
        /// <returns>The output types the given type is valid for</returns>
        public BlockDataTypes GetRegistrationOutput(Type findType)
        {
            if (!registrationIDs.ContainsKey(findType))
                return BlockDataTypes.None;

            return TypeRegistrations[registrationIDs[findType]].OutputType;
        }

        /// <summary>
        /// Get the type for the given registration ID.
        /// </summary>
        /// <param name="findID">The registration ID to find</param>
        /// <returns>The type data</returns>
        public Type GetRegistrationType(int findID)
        {
            if (!TypeRegistrations.ContainsKey(findID))
                return null;
            return TypeRegistrations[findID].ClassType;
        }
    }
}
