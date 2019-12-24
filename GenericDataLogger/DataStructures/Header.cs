using MessagePack;
using System;
using System.Collections.Generic;
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

        [Key(0)]
        public Dictionary<int, TypeRegistration> TypeRegistrations { get; set; } = new Dictionary<int, TypeRegistration>();

        [Key(1)]
        public uint MajorVersion { get; set; }

        [Key(2)]
        public uint MinorVersion { get; set; }

        [Key(3)]
        public uint Revision { get; set; }

        public Header()
        {

        }

        /// <summary>
        /// Verifies that the given version information matches or is able to be used
        /// with the read file header.
        /// </summary>
        /// <param name="major"></param>
        /// <param name="minor"></param>
        /// <param name="revision"></param>
        /// <returns></returns>
        public bool Verify(uint major, uint minor, uint revision)
        {
            if (MajorVersion == 0 && MinorVersion == 0 && Revision == 0)
                return true;

            if (major >= MajorVersion &&
                minor >= MinorVersion &&
                revision >= Revision)
                return true;

            return false;
        }

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
        /// <param name="newType"></param>
        /// <param name="outputType"></param>
        public void RegisterType(Type newType, BlockDataTypes outputType)
        {
            int id = TypeRegistrations.Count;
            TypeRegistrations.Add(id, new TypeRegistration(id, newType, outputType));
            registrationIDs.Add(newType, id);
        }

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

        public int GetRegistrationID(Type findType)
        {
            if (!registrationIDs.ContainsKey(findType))
                return -2;
            return registrationIDs[findType];
        }

        public BlockDataTypes GetRegistrationOutput(Type findType)
        {
            if (!registrationIDs.ContainsKey(findType))
                return BlockDataTypes.None;

            return TypeRegistrations[registrationIDs[findType]].OutputType;
        }

        public Type GetRegistrationType(int findID)
        {
            if (!TypeRegistrations.ContainsKey(findID))
                return null;
            return TypeRegistrations[findID].ClassType;
        }
    }
}
