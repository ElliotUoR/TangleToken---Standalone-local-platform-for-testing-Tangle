using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TestCoin.Blockcode;
using TestCoin.Wallet;
using TestCoin.Connections;
using System.Configuration;
using System.Collections.Specialized;
using System.Threading;
using TangleToken.Tanglecode;

namespace TestCoin
{

    //acts as form and controller
    public partial class Form1 : Form 
    {

        public bool printActive = true;
        ConnectionController conControl;
        Tangle tangleToken;

        TangleTester tangleTester = null;

        bool AutoSave = bool.Parse(ConfigurationManager.AppSettings.Get("AutoSave"));
        bool AutoLoad = bool.Parse(ConfigurationManager.AppSettings.Get("AutoLoad"));
        bool AutoFill = bool.Parse(ConfigurationManager.AppSettings.Get("AutoFill"));
        bool CreateGen = bool.Parse(ConfigurationManager.AppSettings.Get("CreateGenesis"));

        int port = 20000;
        bool portReady = false;


        public Form1()
        {
            InitializeComponent();
            tangleToken = new Tangle();
            conControl = new ConnectionController(ProcessMessage,PortUpdate,AppServerPrint,this,null,AutoLoader,tangleToken);


            checkBox1.Checked = AutoFill;


            
            

            if (AutoLoad)
            {
                 //loads chain from file
            }
            
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


        public bool Print(String message)
        {
            richTextBox1.Invoke(new Action(() => richTextBox1.Text = message)); //thread line of good
            return true; 
        }

        public bool ServerPrint(String message)
        {
            richTextBox2.Invoke(new Action(() => richTextBox2.Text = message)); //bypasses thread restrictions somehow, this is convienient.
            richTextBox2.Invoke(new Action(() => richTextBox2.SelectionStart = richTextBox2.Text.Length));
            richTextBox2.Invoke(new Action(() => richTextBox2.ScrollToCaret()));
            return true;
        }

        public bool AppServerPrint(String message) //appends
        {
            if (printActive)
            {
                message = "\n" + message;
                richTextBox2.Invoke(new Action(() => richTextBox2.Text += message)); //bypasses thread restrictions somehow, this is convienient.
                richTextBox2.Invoke(new Action(() => richTextBox2.SelectionStart = richTextBox2.Text.Length));
                richTextBox2.Invoke(new Action(() => richTextBox2.ScrollToCaret())); ;
            }
            return true;
        }

        public void publish(String data, bool append = false)
        {
            if (append){
                richTextBox1.Text = richTextBox1.Text + data;
            }
            else
            {
                richTextBox1.Text = data;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }



        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {
            Print(tangleToken.DetailedBalanceCheck(textBox1.Text));
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        public static void miningComplete(Block block, Blockchain blockchain)
        {
            String mineDetails = blockchain.addMinedBlock(block);
            MiningStatusComplete MSC = new MiningStatusComplete(mineDetails);
            MSC.ShowDialog();    
        }
        

        public static void genBlockMined(Block block, Blockchain genChain)
        {
            genChain.GenBlockMined(new List<Block> { block });
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void button8_Click(object sender, EventArgs e)
        {
            String privateKey;
            Wallet.Wallet wallet = new Wallet.Wallet(out privateKey);
            publish("Your Wallet PublicID is " + wallet.publicID + "\n" +
                "Your Private Key is " + privateKey + "\n" +
                "Do not lose your private key, without it you will be unable to make transactions."
                );

            if (checkBox1.Checked)
            {
                textBox1.Text = wallet.publicID;
                textBox4.Text = wallet.publicID;
                textBox5.Text = privateKey;
            }

        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox7_TextChanged(object sender, EventArgs e)
        {

        }

        private void button10_Click(object sender, EventArgs e)
        {
            if (textBox4.Text.Equals(String.Empty) || textBox5.Text.Equals(String.Empty))
            {
                publish("Enter both publicID and private key forms for validation");
            }
            else
            {
                bool isValid = Wallet.Wallet.ValidatePrivateKey(textBox5.Text, textBox4.Text);
                if (isValid)
                {
                    publish("Private key is valid for " + textBox4.Text);
                }
                else
                {
                    publish("Private key is invalid");
                }
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            if(textBox4.Text.Equals(String.Empty) || textBox5.Text.Equals(String.Empty) || textBox6.Text.Equals(String.Empty))
            {
                publish("Enter publicID, private key and reciever ID forms for validation");
                return;
            } 
            if (Wallet.Wallet.ValidatePrivateKey(textBox5.Text, textBox4.Text))
            {
                String outputMessage;

                //creates new transaction and adds to pending transactions (if valid)

                bool validTransaction = tangleToken.GenerateTransaction(textBox6.Text, textBox4.Text,(double) numericUpDown1.Value, textBox5.Text, out outputMessage);

                if (validTransaction)
                {
                    publish("Transaction was successful and has been added to Tangle \n" + outputMessage);
                }
                else
                {
                    publish("Transaction failed \n" + outputMessage);
                }
            }
            else
            {
                publish("Private key is invalid");
            }
        }

        private void button11_Click(object sender, EventArgs e)
        {
            List<Block> throwaway;
            if (tangleToken.ValidateWholeTangle())
            {
                publish("Tangle is valid");
            }
            else
            {
                publish("Tangle is invalid");
            }
        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }


        

        public bool ProcessMessage(String message)
        {
            if (printActive)
            {
                String newMessage = "RECIEVED: " + message;
                AppServerPrint(newMessage);
            }


            return false; 
        }

        public bool PortUpdate(int port)
        {
            String message = "Listening on Port: " + port;
            PortChange(message);

            tangleToken.SetReaderPath(port);
            if (!tangleToken.CheckGenTran())
            {
                Print(tangleToken.CreateNewTangle(port));
            }

            return true;
        }

        public bool PortChange(String messsage)
        {
            try
            {
                label9.Invoke(new Action(() => label9.Text = messsage)); //bypasses thread restrictions somehow, this is convienient.
            }
            catch(Exception e)
            {

            }
            return true;
        }


        public bool BlockRead(Block block, string intent)
        {
            //do something with recieved block
            Print(block.loginfo());
            return true;
        }


        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }


        private void button16_Click(object sender, EventArgs e)
        {
            Print(tangleToken.GetSliceInfo(textBox7.Text, textBox8.Text));
        }




        private void button17_Click(object sender, EventArgs e) //Read All
        {
            richTextBox1.Text = tangleToken.ReadWholeText();
        }

        private void button18_Click(object sender, EventArgs e) //Read Last
        {
            Print(tangleToken.GetLastInfo());
        }


        private void button23_Click(object sender, EventArgs e)
        {
            richTextBox2.Text = "";
        }

        private void button20_Click(object sender, EventArgs e)
        {
            sendMessage("CONNECT");
        }

        private void button24_Click(object sender, EventArgs e)
        {
            string ip = textBox10.Text;
            string port = textBox9.Text;
            conControl.Ping(ip,port);
        }

        private void sendMessage(string message)
        {
            try
            {
                string ip = textBox10.Text;
                int port = Int32.Parse(textBox9.Text);
                conControl.PopMessage(message, port, ip);
            }
            catch (Exception error)
            {
                Print(error.ToString());
            }
        }


        private void button26_Click(object sender, EventArgs e)
        {
            sendMessage("REQUEST.NODE.POOL");
        }

        private void button27_Click(object sender, EventArgs e)
        {
            ProcessMessage(conControl.conPool.ToString());
        }

        private void button21_Click(object sender, EventArgs e)
        {
            sendMessage(textBox12.Text);
        }

        private void tabPage3_Click(object sender, EventArgs e)
        {

        }

        private void label12_Click(object sender, EventArgs e)
        {

        }

        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult dialog = MessageBox.Show("Do you really want to close program?", "Exit", MessageBoxButtons.YesNo);
            if (dialog == DialogResult.Yes)
            {
                e.Cancel = true;
                conControl.ShutDownGracefully(ShutDown);
            }
            else
            {
                e.Cancel = true;
            }
        }



        public bool ShutDown()
        {
            Environment.Exit(Environment.ExitCode);
            
            return true;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (!textBox4.Text.Equals(String.Empty))
            {
                Print(tangleToken.FaucetDistribution(textBox4.Text));
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Print(tangleToken.simpleTextDisplay());
        }

        private void button2_Click(object sender, EventArgs e)
        {
            TangleTester.SyncFiles();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (tangleTester == null)
            {
                int nodes = (int)numericUpDown3.Value;
                int TPM = (int)numericUpDown2.Value;

                tangleTester = new TangleTester(nodes, TPM, PrintTest1,PrintTest2,UpdateRunningNodes, tangleToken, nodesRunning);
            }
        }

        public bool UpdateRunningNodes(int num)
        {
            label11.Invoke(new Action(() => label11.Text = "Nodes: " + num));
            return true;
        }


        public bool PrintTest1(String message)
        {
            richTextBox3.Invoke(new Action(() => richTextBox3.Text += message + "\n")); //thread line of good
            richTextBox3.Invoke(new Action(() => richTextBox3.SelectionStart = richTextBox3.Text.Length));
            richTextBox3.Invoke(new Action(() => richTextBox3.ScrollToCaret()));
            return true;
        }

        public bool PrintTest2(String message)
        {
            richTextBox4.Invoke(new Action(() => richTextBox4.Text += message + "\n")); //thread line of good
            richTextBox4.Invoke(new Action(() => richTextBox4.SelectionStart = richTextBox4.Text.Length));
            richTextBox4.Invoke(new Action(() => richTextBox4.ScrollToCaret()));
            return true;
        }

        private void button28_Click(object sender, EventArgs e)
        {
            if (tangleTester != null)
            {
                tangleTester.StartNodes();
            }
        }

        private void button29_Click(object sender, EventArgs e)
        {
            if (tangleTester != null)
            {
                tangleTester.StopNodes();
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (tangleTester != null)
            {
                tangleTester.ShowGraph();
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (tangleTester != null)
            {
                tangleTester.ShowGraphConfirm();
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            printActive = checkBox2.Checked;
        }

        private void button12_Click(object sender, EventArgs e)
        {
            if (tangleTester != null)
            {
                tangleTester.ShowGraphHash();
            }
        }

        public bool nodesRunning(bool b)
        {
            if (b)
            {
                label10.Invoke(new Action(() => label10.Text = "Automation Running: True"));
            }
            else
            {
                label10.Invoke(new Action(() => label10.Text = "Automation Running: False"));
            }
            return b;
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (tangleTester != null)
            {
                tangleTester.logSetting = checkBox3.Checked;
            }
        }

        private void button13_Click(object sender, EventArgs e)
        {
            TangleReader.DeleteAll();
            this.Close();

        }
    }

    }
