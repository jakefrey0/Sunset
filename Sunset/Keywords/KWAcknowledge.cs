using System;
namespace Sunset.Keywords {

    public class KWAcknowledge : Keyword {

        public const String constName="acknowledge";

        public KWAcknowledge () : base(constName,KeywordType.TYPE_DEFINITION) { }

        public override KeywordResult execute (Parser sender,String[] @params) {

            Keyword.throwIfShouldBeHeader(sender,constName);

            return new KeywordResult() { newStatus=ParsingStatus.SEARCHING_TYPE_DEFINITION_NAME,newOpcodes=new Byte[0] };

        }

    }

}
