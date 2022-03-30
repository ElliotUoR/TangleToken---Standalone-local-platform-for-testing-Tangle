using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TangleToken.Tanglecode
{
    class TangleTester
    {
        List<TangleController> nodes = new List<TangleController>();
        List<String> nodeaddresses = new List<String>();
        Func<int, bool> runningNodes;
        Func<String, bool> print;
        Func<String, bool> print2;
        Func<bool, bool> isRunning;
        TangleTransaction firstTran = null;

        public bool logSetting = false;

        List<TangleTransaction> collection = new List<TangleTransaction>();

        bool started = false;

        Tangle mainTangle;

        int TPM;
        double TPMNode;

        public TangleTester(int nodes, int TPM, Func<String,bool> print, Func<String,bool> print2, Func<int,bool> updateRun, Tangle tangle, Func<bool,bool> isRunning)
        {
            TangleController.FeedbackCallback = print;
            TangleController.FeedbackCallback2 = print2;
            TangleController.getAddress = getRandomAddress;
            TangleController.newTran = NewTran;
            this.isRunning = isRunning;
            this.mainTangle = tangle;
            this.runningNodes = updateRun;
            this.print = print;
            this.print2 = print2;
            TPMNode = TPMCalc(TPM, nodes);
            this.nodes = PopulateNodes(nodes, TPMNode);

            print2("Nodes Created");
        }


        public List<TangleController> PopulateNodes(int amount, double TPM)
        {
            List<TangleController> list = new List<TangleController>();
            TangleController tC;

            for(int i = 0; i < amount; i++)
            {
                tC = new TangleController(TPM);
                nodeaddresses.Add(tC.publicID);
                list.Add(tC);
                runningNodes(list.Count);
            }

            return list;
        }

        public void StartNodes()
        {
            foreach (TangleController tc in nodes)
            {
                tc.Start();
            }
            if (!started)
            {
                started = true;
            }
            isRunning(true);
            print2("Nodes started");
        }

        public void StopNodes()
        {
            if (started)
            {
                foreach (TangleController tc in nodes)
                {
                    tc.Stop();
                }
            }
            isRunning(false);
            print2("Nodes stopped");
        }


        public double TPMCalc(int Tpm, int nodeCount)
        {
            double nodeTPM = ((double)Tpm) / ((double)nodeCount);

            return nodeTPM;


        }

        public String getRandomAddress(String currAddress)
        {
            if (nodeaddresses.Count > 1)
            {
                Random ran = new Random(DateTime.Now.Millisecond);
                int roll = ran.Next(0, nodeaddresses.Count);
                String addr = nodeaddresses[roll];
                if (addr.Equals(currAddress))
                {
                    return getRandomAddress(currAddress);
                }
                return addr;
            }
            return "error";
        }

        public bool NewTran(TangleTransaction t)
        {
            if (firstTran == null)
            {
                firstTran = t;
            }
            //something
            collection.Add(t);
            return true;
        }

        public static void SyncFiles()
        {
            String path = Directory.GetCurrentDirectory().ToString() + "\\Tangle";

            int workingDirCount = 50;
            for (int i = 1; i < workingDirCount; i++)
            {
                try
                {
                    File.Copy(path + "\\20000\\Tangle.tcn", path + "\\" + (20000 + i) + "\\Tangle.tcn", true);
                }
                catch (Exception e)
                {

                }

            }
        }

        public void ShowGraph()
        {
            Analysis graph = new Analysis(collection);
            graph.isLog = logSetting;
            graph.Show();
            graph.Display();
        }

        public void ShowGraphConfirm()
        {
            List<TangleTransaction> list = mainTangle.getSampleFrom(firstTran.hash);
            if (list == null)
            {
                return;
            }
            Analysis graph = new Analysis(list, true);
            graph.isLog = logSetting;
            graph.Show();
            graph.confirmDisplay();
        }

        public void ShowGraphHash()
        {
            Analysis graph = new Analysis(collection);
            graph.isLog = logSetting;
            graph.Show();
            graph.HashDisplay();

        }

        
    }
}
