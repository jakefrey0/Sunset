using System;
namespace ProgrammingLanguageTutorialIdea.Keywords {

    public class KWPullable : Keyword {

        public const String constName="pullable";

        public KWPullable () : base(constName,KeywordType.MODIFIER) { }

        public override KeywordResult execute(Parser sender,String[]@params) {

            sender.nextExpectedKeywordTypes=new KeywordType[]{KeywordType.MODIFIER,KeywordType.TYPE,KeywordType.FUNCTION };
            sender.currentMods.throwIfhasAccessorModifier();
            sender.throwModErrIfInBlock();
            sender.currentMods=sender.currentMods|Modifier.PULLABLE;
            return base.execute(sender, @params);

        }

    }

}
