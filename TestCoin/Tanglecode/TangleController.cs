using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestCoin.Connections;
using TestCoin.Wallet;

namespace TangleToken.Tanglecode
{
    class TangleController
    {
        double TPMChance;

        public static Func<String, bool> FeedbackCallback;
        public static Func<String, bool> FeedbackCallback2;
        public static Func<String, String> getAddress;
        public static Func<TangleTransaction, bool> newTran;

        public int attemptCount = 0;

        public String publicID;
        private String privateID;

        public bool go = false;

        ConnectionController conControl;
        Tangle tangleToken;


        bool AutoSave = bool.Parse(ConfigurationManager.AppSettings.Get("AutoSave"));
        bool AutoLoad = bool.Parse(ConfigurationManager.AppSettings.Get("AutoLoad"));
        bool AutoFill = bool.Parse(ConfigurationManager.AppSettings.Get("AutoFill"));
        bool CreateGen = bool.Parse(ConfigurationManager.AppSettings.Get("CreateGenesis"));

        int port = 20000;
        bool portReady = false;

        public TangleController(double TPMChance)
        {
            this.TPMChance = TPMChance;
            Wallet wal = new Wallet(out privateID);
            publicID = wal.publicID;
            SetupTangle();
        }


        public void SetupTangle()
        {
            tangleToken = new Tangle();
            tangleToken.testCallback = NewTransactionFinished;
            conControl = new ConnectionController(UnusedCB, PortUpdate, UnusedCB, null, null, AutoLoader, tangleToken);
            Thread _TranGenThread = new Thread(AutoTranGenerate);
            _TranGenThread.IsBackground = true;
            _TranGenThread.Start();
        }


        public void AutoTranGenerate()
        {
            double roll;
            int extra = TransformString(publicID);
            Random ran = new Random(DateTime.Now.Millisecond + extra);
            while (true)
            {
                Thread.Sleep(10);
                while (TPMChance > 0)
                {
                    Thread.Sleep(10);
                    while(go)
                    {
                        Thread.Sleep(1000);
                        roll = ran.Next(1, 60000);
                        roll = roll / 1000;
                        if (TPMChance >= roll)
                        {
                            GenerateRandomTransaction();
                        }


                    }
                }
            }
        }

        public void GenerateRandomTransaction()
        {
            double balance = tangleToken.CheckBalance(publicID);
            double amount = 0;
            String reciever = getAddress(publicID);
            String output;
            if (balance > 0)
            {
                amount = new Random(DateTime.Now.Millisecond).NextDouble() * (balance / 10);
                tangleToken.GenerateTransaction(reciever, publicID, amount, privateID, out output, port);
            }
            if (attemptCount > 3)
            {
                tangleToken.GenerateTransaction(reciever, publicID, amount, privateID, out output, port);
                attemptCount = 0;
            }
            else
            {
                attemptCount++;
                tangleToken.FaucetDistribution(publicID);
            }
            
            
        }


        public bool NewTransactionFinished(TangleTransaction tran)
        {
            FeedbackCallback("New Tran, N: " + port + " H: " + tran.hash.Substring(0, 4) + "  A: " + tran.amount.ToString("G3"));
            newTran(tran);
            return true;
        }

        public void Start()
        {
            go = true;
        }

        public void Stop()
        {
            go = false;
        }

        public bool AutoLoader(int port)
        {
            if (AutoLoad)
            {
                this.port = port;
                portReady = true;
                //reader.readBCThread(port);

            }
            return AutoLoad;
        }

        public bool PortUpdate(int port)
        {
            FeedbackCallback("Node Setup on port " + port);
            tangleToken.SetReaderPath(port);
            return true;
        }

        public bool UnusedCB(Object ob)
        {
            return true;
        }

        public int TransformString(String str)
        {
            int sum = 0;
            foreach (char c in str)
            {
                sum += c;
            }
            return sum;
        }

    }
}
