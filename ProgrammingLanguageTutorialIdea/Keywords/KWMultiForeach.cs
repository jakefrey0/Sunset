using System;
using ProgrammingLanguageTutorialIdea.Stack;
using System.Linq;
using System.Collections.Generic;
namespace ProgrammingLanguageTutorialIdea.Keywords {

    public class KWMultiForeach : Keyword {

        public const String constName="mforeach";

        public KWMultiForeach () : base (constName,KeywordType.NATIVE_CALL,true) { }

        public override KeywordResult execute(Parser sender,String[]@params) {

            if (@params.Length%2!=0)
                throw new ParsingError("Expected a multiple of 2 parameters for \""+constName+"\". Format: \"item,array,item0,array0, ...\", each 2 items are a pair");

            IEnumerable<Tuple<String,String>>pairs=KWMultiForeach.pairCollection(@params);
            Tuple<String,String>firstPair=pairs.First();
            sender.pushValue(firstPair.Item2);
            sender.addByte(0x58);//POP EAX
            sender.addBytes(new Byte[]{0x8B,8 });//MOV ECX,[EAX]
            foreach (Tuple<String,String>pair in pairs.Skip(1)) {

                sender.pushValue(firstPair.Item2);

                sender.addByte(0x58);//POP EAX
                sender.addBytes(new Byte[]{0x39,8 }); //CMP [EAX],ECX
                sender.addBytes(new Byte[]{0x7D,2 }); //JGE 2 BYTES (JGE OVER NEXT INSTRUCTION)
                sender.addBytes(new Byte[]{0x8B,8 });//MOV ECX,[EAX]
                
            }

            sender.addByte(0x51);//PUSH ECX
            sender.pseudoStack.push(new PreservedECX());
            sender.addBytes(new Byte[]{0x31,0xC9}); //XOR ECX,ECX
            UInt32 cMemAddr=sender.memAddress;
            Block foreachBlock=new Block(delegate {sender.writeJump(cMemAddr);sender.pseudoStack.pop(2);},0,new Byte[]{0x59,0x83,0xC4,(Byte)(@params.Length*2)}/*POP ECX,ADD ESP,(@params.Length/2)*4*/,false,false){isLoopOrSwitchBlock=true,continueAddress=cMemAddr};
            foreachBlock.continueInstructions=foreachBlock.breakInstructions=foreachBlock.opcodesToAddOnBlockEnd;
            foreachBlock.afterBlockClosedOpcodes=new Byte[]{0x83,0xC4,4 }; // ADD ESP,4 to remove the preserved ecx

            sender.addBytes(new Byte[]{0x3B,0x0C,0x24 }); //CMP ECX,[ESP]
            foreachBlock.blockMemPositions.Add(sender.getOpcodesCount()+2);
            sender.addBytes(new Byte[]{0x0F,0x84,0,0,0,0});//JZ
            foreachBlock.startMemAddr=sender.memAddress;

            foreach (Tuple<String,String>pair in pairs) {
                String arrName=pair.Item2,varName=pair.Item1;
                if (sender.nameExists(varName))
                    throw new ParsingError("Foreach iteration variable name \""+varName+"\" is already in use");
                if (varName.Any(x=>!Char.IsLetterOrDigit(x)))
                    throw new ParsingError("Invalid foreach iteration variable name \""+varName+"\" (should be alphanumeric)");
                // EBX can be preserved throughout the whole loop so it doesn't have to constantly run pushValue instructions
                // (but it doesn't happen here, it is an optimization idea)
                Tuple<String,VarType>vt=sender.pushValue(arrName);
                sender.addByte(0x5B);
                if (vt.Item2!=VarType.NATIVE_ARRAY) 
                    throw new ParsingError("Expected var of type array, got \""+arrName+"\" of type \""+vt.Item1+"\" (\""+vt.Item2.ToString()+"\")");
                sender.addBytes(new Byte[]{0x8B,0x43,4}); //MOV EAX,[EBX+4]
                sender.addBytes(new Byte[]{0xF7,0xE1}); //MUL ECX
                sender.addBytes(new Byte[]{1,0xD8}); //ADD EAX,EBX
                switch (sender.keywordMgr.getVarTypeByteSize(vt.Item1)) {
                    
                    // case 1 & 2 use a bit of a "trick" here because on the MUL ECX instruction
                    // EDX will in most cases be set to zero (though not always) so it should be
                    // a temporary "trick" and probably changed later (though I haven't looked into it)
                    
                    case 1:
                        sender.addBytes(new Byte[]{0x8A,0x50,8});//MOV DL,[EAX+8]
                        sender.addByte(0x52);//PUSH EDX
                        break;
                    case 2:
                        sender.addBytes(new Byte[]{0x66,0x8B,0x50,8});//MOV DX,[EAX+8]
                        sender.addByte(0x52);//PUSH EDX
                        break;
                    default:
                    case 4:
                        sender.addBytes(new Byte[]{0xFF,0x70,8}); //PUSH DWORD [EAX+8]
                        break;
                        
                }
                sender.pseudoStack.push(new LocalVar(varName));
                foreachBlock.localVariables.Add(varName,new Tuple<Tuple<String,VarType>>(sender.getVarType(vt.Item1)));
            }
            
            sender.addBlock(foreachBlock);
            sender.addByte(0x41);//INC ECX
            sender.addByte(0x51);//PUSH ECX
            sender.pseudoStack.push(new PreservedECX());
            sender.addBytes(sender.getEnterBlockOpcodes(foreachBlock));
            return base.execute(sender,@params);

        }

        /// <summary>
        /// Pairs the collection.
        /// </summary>
        /// <returns>The pairs: Item 1 - Var Name, Item 2 - Array Name</returns>
        /// <param name="col">The collection.</param>
        public static IEnumerable<Tuple<String,String>> pairCollection (IEnumerable<String>col) {
            
            Tuple<String,String>ctpl=new Tuple<String,String>(null,null);
            foreach (String s in col) {

                if (ctpl.Item1==null)
                    ctpl=new Tuple<String,String>(s,null);
                else {
                    ctpl=new Tuple<String,String>(ctpl.Item1,s);
                    yield return ctpl;
                    ctpl=new Tuple<String,String>(null,null);
                }

            }

        }

    }

}