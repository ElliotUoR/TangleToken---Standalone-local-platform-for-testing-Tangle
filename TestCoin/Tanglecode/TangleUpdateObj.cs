using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TangleToken.Tanglecode
{
    public class TangleUpdateObj
    {
        public List<TangleTransaction> trans;
        public List<String> hashes;
        Tangle tangle;

        public TangleUpdateObj(Tangle t)
        {
            trans = new List<TangleTransaction>();
            hashes = new List<string>();
            tangle = t;
        }

        public bool isInHash(String hash)
        {
            return hashes.Contains(hash);
        }

        public bool isInTrans(String hash)
        {
            foreach (TangleTransaction t in trans)
            {
                if (t.hash.Equals(hash))
                {
                    return true;
                }
            }
            return false;
        }

        public bool CheckFinished()
        {
            bool check = true;
            foreach (String s in hashes)
            {
                if (!isInTrans(s))
                {
                    return false;
                }
            }
            return check;
        }

        public void Wipe()
        {
            trans = new List<TangleTransaction>();
            hashes = new List<String>();
        }


    }
}
