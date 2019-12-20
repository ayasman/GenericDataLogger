using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace AYLib.GenericDataLogger
{
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

        public void RegisterVersion(uint majorVersion, uint minorVersion, uint revision)
        {
            MajorVersion = majorVersion;
            MinorVersion = minorVersion;
            Revision = revision;
        }

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
