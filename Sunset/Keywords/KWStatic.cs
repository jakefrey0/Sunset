using System;
namespace Sunset.Keywords {

    public class KWStatic : Keyword {

        public const String constName="static";

        public KWStatic () : base(constName,KeywordType.MODIFIER) { }

        public override KeywordResult execute(Parser sender,String[]@params) {

            sender.nextExpectedKeywordTypes=new KeywordType[]{KeywordType.MODIFIER,KeywordType.TYPE,KeywordType.FUNCTION };
            sender.throwModErrIfInBlock();
            sender.currentMods=sender.currentMods|Modifier.STATIC;
            return base.execute(sender, @params);

        }

    }

}
