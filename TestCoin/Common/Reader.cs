using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestCoin.Blockcode;

namespace TestCoin.Common
{
    class Reader
    {
        private Func<string, bool> printCallback;
        private Func<Block, string, bool> blockCallback;
        public Func<List<Block>, string, bool> chainCallback;
        private Func<BlockchainInfo, bool> infoCallback;
        public int port = 20000;
        private static String path = Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).ToString()).ToString()).ToString() + "\\Testchain.tcn";
        String intent; //defined by operation  called

        public Func<bool> genCallback = null;

        public Reader()
        {

        }

        public Reader(Func<string,bool> pcb, Func<Block, string, bool> bcb, Func<List<Block>, string, bool> ccb)
        {
            printCallback = pcb;
            blockCallback = bcb;
            chainCallback = ccb;
        }

        public void findLatestBlock()
        {
            intent = "LatestBlock";
        }

        public void getChainData()
        {
            intent = "ChainData";
            //Thread 
        }
        /// <summary>
        /// Gets slice of blockchain from file
        /// </summary>
        /// <param name="start"></param>
        /// <param name="finish"></param>
        public void getSlice(int start, int finish, String intent)
        {
            List<Block> chainsegment = new List<Block>();

            String blocktext = "";
            String[] splitter;
            int index;
            bool go = true;
            bool started = false; //toggled when starting index has been reached
            bool read = false;
            if (File.Exists(path))
            {
                try
                {
                    using (StreamReader sr = new StreamReader(path))
                    {
                        string s;
                        while ((s = sr.ReadLine()) != null && go)
                        {
                            if (s.Contains("Block Index: "))
                            {
                                index = getBlockIndex(s);
                                if (index >= start && index <= finish)
                                {
                                    if (started)
                                    {
                                        chainsegment.Add(ParseBlock(blocktext));
                                        blocktext = "";
                                    }
                                    read = true;
                                    started = true;

                                }
                                else if (index-1 == finish) {
                                    chainsegment.Add(ParseBlock(blocktext));
                                    blocktext = "";
                                    read = false;
                                    go = false;
                                }
                                else if (index > finish) //Shouldn't be hit, just in case
                                {
                                    read = false;
                                    go = false; //past range of blocks so finish reading file
                                }
                            }
                            if (read)
                            {
                                blocktext += s + "\n";
                            }
                            
                        }
                        if (go)
                        {
                            chainsegment.Add(ParseBlock(blocktext));
                        }
                        printCallback("Sliced!");
                        chainCallback(chainsegment, "Slice:" + intent);
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine("File Read Fail: " + e.Message);
                }
            }
        }

        public void readBThread(int index, int portNum)
        {
            port = portNum;
            path = FindPath();
            Thread _blockThread = new Thread(() => getSlice(index, index, "Block"));
            _blockThread.IsBackground = true;
            _blockThread.Start();
        }

        public void SliceThread(int start, int fin, String intent,int portNum)
        {
            port = portNum;
            path = FindPath();
            Thread _sliceThread = new Thread(() => getSlice(start, fin, intent));
            _sliceThread.IsBackground = true;
            _sliceThread.Start();
        }

        public void readBCThread(int portNum)
        {
            port = portNum;
            path = FindPath();
            Thread _readBlockchain = new Thread(readBlockchain);
            _readBlockchain.IsBackground = true;
            _readBlockchain.Start();
        }

        public void lastThread(int portNum)
        {
            port = portNum;
            path = FindPath();
            Thread _lastBlock = new Thread(LastBlock);
            _lastBlock.IsBackground = true;
            _lastBlock.Start();
        }


        public void readBlockchain()
        {
            List<Block> blocks = new List<Block>();

            String blocktext = "";
            if (File.Exists(path))
            {
                try
                {
                    using (StreamReader sr = new StreamReader(path))
                    {
                        string s;
                        while ((s = sr.ReadLine()) != null) {
                            if (s.Contains("Block Index") && (!blocktext.Equals(""))) {
                                blocks.Add(ParseBlock(blocktext));
                                blocktext = "";
                            }
                            blocktext += s + "\n"; //debug line
                            //Console.WriteLine(s);
                        }
                        if (blocktext != null)
                        {
                            blocks.Add(ParseBlock(blocktext));
                        }
                    }
                    blocktext += "Read blockchain successfully";

                    if (printCallback != null)
                    {
                        printCallback(blocktext);
                    }
                    chainCallback(blocks, "New Chain");
                }
                catch (Exception e)
                {
                    Console.WriteLine("File Read Fail: " + e.Message);
                }

            }
            else 
            {
                genCallback?.Invoke(); //if no file exist generate genesis block
            }
     
        }

        public static Block ParseBlock(String blocktext)
        {
            String[] blockdetails = splitAt(blocktext, "Transaction Details:");
            String blocks = blockdetails[0];
            String trans = blockdetails[1];
            Block block = null;
            int index = 0;
            DateTime? date = null;
            List<Transaction> transactions;
            String hash = "";
            String prevHash = "";
            long nonce = 0;
            String merkleRoot = "";
            double reward = 0;
            String minerAddress = "";
            double fees = 0;

            float difficulty = 0;
            int extraNonce = 0;

            String[] text = splitAt(blocks, "newline");

            String[] temparr;
            String tempS;
            foreach (String line in text)
            {
                
                if (line.Contains("Timestamp:"))
                {
                    temparr = splitAt(line, "Timestamp: ");
                    date = DateTime.ParseExact(temparr[1], "dd/MM/yyyy HH:mm:ss", null);
                    tempS = splitAt(temparr[0], "Block Index: ")[1];
                    tempS = Common.RemoveWhitespace(tempS);
                    index = Int32.Parse(tempS);
                }
                else if (line.Contains("Previous Hash:"))
                {
                    tempS = splitAt(line, "Previous Hash: ")[1];
                    tempS = Common.RemoveWhitespace(tempS);
                    prevHash = tempS;
                }
                else if (line.Contains("Hash:"))
                {
                    tempS = splitAt(line, "Hash: ")[1];
                    tempS = Common.RemoveWhitespace(tempS);
                    hash = tempS;
                }
                else if (line.Contains("Extra Nonce"))
                {
                    tempS = splitAt(line, "Extra Nonce: ")[1];
                    tempS = Common.RemoveWhitespace(tempS);
                    extraNonce = Int32.Parse(tempS);
                }
                else if (line.Contains("Nonce"))
                {
                    tempS = splitAt(line, "Nonce: ")[1];
                    tempS = Common.RemoveWhitespace(tempS);
                    nonce = Int64.Parse(tempS); //longs need that 64-bit conversion
                }
                else if (line.Contains("MerkleRoot:"))
                {
                    tempS = splitAt(line, "MerkleRoot: ")[1];
                    tempS = Common.RemoveWhitespace(tempS);
                    merkleRoot = tempS;
                }
                else if (line.Contains("Difficulty:"))
                {
                    String[] templineop = splitAt(line, "Difficulty: ");
                    tempS = splitAt(line, "Difficulty: ")[1];
                    tempS = Common.RemoveWhitespace(tempS);
                    difficulty = (float)Double.Parse(tempS);
                }
                else if (line.Contains("Fees Reward: "))
                {
                    tempS = splitAt(line, "Reward: ")[1];
                    tempS = Common.RemoveWhitespace(tempS);
                    fees = Double.Parse(tempS);
                }
                else if (line.Contains("Reward: "))
                {
                    temparr = splitAt(line, " to ");
                    tempS = Common.RemoveWhitespace(temparr[1]);
                    minerAddress = tempS;
                    tempS = splitAt(temparr[0], "Reward: ")[1];
                    tempS = Common.RemoveWhitespace(tempS);
                    reward = Double.Parse(tempS);
                }
            }


            transactions = ParseTransactions(trans);



            block = new Block(index, (DateTime) date, transactions, hash, prevHash, nonce, merkleRoot, reward, minerAddress, fees,difficulty,extraNonce);

            return block;
        }


        public void LastBlock()
        {
            Block block;
            String blocktext = "";
            bool read = false;
            int index = 0;
            if (File.Exists(path))
            {
                try
                {
                    using (StreamReader sr = new StreamReader(path))
                    {
                        string s;
                        while ((s = sr.ReadLine()) != null)
                        {
                            if (s.Contains("Block Index: "))
                            {
                                index = getBlockIndex(s);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("File Read Fail: " + e.Message);
                }
                try
                {
                    using (StreamReader sr = new StreamReader(path))
                    {
                        string s;
                        while ((s = sr.ReadLine()) != null)
                        {
                            if (s.Contains("Block Index: "))
                            {
                                if (index == getBlockIndex(s))
                                {
                                    read = true;
                                    blocktext += s + "\n";
                                }
                            }
                            else if (read)
                            {
                                blocktext += s + "\n";
                            }
                        }
                    }

                    block = ParseBlock(blocktext);
                    blockCallback(block, "Last");
                    printCallback(blocktext);

                }
                catch (Exception e)
                {
                    Console.WriteLine("File Read Fail: " + e.Message);
                }
            }
        }


        public static List<Transaction> ParseTransactions(String transText)
        {
            List<Transaction> trans = new List<Transaction>();

            String[] transArray = splitAt(transText, "Transaction Hash: ");

            int counter = 0;
            foreach (String tran in transArray)
            {
                if (counter != 0)
                {
                    trans.Add(ParseTransaction(tran));
                }
                counter++;
            }

            return trans;
        }


        public static Transaction ParseTransaction(String transText)
        {
            Transaction transaction = null;

            String fromAdd = "";
            String toAdd = "";
            double amount = 0;
            double fee = 0;
            String hash = "";
            String sig = "";
            DateTime? time = null;

            String[] text = splitAt(transText, "newline");

            hash = text[0];

            String tempS;

            for (int i = 1; i < text.Length; i++)
            {
                if (text[i].Contains("Signature: ")){
                    sig = splitAt(text[i], "Signature: ")[1];
                }
                else if (text[i].Contains("Transfered ")){
                    tempS = splitAt(text[i], "Transfered ")[1];
                    amount = Double.Parse(splitAt(tempS, " TestCoin")[0]);
                }
                else if (text[i].Contains("Fee: ")){
                    tempS = splitAt(text[i], "Fee: ")[1];
                    fee = Double.Parse(splitAt(tempS, " TestCoin")[0]);
                }
                else if (text[i].Contains("To: ")){
                    toAdd = splitAt(text[i], "To: ")[1];
                }
                else if (text[i].Contains("From: ")){
                    fromAdd = splitAt(text[i], "From: ")[1];
                }
            }

            transaction = new Transaction(fromAdd, toAdd, amount, fee, hash, sig);

            return transaction;
        }


        

        public static string[] splitAt(string line, string splitpoint)
        {
            if (splitpoint.Equals("newline"))
            {
                return line.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            }
            else
            {
                return line.Split(new[] { splitpoint }, StringSplitOptions.None);
            }
        }


        private int getBlockIndex(String line)
        {
            String[] temparr;
            String tempS;
            int index;
            temparr = splitAt(line, "Timestamp: ");
            tempS = splitAt(temparr[0], "Block Index: ")[1];
            tempS = Common.RemoveWhitespace(tempS);
            if (Int32.TryParse(tempS, out index))
            {
                return index;
            }
            else
            {
                return -1;
            }
        }

        public String FindPath()
        {
            try
            {
                Directory.CreateDirectory(Directory.GetCurrentDirectory().ToString() + "\\Storage");
            }
            catch(Exception e)
            {

            }

            try
            {
                Directory.CreateDirectory(Directory.GetCurrentDirectory().ToString() + "\\Storage\\" + port);
            }
            catch(Exception e)
            {

            }

            String path = Directory.GetCurrentDirectory().ToString() + "\\Storage\\" + port + "\\Testchain.tcn";
            return path;
        }

    }
}
