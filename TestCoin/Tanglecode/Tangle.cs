using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestCoin.Wallet;

namespace TangleToken.Tanglecode
{
    public class Tangle
    {
        List<TangleTransaction> TheTangle = new List<TangleTransaction>(); //unsorted list of transactions (will be sorted by algortihms on use)
        double tokenCap = 2000000000;
        List<TangleTransaction> TangleCache = new List<TangleTransaction>(); //recent transactions that are kept in cache for operations
        List<TangleTransaction> processedTrans = new List<TangleTransaction>();
        int TCacheInitialSize = 50; //initail size of TangleCache
        int TCacheMaxSize = 100; //at 400 recalcs TangleCache
        public TangleReader reader;
        String path = String.Empty;


        public Func<TangleTransaction, bool> testCallback = null;
        public int port = -1;
        public int defaultPort;
        public Func<TangleTransaction, bool> NewTranCallback;

        //for replacing/changing tangle use https://stackoverflow.com/questions/13509532/how-to-find-and-replace-text-in-a-file-with-c-sharp
        //tutorials for dev https://www.youtube.com/watch?v=MsaPA3U4ung&list=PLmL13yqb6OxdIf6CQMHf7hUcDZBbxHyza&index=1

        public Tangle()
        {
            reader = new TangleReader();
        }

        public void RecalculateCache(int size = 50)
        {
            double lastIndex = reader.getLastTransactionIndexThread();
            List<TangleTransaction> templist = reader.readSectionTangleThread(lastIndex,-1);
            TangleCache = new List<TangleTransaction>();
            double startpoint = lastIndex - size;

            for (int i = 0; i < templist.Count; i++)
            {
                if (i >= startpoint)
                {
                    TangleCache.Add(templist[i]);
                }
            }
            //read 50 most recent 
            //TangleCache = most recent
        }

        public bool CheckGenTran()
        {
            return (!(reader.readSliceTangleThread(0, 1).Count == 0));
        }

        public String CreateNewTangle(int port = -1)
        {

            if (port != -1)
            {
                this.port = port;
            }

            String feedback = "";
            String genPubKey = "";
            String genPrivKey = "";

            TangleTransaction genTran = GenesisTransaction(out feedback, out genPubKey, out genPrivKey);
            
            //Save It

            if (port != -1)
            {
                List<TangleTransaction> list = new List<TangleTransaction>() { genTran };
                SaveChainInfo(list);
                AddToCache(genTran);
            }

            return feedback;
            
        }

        public void AppendToChain(TangleTransaction tran)
        {
            if (port != -1)
            {
                AppendSaveChainInfo(tran);
            }
        }

        public TangleTransaction GenesisTransaction(out String info, out String pubKey, out String privateKey)
        {
            Wallet wallet = new Wallet(out privateKey);
            pubKey = wallet.publicID;
            TangleTransaction tran = new TangleTransaction("UKsloLAbTbrNGUSC979Na376GHUyH6f6Ca4g02iOECy8FLhhAh6Kh5sHDi4uZ0V2bYbZItKssRY4hmqjRbpw5w==", pubKey, privateKey, tokenCap);
            info = "Genesis Tranasction generated:\n" + tran.hash;

            //Genesis Wallet PublicID is UKsloLAbTbrNGUSC979Na376GHUyH6f6Ca4g02iOECy8FLhhAh6Kh5sHDi4uZ0V2bYbZItKssRY4hmqjRbpw5w==
            //Genesis Private Key is udZoXTr9SRHmfoG/ja1BRJAJFWbf2xAda5lwmQUri7E=

            return tran;


        }

        

        public bool TransactionFinishedGenerating(TangleTransaction tran, TangleTransaction pow1, TangleTransaction pow2)
        {
            //add confirms to chain
            //AppendSaveChainInfo(pow1);
            //AppendSaveChainInfo(pow2);
            AppupdateChainInfo(pow1);
            if (!pow1.hash.Equals(pow2.hash))
            {
                AppupdateChainInfo(pow2);
            }

            //add to cache
            AppendToChain(tran);


            AddNewConfirmToCache(pow1);
            if (!pow1.hash.Equals(pow2.hash))
            {
                AddNewConfirmToCache(pow2);
            }
            AddToCache(tran);

            //share transaction
            NewTranCallback(tran);
            if (testCallback != null)
            {
                testCallback(tran);
            }

            return true;
        }

        public bool GenerateTransaction(String toAdd, String fromAdd, double amount, String privateKey, out String message, int port = -1)
        {
            if (!ValidateBalance(fromAdd, amount))
            {
                message = "Balance too low.";
                return false;
            }

            //look through cache find 2 transactions
            message = "";
            RecalculateCache(TCacheInitialSize);
            TangleTransaction tip1 = FindTip();
            TangleTransaction tip2 = FindTip(tip1); //dont want to find tip1 again
            TangleTransaction newTran = new TangleTransaction(amount, toAdd, fromAdd, privateKey, tip1, tip2, TransactionFinishedGenerating, port);

           

            return true;


        }

        public TangleTransaction FindTip(TangleTransaction dupe = null)
        {
            TangleTransaction tip = null;

            

            if (TangleCache.Count == 1)
            {
                return TangleCache[0];
            }

            if (dupe == null)
            {
                processedTrans = new List<TangleTransaction>();


                for (int i = 0; i < TangleCache.Count; i++)
                {
                    depthCount(TangleCache[i], true);
                }
            }

            //search through depth counted trans

            List<TangleTransaction> startTips = getUnconnectedNodes(TangleCache);

            tip = tipSearch(startTips);

            //look through cache and find

            if (tip == null || tip.Equals(dupe))
            {
                tip = tipSearch(startTips); //try twice for fairness?
                //keep searching

                if (tip == null || tip.height > 2)
                {
                    while (tip == null || tip.Equals(dupe))
                    {
                        tip = tipSearch(startTips);
                    }
                }
            }


            return tip;
        }

        public int depthCount(TangleTransaction t, bool unconnected = false)
        {
            t.unconnected = unconnected;
            int index = 0;

            if (CheckHashInList(processedTrans, t.hash))
            {
                return t.depth;
            }

            int childCount = 0;
            for (int i = 0; i < t.confirms.Count; i++)
            {
                try
                {
                    index = findIndexInList(TangleCache, t.confirms[i].hashingTransaction);
                    if (index > -1)
                    {
                        TangleTransaction temp = TangleCache[index];
                        if (temp != null)
                        {
                            childCount += depthCount(temp);
                        }
                    }
                }
                catch (Exception e)
                {
                    //Console.WriteLine(e.ToString());

                    return 0;
                }
            }

            t.depth = childCount + 1;
            processedTrans.Add(t);

            return t.depth;
        }


        public TangleTransaction tipSearch(List<TangleTransaction> tlist)
        {
            TangleTransaction temp = null;
            List<TangleTransaction> templist = new List<TangleTransaction>();
            List<int> depths = new List<int>();
            foreach (TangleTransaction t in tlist)
            {
                if (t != null)
                {
                    temp = t;
                    templist.Add(temp);
                    depths.Add(temp.depth);
                }
                else
                {

                }

            }
            int totaldepth = depths.Sum();
            int randomNum = new Random().Next(totaldepth);
            for (int i = 0; i < depths.Count; i++)
            {
                if (depths[i] >= randomNum)
                {
                    temp = templist[i];
                    break;
                }
                else
                {
                    randomNum -= depths[i];
                }

            }
            
            if (temp == null)
            {
                return temp;
            }
            
            if (temp.confirms.Count > 0)
            {
                temp = tipSearch(temp);
            }

            return temp;

        }

        public TangleTransaction tipSearch(TangleTransaction t)
        {
            TangleTransaction temp = null;
            List<TangleTransaction> templist = new List<TangleTransaction>();
            List<int> depths = new List<int>();
            foreach (TangleProofObject tpo in t.confirms)
            {
                temp = findHashInList(TangleCache, tpo.hashingTransaction);
                if (temp != null)
                {
                    templist.Add(temp);
                    depths.Add(temp.depth);
                }
                
            }
            if (t.confirms.Count <= 2 && lowDepthConfirmCheck(templist))
            {
                templist.Add(t);
                depths.Add(t.depth);
            }
            int totaldepth = depths.Sum();
            int randomNum = new Random().Next(totaldepth);
            for (int i = 0; i < depths.Count; i++)
            {
                if (depths[i] >= randomNum)
                {
                    temp = templist[i];
                    break;
                }
                else
                {
                    randomNum -= depths[i];
                }

            }

            if (temp.hash.Equals(t.hash))
            {
                return temp;
            }

            if (temp.confirms.Count > 0)
            {
                temp = tipSearch(temp);
            }

            return temp;

        }

        public static bool lowDepthConfirmCheck(List<TangleTransaction> list)
        {
            foreach(TangleTransaction t in list)
            {
                if (t.depth >= 1)
                {
                   // return false;
                }
            }
            return true;
        }

        public bool ValidateWholeTangle() //slow
        {
            List<TangleTransaction> wholeTangle = reader.readTangleThread();
            if (wholeTangle.Count < 1)
            {
                return false;
            }
            TangleValidationToken.SetList(wholeTangle);

            TangleTransaction GenesisTran = wholeTangle[0];

            bool validation = TangleValidationToken.GenesisValidation(GenesisTran);
            TangleValidationToken.ClearList();

            return validation;


            //start from genesis transaction verify tran
            return true;

        }

        public bool NewTransaction(TangleTransaction tran, List<TangleTransaction> list = null)
        {
            
            String findAddress1 = tran.confirmAddress1;
            String findAddress2 = tran.confirmAddress2;
            TangleTransaction findTran1 = reader.readTranThread(findAddress1);
            TangleTransaction findTran2 = reader.readTranThread(findAddress2);
            //find attached trans


            if (!TangleTransaction.validateTran(tran, findTran1, findTran2))
            {
                return false;
            }
            //validate transaction

            TangleProofObject tpo1 = new TangleProofObject(tran.hash, tran.powConfirmHash1, tran.POWTimestamp1);
            TangleProofObject tpo2 = new TangleProofObject(tran.hash, tran.powConfirmHash2, tran.POWTimestamp2);

            findTran1.confirms.Add(tpo1);
            findTran2.confirms.Add(tpo2);
            //add confirms to trans
            AppupdateChainInfo(findTran1);
            if (!findTran1.hash.Equals(findTran2.hash))
            {
                AppupdateChainInfo(findTran2);
            }


            //save tran to chain
            AppendToChain(tran);

            AddToCache(tran);
            AddNewConfirmToCache(findTran1);
            if (!findTran1.hash.Equals(findTran2.hash))
            {
                AddNewConfirmToCache(findTran2);
            }


            return true;
        }


        public bool DupeCheck(TangleTransaction tran)
        {
            List<TangleTransaction> list = reader.readTangleThread();
            return (CheckHashInList(list, tran.hash));

        }

        public bool DupeCheck(String hash)
        {
            List<TangleTransaction> list = reader.readTangleThread();
            return (CheckHashInList(list, hash));

        }


        public static TangleTransaction findHashInList(List<TangleTransaction> list, String hash)
        {
            TangleTransaction tran = null;
            List<TangleTransaction> copy = new List<TangleTransaction>(list);
            foreach (TangleTransaction t in copy)
            {
                if (t != null)
                {
                    if (t.hash.Equals(hash))
                    {
                        tran = t;
                    }
                }
            }

            return tran;
        }

        public static bool CheckHashInList(List<TangleTransaction> list, String hash)
        {
            List<TangleTransaction> copy = new List<TangleTransaction>(list);
            foreach (TangleTransaction t in copy)
            {
                if (t.hash.Equals(hash))
                {
                    return true;
                }
            }

            return false;
        }


        public static int findIndexInList(List<TangleTransaction> list, String hash)
        {
            List<TangleTransaction> copy = new List<TangleTransaction>(list);

            for (int i = 0; i < copy.Count; i++)
            {
                if (copy[i].hash.Equals(hash))
                {
                    return i;
                }
            }
            return -1;
        }

        public static List<TangleTransaction> getUnconnectedNodes(List<TangleTransaction> list)
        {
            List<TangleTransaction> unconnected = new List<TangleTransaction>();
            List<TangleTransaction> copy = new List<TangleTransaction>(list);

            foreach (TangleTransaction t in copy)
            {
                if (t.unconnected)
                {
                    unconnected.Add(t);
                }
            }

            return unconnected;
        }

        public String SetPath(int port)
        {
            String path;
            path = TangleCommon.FindPath(port);
            return path;
        }

        public void SetReaderPath(int port)
        {
            reader.SetPath(port);
            this.port = port;
        }

        public String SaveChainInfo(List<TangleTransaction> tangleList = null) //ideally make an append save too
        {
            if (tangleList == null)
            {
                tangleList = TheTangle;
            }
            //String saveLocation = Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).ToString()).ToString()).ToString() + "\\Testchain.tcn";
            String saveLocation = SetPath(port);

            using (StreamWriter file = new StreamWriter(saveLocation)) //tcn = testchain
            {
                foreach (TangleTransaction t in tangleList)
                {
                    file.Write(t.getInfo());
                }

                return saveLocation;
            }

        }

        public String updateChainInfo(TangleTransaction tran)
        {
            try
            {
                TangleTransaction oldTran = reader.readTranThread(tran.hash);
                String updatePath = SetPath(port);
                string text = File.ReadAllText(updatePath);
                text = text.Replace(oldTran.getInfo(), tran.getInfo());

                return updatePath;
            }
            catch(Exception e)
            {
                return e.ToString();
            }
        }

        public String AppupdateChainInfo(TangleTransaction tran)
        {
            try
            {
                TangleTransaction oldTran = reader.readTranThread(tran.hash);
                TangleProofObject newTPO = findNewTPO(oldTran,tran);
                if (newTPO == null)
                {
                    return "Cant find new confirm";
                }
                String updatePath = SetPath(port);
                waitForFile(updatePath);
                string[] contents = File.ReadAllLines(updatePath);

                bool ready = false;

                for (int i=0; i<contents.Length; ++i)
                {
                    if (contents[i].Contains("Transaction Hash: " + tran.hash)){
                        ready = true;
                    }
                    if(ready && contents[i].Contains("Confirms:"))
                    {
                        contents[i] += "\n" + newTPO.toText();
                        ready = false;
                    }
                }

                waitForFile(updatePath);

                File.WriteAllLines(updatePath, contents);

                return updatePath;
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public TangleProofObject findNewTPO(TangleTransaction oldTran, TangleTransaction newTran)
        {
            bool check = false;
            foreach (TangleProofObject tpo in newTran.confirms)
            {
                check = false;
                foreach(TangleProofObject tpj in oldTran.confirms)
                {
                    if (tpj.hashingTransaction.Equals(tpo.hashingTransaction))
                    {
                        check = true;
                    }
                }
                if (!check)
                {
                    return tpo;
                }
            }

            return null;
        }

        public String AppendSaveChainInfo(TangleTransaction tran = null) //Superior to SaveChainInfo as uses less memory, using SaveChainInfo is bad
        {
            try
            {
                if (tran == null)
                {
                    return SaveChainInfo(); //thinking emojie
                }


                

                //String AppendLocation = Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).ToString()).ToString()).ToString() + "\\Testchain.tcn";
                String AppendLocation = SetPath(port);

                waitForFile(AppendLocation);

                if (File.Exists(AppendLocation))
                {
                    using (StreamWriter file = File.AppendText(AppendLocation))
                    {
                        file.Write(tran.getInfo()); //appending > overwriting
                    }

                    return AppendLocation;
                }
                else
                {
                    return SaveChainInfo(); //boooo
                }
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public void AddToCache(TangleTransaction tran)
        {
            if (TangleCache.Count >= TCacheMaxSize)
            {
                RecalculateCache(TCacheInitialSize);
            }
            TangleCache.Add(tran);

        }

        public void AddNewConfirmToCache(TangleTransaction tran)
        {
            int index = findIndexInList(TangleCache, tran.hash);
            if (index == -1)
            {
                return;
            }
            else
            {
                TangleCache[index] = tran;
            }
        }

        public String ReadSliceText(double start, double end)
        {
            List<TangleTransaction> temp = reader.readSliceTangleThread(start, end);
            return TangleListToString(temp);
        }

        public String ReadTranText(String hash)
        {
            TangleTransaction tran = reader.readTranThread(hash);
            return tran.getInfo();
        }

        public String ReadWholeText()
        {
            List<TangleTransaction> tangleTemp = reader.readTangleThread();
            return TangleListToString(tangleTemp);
        }

        public String TangleListToString(List<TangleTransaction> list)
        {
            String info = "";
            foreach(TangleTransaction tran in list)
            {
                info += tran.getInfo();
            }
            return info;
        }


        public String ReadTangleIntoText()
        {
            String output = "";

            TangleTransaction start = reader.readSliceTangleThread(0, 1)[0];

            output = TangleTextRecursion(start,1);

            return output;
        }

        public String TangleTextRecursion(TangleTransaction tran, int scope)
        {
            String output = scope + String.Concat(Enumerable.Repeat("-", scope)) + tran.hash.Substring(0, 4) + "--H:" + tran.height + "--C:" + tran.confirms.Count + "\n";

            TangleTransaction temp;

            for (int i = 0; i < tran.confirms.Count; i++)
            {
                temp = reader.readTranThread(tran.confirms[i].hashingTransaction);
                output += TangleTextRecursion(temp, scope + 1);
            }


            return output;
        }


        public List<TangleTransaction> getSampleFrom(String hash)
        {
            List<TangleTransaction> list = reader.readTangleThread();
            int index = findIndexInList(list, hash);
            if (index == -1)
            {
                return null;
            }
            List<TangleTransaction> sample = new List<TangleTransaction>();
            for (int i = index; i < list.Count; i++)
            {
                sample.Add(list[i]);
            }

            return sample;

        }

        public double CheckBalance(String hash, bool getPendingBalance = false)
        {
            double balance = 0;

            List<TangleTransaction> trans = reader.readTangleThread();

            foreach (TangleTransaction tran in trans)
            {
                if (tran.fromAdd.Equals(hash))
                {
                    balance -= tran.amount;
                }
                if (tran.toAdd.Equals(hash) && (getPendingBalance || tran.confirms.Count >= 1 || tran.height == 1))
                {
                    balance += tran.amount;
                }

            }

            return balance;
        }

        public bool ValidateBalance(String hash, double amount)
        {
            double hashBal = CheckBalance(hash);
            return hashBal >= amount;
        }

        public String FaucetDistribution(String hash)
        {
            String output;
            if (CheckBalance(hash,true) == 0)
            {
                bool Success = GenerateTransaction(hash, "UKsloLAbTbrNGUSC979Na376GHUyH6f6Ca4g02iOECy8FLhhAh6Kh5sHDi4uZ0V2bYbZItKssRY4hmqjRbpw5w==", 500, "udZoXTr9SRHmfoG/ja1BRJAJFWbf2xAda5lwmQUri7E=", out output); //Uses Genesis wallet details

                if (Success)
                {
                    output = "Transaction for 500 TangleToken added to Tangle";
                }
                else
                {
                    output = "Transaction failed to generate";
                }
            }
            else
            {
                output = "Balance of receiving hash must be 0";
            }


            return output;
        }

        public String DetailedBalanceCheck(String hash)
        {
            if (hash.Equals(String.Empty))
            {
                return "Invalid Input";
            }


            String output = "Address: " + hash + "\n";
            output += "Availible Balance: " + CheckBalance(hash) + "\n";
            output += "Pending Balance: " + CheckBalance(hash, true) + "\n";

            return output;
        }

        public TangleTransaction GetLastInTangle()
        {
            try
            {
                double lastIndex = reader.getLastTransactionIndexThread();
                List<TangleTransaction> trans = reader.readSliceTangleThread(lastIndex);
                return trans[0];
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
                return null;
            }
        }

        public String GetLastInfo()
        {
            TangleTransaction temp = GetLastInTangle();
            if (temp != null)
            {
                return temp.getInfo();
            }
            return "Can't get last transaction";
        }

        public String GetSliceInfo(String startStr, String endStr = "")
        {
            String output = "";
            double start = 0;
            double end = -1;
            try
            {
                start = Convert.ToDouble(startStr);
                end = Convert.ToDouble(endStr);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            List<TangleTransaction> trans = reader.readSliceTangleThread(start, end);

            foreach (TangleTransaction tran in trans)
            {
                output += tran.getInfo();
            }


            return output;

        }

        public String simpleTextDisplay()
        {
            List<TangleTransaction> trans = reader.readTangleThread();

            String output = "";

            foreach (TangleTransaction tran in trans)
            {
                output += tran.hash.Substring(0, 4) + "    Height: " + tran.height + "    Confirms: " + tran.confirms.Count + "    Amount: " + tran.amount + "\n\n";
            }

            return output;
        }

        public TangleTransaction GetGenesis()
        {
            List<TangleTransaction> list = reader.readSliceTangleThread(0, 0);
            return list[0];

        }

        public void ResetTangle(TangleTransaction genesis)
        {
            TangleCache = new List<TangleTransaction>();
            SaveChainInfo(new List <TangleTransaction>{genesis});

        }

        public static bool IsFileReady(string filename)
        {
            // If the file can be opened for exclusive access it means that the file
            // is no longer locked by another process.
            try
            {
                using (FileStream inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None))
                    return inputStream.Length > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void waitForFile(String path)
        {
            while (!IsFileReady(path))
            {
                Thread.Sleep(100);
            }
        }


    }

}
