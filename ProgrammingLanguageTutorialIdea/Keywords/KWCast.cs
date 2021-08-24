using System;
namespace ProgrammingLanguageTutorialIdea.Keywords {

    public class KWCast : Keyword {

        public const String constName="cast";

        public KWCast () : base(constName,KeywordType.NATIVE_CALL_WITH_RETURN_VALUE,true) {}

        public override KeywordResult execute(Parser sender,String[] @params) {

            if (@params.Length!=2)
                throw new ParsingError("Expected 2 parameters for \""+constName+"\", instance name and variable type respectively");

            Tuple<String,VarType>firstType=sender.pushValue(@params[0]),secondType=sender.getVarType(@params[1]);

            UInt32 resultB_Size=sender.keywordMgr.getVarTypeByteSize(secondType.Item1);

            if (sender.acknowledgements.ContainsKey(firstType.Item1)) sender.addByte(0x58);//POP EAX
            else if (sender.keywordMgr.getVarTypeByteSize(firstType.Item1)>resultB_Size) {
            
                sender.addByte(0x5A);//POP EDX
                sender.addBytes(new Byte[]{0x33,0xC0 });//XOR EAX,EAX

                if (resultB_Size==1)
                    sender.addBytes(new Byte[]{0x8A,0xC2 });//mov al,dl
                else /*resultB_Size==2*/
                    sender.addBytes(new Byte[]{0x66,0x8B,0xC2 });// mov ax,dx

            }
            else sender.addByte(0x58);//POP EAX

            this.outputType=secondType;

            return base.execute(sender, @params);

        }

    }

}