using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestCoin.Common;

namespace TangleToken.Tanglecode
{
    public class TangleReader
    {
        List<TangleTransaction> temp = new List<TangleTransaction>();
        TangleTransaction trantemp = null;
        List<TangleTransaction> sliceTemp = new List<TangleTransaction>();
        private String path;

        double tempindex = 0;


        public List<TangleTransaction> readTangleThread()
        {
            waitForFile(path);
            var promise = new Task(readWholeTangle);
            try
            {
                promise.Start();
                promise.Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return temp;
        }

        public double getLastTransactionIndexThread()
        {
            waitForFile(path);
            try
            {
                var promise = Task.Run(() => findLastIndex());
                //promise.Start();
                promise.Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return tempindex;
        }

        public List<TangleTransaction> readSectionTangleThread(double max, int range = 200)
        {
            waitForFile(path);
            try
            {
                var promise = Task.Run(() => readSectionTangle(max,range));
                //promise.Start();
                promise.Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return temp;
        }

        public List<TangleTransaction> readSliceTangleThread(double start = 0, double end = -1)
        {
            waitForFile(path);
            try
            {
                var promise = Task.Run(() => readSliceTangle(start, end));
                //promise.Start();
                promise.Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return sliceTemp;
        }

        public TangleTransaction readTranThread(String hash)
        {
            waitForFile(path);
            try
            {
                var promise = Task.Run(() => readTangleTran(hash));
                //promise.Start();
                promise.Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return trantemp;
        }

        public void readWholeTangle()
        {
            List<TangleTransaction> tangleTemp = new List<TangleTransaction>();

            String text = "";

            if (File.Exists(path))
            {
                try
                {
                    using (StreamReader sr = new StreamReader(path))
                    {
                        string s;
                        while ((s = sr.ReadLine()) != null)
                        {
                            if (s.Contains("Transaction Hash:") && (!text.Equals("")))
                            {
                                tangleTemp.Add(TangleTransaction.ParseTranText(text));
                                text = "";
                            }
                            text += s + "\n"; //debug line
                            //Console.WriteLine(s);
                        }
                        if (text != null)
                        {
                            tangleTemp.Add(TangleTransaction.ParseTranText(text));
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            temp = tangleTemp;

            return;

        }

        public void readSliceTangle(double start, double end)
        {
            List<TangleTransaction> tangleTemp = new List<TangleTransaction>();

            String text = "";
            if (end == -1)
            {
                end = double.PositiveInfinity;
            }
            double count = 0;
            if (File.Exists(path))
            {
                try
                {
                    using (StreamReader sr = new StreamReader(path))
                    {
                        string s;
                        while ((s = sr.ReadLine()) != null)
                        {
                            if (s.Contains("Transaction Hash:"))
                            {
                                if (count > start && count <= end+1 && (!text.Equals("")))
                                {
                                    tangleTemp.Add(TangleTransaction.ParseTranText(text));
                                }
                                count++;
                                text = "";
                            }
                            if (count >= start && count <= end+1)
                            {
                                text += s + "\n"; //debug line
                            }
                        }
                        if (text != null && (count >= start && count <= end+1))
                        {
                            if (text != String.Empty)
                            {
                                tangleTemp.Add(TangleTransaction.ParseTranText(text));
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            sliceTemp = tangleTemp;
        }

        public void readSectionTangle(double currentMax, int range)
        {
            List<TangleTransaction> tangleTemp = new List<TangleTransaction>();

            String text = "";
            double startPoint;
            if (range == -1)
            {
                startPoint = 0;
            }
            else
            {
                startPoint = currentMax - range;
            }
            int count = -1;
            if (File.Exists(path))
            {
                try
                {
                    using (StreamReader sr = new StreamReader(path))
                    {
                        string s;
                        while ((s = sr.ReadLine()) != null)
                        {
                            if (s.Contains("Transaction Hash:"))
                            {
                                count++;
                            }
                            if (s.Contains("Transaction Hash:") && (!text.Equals("")))
                            {
                                if (count > startPoint)
                                {
                                    tangleTemp.Add(TangleTransaction.ParseTranText(text));
                                }
                                text = "";
                            }
                            if (count >= startPoint)
                            {
                                text += s + "\n"; //debug line
                            }
                        }
                        if (text != null && !text.Equals(String.Empty))
                        {
                            tangleTemp.Add(TangleTransaction.ParseTranText(text));
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            temp = tangleTemp;
        }

        public void readTangleTran(String hash)
        {
            TangleTransaction tran = null;

            String text = "";

            bool record = false;
            bool finished = false;


            if (File.Exists(path))
            {
                try
                {
                    using (StreamReader sr = new StreamReader(path))
                    {
                        string s;
                        while ((s = sr.ReadLine()) != null)
                        {
                            
                            if (s.Contains("Transaction Hash: " + hash))
                            {
                                record = true;
                            }
                            else if (s.Contains("Transaction Hash:") && (!text.Equals("")))
                            {
                                if (record)
                                {
                                    record = false;
                                    tran = TangleTransaction.ParseTranText(text);
                                    trantemp = tran;
                                    return;
                                }
                                text = "";
                            }
                            if (record)
                            {
                                text += s + "\n"; //debug line
                            }
                        }
                        if (text != null)
                        {
                            tran = TangleTransaction.ParseTranText(text);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            trantemp = tran;
        }

        private void findLastIndex()
        {

            double counter = 0;

            if (File.Exists(path))
            {
                try
                {
                    using (StreamReader sr = new StreamReader(path))
                    {
                        string s;
                        while ((s = sr.ReadLine()) != null)
                        {
                            if (s.Contains("Transaction Hash:"))
                            {
                                counter++;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            tempindex = counter;

            return;
        }

        public void SetPath(int port)
        {
            path = TangleCommon.FindPath(port);
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
            catch (FileNotFoundException)
            {
                return true;
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

        /// <summary>
        /// Deletes all files in storage directory so Tangle can start over.
        /// </summary>
        /// <returns></returns>
        public static void DeleteAll()
        {
            DirectoryInfo di = new DirectoryInfo(Directory.GetCurrentDirectory().ToString() + "\\Tangle");

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }
        }

    }
}
