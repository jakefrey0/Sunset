using System;
namespace ProgrammingLanguageTutorialIdea {

    [Flags]
    public enum Modifier : uint {
       
        NONE,
        CONSTANT,
        STATIC,

        PUBLIC=0x10,
        PRIVATE=0x100,
        LOCAL=0x1000,
        PULLABLE=0x10000,

    }

    public static class ModifierTools {

        public static Boolean hasAccessorModifier (this Modifier mod) { return mod>=Modifier.PUBLIC; }

        public static void throwIfhasAccessorModifier (this Modifier mod) { 

            if (mod.hasAccessorModifier())
                throw new ParsingError("Can't add multiple accessor modifiers to an instance");

        }

        public static void throwModErrIfInBlock (this Parser psr) { 

            if (psr.blocks.Count!=0)
                throw new ParsingError("Can't place modifiers inside of a block");

        }

    }

}
