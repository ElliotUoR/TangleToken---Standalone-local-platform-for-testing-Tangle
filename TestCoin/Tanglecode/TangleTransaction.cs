using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestCoin.Wallet;
using TestCoin.Common;

namespace TangleToken.Tanglecode
{
    public class TangleTransaction
    {
        public static string path;

        public DateTime preTimestamp; //time at generation of signature 
        public DateTime POWTimestamp1;
        public DateTime POWTimestamp2;
        public String signature; //signed version of the hash

        public int portGeneratedOn = -1; //used for testing only

        public String powConfirmHash1 = String.Empty; //POW of previous transaction (includes its 2 POWs, its hash + this signature + this weight + nonce stored in this transaciton)
        public String confirmAddress1;
        public String powConfirmHash2 = String.Empty;
        public String confirmAddress2;

        public String hash; //simple hash of data in transaction (before powHash has been generated)
        public double nonce1; //no need for extra nonce as POW is expected to be small
        public double nonce2;

        public List<TangleProofObject> confirms = new List<TangleProofObject>();

        public double amount;
        public String toAdd;
        public String fromAdd;

        public double height; //longest path of transactions 

        double weight; //relavent to POW
        double totalWeight;
        //pick transaction using random walk monte carlo


        bool miningDone1;
        bool miningDone2;

        public Func<TangleTransaction, TangleTransaction, TangleTransaction, bool> TranCallback;

        public int depth = 1; //default of 1
        public bool unconnected = false;


        TangleTransaction temp1;

        TangleTransaction temp2;

        /// <summary>
        /// For generating a transaction
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="toAdd"></param>
        /// <param name="fromAdd"></param>
        public TangleTransaction(double amount, String toAdd, String fromAdd, String privateKey, TangleTransaction tran1, TangleTransaction tran2, Func<TangleTransaction, TangleTransaction, TangleTransaction, bool> TranCallback, int port = -1)
        {
            this.amount = amount;
            this.toAdd = toAdd;
            this.fromAdd = fromAdd;
            this.TranCallback = TranCallback;
            this.portGeneratedOn = port;

            this.preTimestamp = DateTime.Now;

            if (tran1.height >= tran2.height)
            {
                this.height = tran1.height + 1;
            }
            else
            {
                this.height = tran2.height + 1;
            }

            this.weight = CalcWeight();

            this.hash = TangleTransaction.createHash(this);
            this.signature = Wallet.CreateSignature(fromAdd, privateKey, hash);

            this.temp1 = tran1;
            this.temp2 = tran2;

            POWTransactions(tran1, tran2);

            while(!miningDone1 && !miningDone2)
            {
                Thread.Sleep(5);
            }

            //do hash
            //make sig
            //mine powhash
            //also assign nonce
            //get powtimestamp

            //throw on the tangle chain

        }

        /// <summary>
        /// For genesis Transactions only
        /// </summary>
        /// <param name="toAdd"></param>
        /// <param name="fromAdd"></param>
        /// <param name="privateKey"></param>
        public TangleTransaction(String toAdd, String fromAdd, String privateKey, double TokenCap)
        {
            this.toAdd = toAdd;
            this.fromAdd = fromAdd;

            this.preTimestamp = DateTime.Now;

            this.weight = CalcWeight();

            this.height = 1;

            this.hash = TangleTransaction.createHash(this);
            this.signature = Wallet.CreateSignature(fromAdd, privateKey, hash);

            amount = TokenCap;

            //do hash
            //make sig
            //mine powhash
            //also assign nonce
            //get powtimestamp

            //throw on the tangle chain

        }

        public TangleTransaction(String hash, String sig, DateTime pretime, Double amount, String toAdd, String fromAdd, int height, int weight, String confirmHash1="None", String confirmAddress1="None", double Nonce1 = 0,
            String confirmHash2="None", String confirmAddress2="None", double Nonce2=0, DateTime? POWTime1 = null, DateTime? POWTime2 = null, List<TangleProofObject> tempTpos = null)
        {
            this.hash = hash;
            this.signature = sig;
            this.preTimestamp = pretime;
            this.amount = amount;
            this.toAdd = toAdd;
            this.fromAdd = fromAdd;
            this.height = height;
            this.weight = weight;
            this.powConfirmHash1 = confirmHash1;
            this.confirmAddress1 = confirmAddress1;
            this.powConfirmHash2 = confirmHash2;
            this.confirmAddress2 = confirmAddress2;
            this.nonce1 = Nonce1;
            this.nonce2 = Nonce2;
            if (POWTime1 == null)
            {
                this.POWTimestamp1 = DateTime.MaxValue;
            }
            else
            {
                this.POWTimestamp1 = (DateTime)POWTime1;
            }
            if (POWTime2 == null)
            {
                this.POWTimestamp2 = DateTime.MaxValue;
            }
            else
            {
                this.POWTimestamp2 = (DateTime)POWTime2;
            }
            if (tempTpos == null)
            {
                this.confirms = new List<TangleProofObject>();
            }
            else
            {
                this.confirms = tempTpos;
            }
        }

        public void SetTPO(List<TangleProofObject> list = null, TangleProofObject tpo = null)
        {
            if (list != null)
            {
                confirms = list;
            }
            if (tpo != null) {
                confirms.Add(tpo);
            }

        }



        public static String createHash(TangleTransaction trans)
        {
            SHA256 hashSys = SHA256Managed.Create();
            Byte[] hashByte = hashSys.ComputeHash(Encoding.UTF8.GetBytes(trans.fromAdd + trans.toAdd + trans.amount + trans.preTimestamp.ToString("dd/MM/yyyy HH:mm:ss.ff")));//+ trans.timestamp.ToString()
            String temphash = string.Empty;
            foreach (byte x in hashByte)
            {
                temphash += String.Format("{0:x2}", x);
            }
            return temphash;
        }

        public void POWTransactions(TangleTransaction prevTran1, TangleTransaction prevTran2)
        {
            miningDone1 = false;
            miningDone2 = false;
            Thread _pow1 = new Thread(() => POW(prevTran1, true));
            _pow1.Start();
            Thread _pow2 = new Thread(() => POW(prevTran2, false));
            _pow2.Start();
        }


        public void POW(TangleTransaction tran, bool firstConfirm)
        {
            String POWhash = "";

            int hashattempts = 1;

            int localNonce = 0;

            double difficulty = CalcDifficulty();

            POWhash = CalcHash(tran, localNonce);
            while (POWhash.Substring(0, (int)difficulty) != String.Join("", Enumerable.Repeat('0', (int)Math.Floor(difficulty)).ToArray())) {
                hashattempts++;
                localNonce++;
                POWhash = CalcHash(tran, localNonce);

            }


            if (firstConfirm)
            {
                miningDone1 = true;
                confirmAddress1 = tran.hash;
                powConfirmHash1 = POWhash;
                nonce1 = localNonce;
                POWTimestamp1 = DateTime.Now;

                TangleProofObject tpo = new TangleProofObject(hash, POWhash);

                temp1.confirms.Add(tpo);

            }
            else
            {
                miningDone2 = true;
                confirmAddress2 = tran.hash;
                powConfirmHash2 = POWhash;
                nonce2 = localNonce;
                POWTimestamp2 = DateTime.Now;

                TangleProofObject tpo = new TangleProofObject(hash, POWhash);

                temp2.confirms.Add(tpo);
            }

            while ((!miningDone1 || !miningDone2) && firstConfirm)
            {
                Thread.Sleep(2);
            }
            if (miningDone1 && miningDone2 && firstConfirm)
            {
                TranCallback(this, temp1, temp2);
            }

            return;
        }

        public String CalcHash(TangleTransaction tran, int Nonce)
        {
            SHA256 hashSys;
            hashSys = SHA256Managed.Create();
            Byte[] hashByte = hashSys.ComputeHash(Encoding.UTF8.GetBytes((tran.powConfirmHash1 + tran.powConfirmHash2 + tran.signature + signature + Nonce + weight)));
            String temphash = string.Empty;
            foreach (byte x in hashByte)
            {
                temphash += String.Format("{0:x2}", x);
            }
            return temphash;
        }

        public static String CalcHash(String pow1, String pow2, String oldSig, String sig, double nonce, double weight)
        {
            SHA256 hashSys;
            hashSys = SHA256Managed.Create();
            Byte[] hashByte = hashSys.ComputeHash(Encoding.UTF8.GetBytes((pow1 + pow2 + oldSig + sig + nonce + weight)));
            String temphash = string.Empty;
            foreach (byte x in hashByte)
            {
                temphash += String.Format("{0:x2}", x);
            }
            return temphash;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            try
            {
                TangleTransaction t = (TangleTransaction)obj;
                return t.hash.Equals(hash);
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public int CalcWeight()
        {
            return 1;
        }

        public int CalcDifficulty()
        {
            if (weight == 1)
            {
                return 2;
            }
            if (weight <= 3)
            {
                return 3;
            }
            if (weight > 3)
            {
                return 4;
            }

            return 2;
        }

        public String getInfo()
        {
            String info = "Transaction Hash: " + hash + "\n";
            info += "Signature: " + signature + "\n";
            info += "Creation Timestamp: " + preTimestamp.ToString("dd/MM/yyyy HH:mm:ss.ff") + "\n";
            info += "Amount: " + amount + "\n";
            info += "To Address: " + toAdd + "\n";
            info += "From Address: " + fromAdd + "\n";
            info += "Height: " + height + "\n";
            info += "Weight: " + weight + "\n\n";
            if (!powConfirmHash1.Equals(String.Empty)) {
                info += "Confirming Address 1: " + confirmAddress1 + "\n";
                info += "Confirming Hash 1: " + powConfirmHash1 + "\n";
                info += "Nonce 1: " + nonce1 + "\n";
                info += "Time 1: " + POWTimestamp1.ToString("dd/MM/yyyy HH:mm:ss.ff") + "\n\n";
            }
            if (!powConfirmHash2.Equals(String.Empty))
            {
                info += "Confirming Address 2: " + confirmAddress2 + "\n";
                info += "Confirming Hash 2: " + powConfirmHash2 + "\n";
                info += "Nonce 2: " + nonce2 + "\n";
                info += "Time 2: " + POWTimestamp2.ToString("dd/MM/yyyy HH:mm:ss.ff") + "\n\n";
            }
            info += ConfirmsDetails();
            info += "\n--------------------------------------------------------------------------------------------------------------------------------\n";

            return info;
        }

        public String ConfirmsDetails()
        {
            String info = "Confirms: " + "\n";
            if (confirms.Count < 1)
            {
                return info;
            }
            else
            {
                foreach (TangleProofObject tpo in confirms)
                {
                    info += "Confirmed by: " + tpo.hashingTransaction + "\n";
                    info += "With Hash: " + tpo.POWHash + "\n";
                    info += "At: " + tpo.POWTime.ToString("dd/MM/yyyy HH:mm:ss.ff") + "\n\n";
                }
            }

            return info;
        }



        public static TangleTransaction ParseTranText(String text)
        {
            if (text.Equals(String.Empty))
            {
                return null;
            }
            //read
            String hash = "";
            String sig = "";
            DateTime? preTime = null;
            Double amount = 0;
            String toAdd = "";
            String fromAdd = "";
            int height = 0;
            int weight = 0;
            String confirmHash1 = "";
            String confirmAddress1 = "";
            double Nonce1 = 0;
            String confirmHash2 = "";
            String confirmAddress2 = "";
            double Nonce2 = 0;
            DateTime? POWTime1 = null;
            DateTime? POWTime2 = null;

            bool confirms = true;
            String confirmText = "";
            String[] splitText = Common.splitAt(text, "Confirms: ");
            String tranText = splitText[0];

            bool pow1 = false;
            bool pow2 = false;

            List<TangleProofObject> tempTpos = new List<TangleProofObject>();

            try
            {
                confirmText = splitText[1];
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
                confirms = false;

            }

            String[] lineList = Common.splitAt(tranText, "newline");
            String tempS;
            String[] temparr;

            foreach (String line in lineList)
            {
                if (line.Contains("Creation Timestamp:"))
                {
                    temparr = Common.splitAt(line, "Timestamp: ");
                    preTime = DateTime.ParseExact(temparr[1], "dd/MM/yyyy HH:mm:ss.ff", null);
                }
                else if (line.Contains("Transaction Hash:"))
                {
                    tempS = Common.splitAt(line, "Hash: ")[1];
                    tempS = Common.RemoveWhitespace(tempS);
                    hash = tempS;
                }
                else if (line.Contains("Signature: "))
                {
                    tempS = Common.splitAt(line, "Signature: ")[1];
                    tempS = Common.RemoveWhitespace(tempS);
                    sig = tempS;
                }
                else if (line.Contains("Amount:"))
                {
                    tempS = Common.splitAt(line, "Amount: ")[1];
                    tempS = Common.RemoveWhitespace(tempS);
                    amount = Double.Parse(tempS);
                }
                else if (line.Contains("To Address:"))
                {
                    tempS = Common.splitAt(line, "To Address: ")[1];
                    tempS = Common.RemoveWhitespace(tempS);
                    toAdd = tempS;
                }
                else if (line.Contains("From Address:"))
                {
                    tempS = Common.splitAt(line, "From Address: ")[1];
                    tempS = Common.RemoveWhitespace(tempS);
                    fromAdd = tempS;
                }
                else if (line.Contains("Height:"))
                {
                    tempS = Common.splitAt(line, "Height: ")[1];
                    tempS = Common.RemoveWhitespace(tempS);
                    height = Int32.Parse(tempS);
                }
                else if (line.Contains("Weight:"))
                {
                    tempS = Common.splitAt(line, "Weight: ")[1];
                    tempS = Common.RemoveWhitespace(tempS);
                    weight = Int32.Parse(tempS);
                }
                else if (line.Contains("Confirming Address 1: "))
                {
                    pow1 = true;
                    tempS = Common.splitAt(line, "Address 1: ")[1];
                    tempS = Common.RemoveWhitespace(tempS);
                    confirmAddress1 = tempS;
                }
                else if (line.Contains("Confirming Hash 1:"))
                {
                    tempS = Common.splitAt(line, "Hash 1: ")[1];
                    tempS = Common.RemoveWhitespace(tempS);
                    confirmHash1 = tempS;
                }
                else if (line.Contains("Nonce 1:"))
                {
                    tempS = Common.splitAt(line, "Nonce 1: ")[1];
                    tempS = Common.RemoveWhitespace(tempS);
                    Nonce1 = Int32.Parse(tempS);
                }
                else if (line.Contains("Time 1:"))
                {
                    temparr = Common.splitAt(line, "Time 1: ");
                    POWTime1 = DateTime.ParseExact(temparr[1], "dd/MM/yyyy HH:mm:ss.ff", null);
                }
                else if (line.Contains("Confirming Address 2: "))
                {
                    pow2 = true;
                    tempS = Common.splitAt(line, "Address 2: ")[1];
                    tempS = Common.RemoveWhitespace(tempS);
                    confirmAddress2 = tempS;
                }
                else if (line.Contains("Confirming Hash 2:"))
                {
                    tempS = Common.splitAt(line, "Hash 2: ")[1];
                    tempS = Common.RemoveWhitespace(tempS);
                    confirmHash2 = tempS;
                }
                else if (line.Contains("Nonce 2:"))
                {
                    tempS = Common.splitAt(line, "Nonce 2: ")[1];
                    tempS = Common.RemoveWhitespace(tempS);
                    Nonce2 = Int32.Parse(tempS);
                }
                else if (line.Contains("Time 2:"))
                {
                    temparr = Common.splitAt(line, "Time 2: ");
                    POWTime2 = DateTime.ParseExact(temparr[1], "dd/MM/yyyy HH:mm:ss.ff", null);
                }
            }

            TangleTransaction tempTran;

            if (confirms)
            {
                tempTpos = ParseTPOText(confirmText);
            }
            if (pow1 && pow2 && confirms)
            {
                tempTran = new TangleTransaction(hash, sig, (DateTime)preTime, amount, toAdd, fromAdd, height, weight, confirmHash1, confirmAddress1, Nonce1, confirmHash2, confirmAddress2,
                                                                    Nonce2, (DateTime)POWTime1, (DateTime)POWTime2, tempTpos);
            }
            else if (pow1 && pow2)
            {
                tempTran = new TangleTransaction(hash, sig, (DateTime)preTime, amount, toAdd, fromAdd, height, weight, confirmHash1, confirmAddress1, Nonce1, confirmHash2, confirmAddress2,
                                                                    Nonce2, (DateTime)POWTime1, (DateTime)POWTime2);
            }
            else if (confirms)
            {
                tempTran = new TangleTransaction(hash, sig, (DateTime)preTime, amount, toAdd, fromAdd, height, weight,"None", "None",0,"None","None",0,null,null,tempTpos);
            }
            else
            {
                tempTran = new TangleTransaction(hash, sig, (DateTime)preTime, amount, toAdd, fromAdd, height, weight);
            }

            return tempTran;
        }

        public static List<TangleProofObject> ParseTPOText(String text)
        {
            List<TangleProofObject> tempTpos = new List<TangleProofObject>();

            try
            {
                if (!text.Contains("Confirmed by"))
                {
                    return tempTpos;
                }
                String[] confirms = Common.splitAt(text, "Confirmed by:");



                TangleProofObject tempTpo;
                String temps;

                foreach (String tempText in confirms)
                {
                    if (tempText.Contains("With"))
                    {
                        temps = "Confirmed by:" + tempText;
                        tempTpo = ParseTPO(temps);
                        tempTpos.Add(tempTpo);
                    }

                }
            }

            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return tempTpos;
        }

        public static TangleProofObject ParseTPO(String text)
        {
            String[] cleanLines = Common.splitAt(text, "newline");
            String tempS;
            String[] temparr;
            String confirmHash = "";

            String PowHash = "";

            DateTime? time = null;

            foreach (String line in cleanLines)
            {
                if (line.Contains("Confirmed by:"))
                {
                    tempS = Common.splitAt(line, "by: ")[1];
                    tempS = Common.RemoveWhitespace(tempS);
                    confirmHash = tempS;
                }
                else if (line.Contains("With Hash:"))
                {
                    tempS = Common.splitAt(line, "Hash: ")[1];
                    tempS = Common.RemoveWhitespace(tempS);
                    PowHash = tempS;
                }
                else if (line.Contains("At:"))
                {
                    temparr = Common.splitAt(line, "At: ");
                    time = DateTime.ParseExact(temparr[1], "dd/MM/yyyy HH:mm:ss.ff", null);
                }
            }

            TangleProofObject tpo = new TangleProofObject(confirmHash, PowHash, (DateTime)time);

            return tpo;

        }


        public void SetPath(int port)
        {
            path = TangleCommon.FindPath(port);
        }

        public static bool validateTran(TangleTransaction validate, TangleTransaction powtran1, TangleTransaction powtran2)
        {
            String hash = validate.hash;
            String sig = validate.signature;
            if (!Wallet.ValidateTangleSig(validate))
            {
                return false;
            }
            //confirm valid transaction ???

            //validate height
            try
            {
                if (!(calcHeight(powtran1, powtran2) == validate.height))
                {
                    return false;
                }

                //validate pow
                if (!validatePow(validate, powtran1, powtran2))
                {
                    return false;
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }

            return true;
        }

        public static bool validatePow(TangleTransaction validater, TangleTransaction originator,TangleTransaction originator2)
        {
            if (!validater.powConfirmHash1.Equals(CalcHash(originator.powConfirmHash1, originator.powConfirmHash2, originator.signature, validater.signature, validater.nonce1, validater.weight))){
                if (!validater.powConfirmHash1.Equals(CalcHash("", "", originator.signature, validater.signature, validater.nonce1, validater.weight)))
                {
                    return false;
                }
            }
            if (!validater.powConfirmHash2.Equals(CalcHash(originator2.powConfirmHash1, originator2.powConfirmHash2, originator2.signature, validater.signature, validater.nonce2, validater.weight))){
                if (!validater.powConfirmHash2.Equals(CalcHash("", "", originator2.signature, validater.signature, validater.nonce2, validater.weight)))
                {
                    return false;
                }
            }
            if (validater.confirmAddress1.Equals(validater.confirmAddress2))
            {
                if (validater.height > 5) //allow this up to a height of 5
                {
                    return false;
                }
            }
            return true;
        }

        public static double calcHeight(TangleTransaction tran1, TangleTransaction tran2)
        {
            if (tran1.height >= tran2.height)
            {
                return tran1.height + 1;
            }
            else
            {
                return tran2.height + 1;
            }
        }
    }
}