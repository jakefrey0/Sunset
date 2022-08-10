using System;
using System.Collections.Generic;
namespace Sunset.Keywords {

    public class KWGoto : Keyword {

        public const String constName="goto";

        public KWGoto () : base (constName,KeywordType.NATIVE_CALL,true) { }

        public override KeywordResult execute (Parser sender,String[]@params) {

            if (@params.Length!=1) throw new ParsingError("Expected 1 parameter (a label) for \""+constName+'"');

            String labelName=@params[0];
            if (!(sender.labelReferences.ContainsKey(labelName)))
                sender.labelReferences.Add(labelName,new List<Tuple<UInt32,UInt32>>());

            KWGoto.leaveAllBlocks(sender);
            sender.labelReferences[labelName].Add(new Tuple<UInt32,UInt32>(sender.getOpcodesCount()+1,sender.memAddress));
            sender.addBytes(new Byte[]{0xE9,0,0,0,0 });

            return base.execute(sender, @params);

        }

        public static void leaveAllBlocks (Parser sender) {

            foreach (Block b in sender.blocks.Keys) {

                sender.addByte(0xC9); //LEAVE
                if (b.breakInstructions!=null)
                    sender.addBytes(b.breakInstructions);

            }

        }

    }

}
