using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace AYLib.GenericDataLogger
{
    /// <summary>
    /// Definition of the type registraion information, for serialization to file.
    /// </summary>
    [MessagePackObject]
    public class TypeRegistration
    {
        private Type linkedType;

        [Key(0)]
        public int RefID { get; set; }

        [Key(1)]
        public string LongName { get; set; }

        [Key(2)]
        public BlockDataTypes OutputType { get; set; }

        [IgnoreMember]
        public Type ClassType
        {
            get
            {
                if (linkedType == null)
                    linkedType = Type.GetType(LongName);
                return linkedType;
            }
        }

        public TypeRegistration()
        {

        }

        public TypeRegistration(int refID, Type newType, BlockDataTypes outputType)
        {
            linkedType = newType;

            RefID = refID;
            LongName = newType.AssemblyQualifiedName;
            OutputType = outputType;
        }
    }
}
