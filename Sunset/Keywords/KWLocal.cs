using System;
namespace Sunset.Keywords {

    public class KWLocal : Keyword {

        public const String constName="local";

        public KWLocal () : base(constName,KeywordType.MODIFIER) { }

        public override KeywordResult execute(Parser sender,String[]@params) {

            sender.nextExpectedKeywordTypes=new KeywordType[]{KeywordType.MODIFIER,KeywordType.TYPE,KeywordType.FUNCTION };
            sender.currentMods.throwIfhasAccessorModifier(sender);
            sender.currentMods=sender.currentMods|Modifier.LOCAL;
            return base.execute(sender, @params);

        }

    }

}
