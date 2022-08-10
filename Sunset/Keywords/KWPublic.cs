using System;
namespace Sunset.Keywords {

    public class KWPublic : Keyword {

        public const String constName="public";

        public KWPublic () : base(constName,KeywordType.MODIFIER) { }

        public override KeywordResult execute(Parser sender,String[]@params) {

            sender.nextExpectedKeywordTypes=new KeywordType[]{KeywordType.MODIFIER,KeywordType.TYPE,KeywordType.FUNCTION };
            sender.currentMods.throwIfhasAccessorModifier();
            sender.throwModErrIfInBlock();
            sender.currentMods=sender.currentMods|Modifier.PUBLIC;
            return base.execute(sender, @params);

        }

    }

}
