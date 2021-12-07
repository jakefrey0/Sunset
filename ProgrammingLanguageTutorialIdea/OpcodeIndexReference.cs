using System;
namespace ProgrammingLanguageTutorialIdea {

    public class OpcodeIndexReference {

        public UInt32 index;
        public OpcodeIndexType type;

        private OpcodeIndexReference (UInt32 index,OpcodeIndexType type) {

            this.index=index;
            this.type=type;

        }

        public Int32 GetIndexAsInt () { return (Int32)index; }

        public static OpcodeIndexReference NewCodeSectRef (UInt32 index) { return new OpcodeIndexReference(index,OpcodeIndexType.CODE_SECT_REFERENCE); }
        public static OpcodeIndexReference NewDataSectRef (UInt32 index) { return new OpcodeIndexReference(index,OpcodeIndexType.DATA_SECT_REFERENCE); }

    }


}