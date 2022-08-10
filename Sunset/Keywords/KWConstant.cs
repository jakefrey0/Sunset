using System;
namespace Sunset.Keywords {

    public class KWConstant : Keyword {

        public const String constName="constant";

        public KWConstant () : base(constName,KeywordType.MODIFIER) { }

        public override KeywordResult execute(Parser sender,String[]@params) {

            sender.nextExpectedKeywordTypes=new KeywordType[]{KeywordType.MODIFIER,KeywordType.TYPE,KeywordType.FUNCTION };
            sender.currentMods=sender.currentMods|Modifier.CONSTANT;
            sender.throwModErrIfInBlock();
            return base.execute(sender, @params);

        }

    }

}
