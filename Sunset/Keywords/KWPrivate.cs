using System;
namespace Sunset.Keywords {

    public class KWPrivate : Keyword {

        public const String constName="private";

        public KWPrivate () : base(constName,KeywordType.MODIFIER) { }

        public override KeywordResult execute(Parser sender,String[]@params) {

            sender.nextExpectedKeywordTypes=new KeywordType[]{KeywordType.MODIFIER,KeywordType.TYPE,KeywordType.FUNCTION };
            sender.currentMods.throwIfhasAccessorModifier(sender);
            sender.throwModErrIfInBlock();
            sender.currentMods=sender.currentMods|Modifier.PRIVATE;
            return base.execute(sender, @params);

        }

    }

}
