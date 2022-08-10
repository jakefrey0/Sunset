using System;
namespace Sunset.Keywords {

    public class KWAs : Keyword {

        public const String constName="as";

        public KWAs () : base (constName,KeywordType.TYPE_DEFINITION_ASSIGNEMENT) { }

        public override KeywordResult execute(Parser sender,String[]@params) {

            if (String.IsNullOrEmpty(sender.rTypeDefinition))
                throw new ParsingError("Got keyword \""+constName+"\" outside of a type acknowledgement context");

            sender.nextExpectedKeywordTypes=new KeywordType[]{KeywordType.TYPE};
            return base.execute(sender,@params);

        }

    }

}