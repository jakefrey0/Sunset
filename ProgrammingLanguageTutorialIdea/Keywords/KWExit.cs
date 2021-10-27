using System;
using System.Linq;
namespace ProgrammingLanguageTutorialIdea.Keywords {

    public class KWExit : Keyword {

        public const String constName="exit";

        public KWExit() : base (constName,KeywordType.NATIVE_CALL,true) { }

        public override KeywordResult execute (Parser sender,String[]@params) {

            Console.WriteLine(sender.pushValue(@params[0]).Item1+","+sender.pushValue(@params[0]).Item2.ToString());
            Console.ReadKey();
            return base.execute(sender,@params);

        }

    }

}
