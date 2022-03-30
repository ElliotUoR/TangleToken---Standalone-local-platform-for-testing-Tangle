using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TangleToken.Tanglecode
{
    class TangleValidationToken
    {
        public TangleTransaction transaction;
        public TangleTransaction parent1;
        public TangleTransaction parent2;

        public static List<TangleTransaction> wholeList;

        public static List<TangleTransaction> validatedTrans;

        public TangleValidationToken(TangleTransaction validater, TangleTransaction parent1, TangleTransaction parent2)
        {
            this.transaction = validater;
            this.parent1 = parent1;
            this.parent2 = parent2;
        }

        public static void SetList(List<TangleTransaction> list)
        {
            wholeList = list;
            validatedTrans = new List<TangleTransaction>();
        }

        public static void ClearList()
        {
            wholeList = null;
            validatedTrans = null;
        }

        public static bool GenesisValidation(TangleTransaction tran)
        {
            TangleTransaction temp;
            TangleTransaction parentTemp1;
            TangleTransaction parentTemp2;
            foreach(TangleProofObject tpo in tran.confirms)
            {
                temp = Tangle.findHashInList(wholeList, tpo.hashingTransaction);
                if (temp.Equals(null))
                {
                    return false;
                }
                if (temp.confirmAddress1.Equals(tran.hash))
                {
                    parentTemp1 = tran;
                    parentTemp2 = Tangle.findHashInList(wholeList, temp.confirmAddress2);
                    if (parentTemp2.Equals(null))
                    {
                        return false;
                    }
                }
                else if (temp.confirmAddress2.Equals(tran.hash))
                {
                    parentTemp2 = tran;
                    parentTemp1 = Tangle.findHashInList(wholeList, temp.confirmAddress1);
                    if (parentTemp1.Equals(null))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
                TangleValidationToken tvk = new TangleValidationToken(temp, parentTemp1, parentTemp2);
                if (!tvk.ValidateTokenRecursion())
                {
                    return false;
                }
            }

            validatedTrans.Add(tran);
            return true;
        }

        public bool ValidateTokenRecursion()
        {

            TangleTransaction temp;
            TangleTransaction parentTemp1;
            TangleTransaction parentTemp2;
            foreach (TangleTransaction t in validatedTrans)
            {
                if (t.hash.Equals(transaction.hash))
                {
                    return true;
                }
            }

            if (!TangleTransaction.validateTran(transaction, parent1, parent2))
            {
                return false;
            }

            foreach (TangleProofObject tpo in transaction.confirms)
            {
                temp = Tangle.findHashInList(wholeList, tpo.hashingTransaction);
                if (temp == null)
                {
                    return false;
                }
                if (temp.confirmAddress1.Equals(transaction.hash))
                {
                    parentTemp1 = transaction;
                    parentTemp2 = Tangle.findHashInList(wholeList, temp.confirmAddress2);
                    if (parentTemp2.Equals(null))
                    {
                        return false;
                    }
                }
                else if (temp.confirmAddress2.Equals(transaction.hash))
                {
                    parentTemp2 = transaction;
                    parentTemp1 = Tangle.findHashInList(wholeList, temp.confirmAddress1);
                    if (parentTemp1.Equals(null))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
                TangleValidationToken tvk = new TangleValidationToken(temp, parentTemp1, parentTemp2);
                if (!tvk.ValidateTokenRecursion())
                {
                    return false;
                }
            }


            validatedTrans.Add(transaction);
            return true;
        }
    }
}
