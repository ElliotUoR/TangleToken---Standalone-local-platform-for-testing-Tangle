using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TangleToken.Tanglecode
{
    public class TangleProofObject
    {
        /// <summary>
        /// Address of transaction which confirmed this tranasaction
        /// </summary>
        public String hashingTransaction;
        public String POWHash;
        public DateTime POWTime;

        public TangleProofObject(String hash, String pow)
        {
            this.hashingTransaction = hash;
            this.POWHash = pow;
            POWTime = DateTime.Now;
        }

        public TangleProofObject(String hash, String pow, DateTime time)
        {
            this.hashingTransaction = hash;
            this.POWHash = pow;
            this.POWTime = time;
        }

        public String toText()
        {
            String info = "";
            info += "Confirmed by: " + hashingTransaction + "\n";
            info += "With Hash: " + POWHash + "\n";
            info += "At: " + POWTime.ToString("dd/MM/yyyy HH:mm:ss.ff") + "\n\n";
            return info;
        }
    }
}
