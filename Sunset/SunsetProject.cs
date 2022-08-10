using System;
using System.Text;
using System.Linq;
using System.IO;
using System.Collections.Generic;
namespace Sunset {

    public struct SunsetProject {

        public String name,mainFn,projPath;

    }

    public static class SunsetProjectHelper {

        private const Byte signature=18;

        public static SunsetProject LoadProject (String fn) {

            if (!File.Exists(fn))
                throw new InvalidProjectError("file \""+fn+"\" does not exist");

            Byte[]data=File.ReadAllBytes(fn);

            if (data.First()!=signature)
                ThrowCorrupted(fn);

            SunsetProject sp=new SunsetProject();
            List<Byte>lb=new List<Byte>();
            Byte ctr=0;
            foreach (Byte b in data.Skip(1)) {

                if (b==0) {
                    if (ctr==0) sp.name=Encoding.ASCII.GetString(lb.ToArray());
                    else if (ctr==1) sp.mainFn=Encoding.ASCII.GetString(lb.ToArray());
                    else ThrowCorrupted(fn);
                    lb.Clear();
                    ++ctr;
                }
                else lb.Add(b);

            }

            return sp;

        }

        private static void ThrowCorrupted (String fn) {
            
            throw new InvalidProjectError("file \""+fn+"\" is in invalid format or corrupted");

        }

        public static Byte[] ToBytes (this SunsetProject sp) {

            return new Byte[]{signature }.Concat(Encoding.ASCII.GetBytes(sp.name)).Concat(new Byte[1]).Concat(Encoding.ASCII.GetBytes(sp.mainFn)).Concat(new Byte[1]).ToArray();

        }

    }

    public class InvalidProjectError : Exception {

        public InvalidProjectError (String msg) : base ("Invalid sunset project: "+msg) { }

    }

}
