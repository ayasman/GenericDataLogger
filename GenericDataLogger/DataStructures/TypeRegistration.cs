using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace AYLib.GenericDataLogger
{
    /// <summary>
    /// Definition of the type registration information, for serialization to file.
    /// </summary>
    [MessagePackObject]
    public class TypeRegistration
    {
        private Type linkedType;

        /// <summary>
        /// Reference ID, assigned by the system when a type is registered.
        /// </summary>
        [Key(0)]
        public int RefID { get; set; }

        /// <summary>
        /// The long name of the type, for lookup later (deserialization).
        /// </summary>
        [Key(1)]
        public string LongName { get; set; }

        /// <summary>
        /// Which output this type is valid for (partial or full outputs)
        /// </summary>
        [Key(2)]
        public BlockDataTypes OutputType { get; set; }

        /// <summary>
        /// The actual class type object, using the long name if the type isn't assigned through constructor yet.
        /// </summary>
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

        /// <summary>
        /// Default constructor, required for deserialization through MessagePack.
        /// </summary>
        public TypeRegistration()
        {

        }

        /// <summary>
        /// Registers a type to be available to the read/write system.
        /// </summary>
        /// <param name="refID">Unique ID of the type, to be refered to by both read and write</param>
        /// <param name="newType">The type that is being registered</param>
        /// <param name="outputType">The outputs that this type may be serialized with</param>
        public TypeRegistration(int refID, Type newType, BlockDataTypes outputType)
        {
            linkedType = newType;

            RefID = refID;
            LongName = newType.AssemblyQualifiedName;
            OutputType = outputType;
        }
    }
}
