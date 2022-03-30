using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using TangleToken.Tanglecode;

namespace TangleToken
{
    public partial class Analysis : Form
    {

        List<TangleTransaction> trans;
        List<int> timePeriod = new List<int>();
        int maxTime = -1;
        int minTime = -1;

        int total = 0;

        int threePlusC = 0;
        int twoC = 0;
        int oneC = 0;
        int zeroC = 0;

        public bool isLog = false;

        public Analysis(List<TangleTransaction> list, bool type = false)
        {
            InitializeComponent();
            this.trans = list;
            if (type)
            {
                calcConfirmInfo();
            }
            else
            {
                calcTranInfo();
            }
        }

        public void calcConfirmInfo()
        {
            
        }

        public void confirmDisplay()
        {
            DateTime now = DateTime.Now;
            total = trans.Count;

            var chart = chart1.ChartAreas[0];
            chart.AxisX.IntervalType = DateTimeIntervalType.Number;


            chart.AxisX.Title = "Transaction";
            chart.AxisY.Title = "Time till first confirm (Seconds)";
            if (isLog)
            {
                chart.AxisY.Title = "Logged Time till first confirm (Seconds)";
            }


            chart.AxisX.Minimum = 0;
            //chart.AxisY.Maximum = timePeriod.Count;

            chart.AxisY.Minimum = 0;
            //chart.AxisY.Maximum = maxTime;

            chart.AxisX.Interval = 1;

            chart1.Series.Add("Times");
            chart1.Series["Times"].ChartType = SeriesChartType.Spline;
            chart1.Series["Times"].Color = Color.Red;
            chart1.Series[0].IsVisibleInLegend = false;
            chart1.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
            chart1.ChartAreas[0].AxisY.MajorGrid.Enabled = false;


            DateTime? tranTime = null;
            DateTime confirmTime;
            TimeSpan diff;
            double timeDiff;
            double avg;
            double sum = 0;
            double min = -1;
            double max = -1;

            double unconfsum = 0;
            double unconfavg;

            foreach (TangleTransaction t in trans)
            {
                if (t.confirms.Count > 0)
                {
                    
                    tranTime = t.POWTimestamp1;
                    confirmTime = t.confirms[t.confirms.Count - 1].POWTime;
                    diff = confirmTime.Subtract((DateTime)tranTime);
                    timeDiff = diff.TotalSeconds;
                    if (isLog)
                    {
                        chart1.Series["Times"].Points.AddXY(oneC, logMax(timeDiff));
                    }
                    else
                    {
                        chart1.Series["Times"].Points.AddXY(oneC, timeDiff);
                    }
                    
                    oneC++;
                    if (timeDiff < 0)
                    {
                        timeDiff = 0;
                    }
                    sum += timeDiff;
                    if (min == -1)
                    {
                        max = timeDiff;
                        min = timeDiff;
                    }
                    if (timeDiff > max)
                    {
                        max = timeDiff;
                    }
                    if (timeDiff < min)
                    {
                        min = timeDiff;
                    }

                }
                else
                {
                    zeroC++;
                    tranTime = t.POWTimestamp1;
                    diff = now.Subtract((DateTime)tranTime);
                    timeDiff = diff.TotalSeconds;
                    unconfsum += timeDiff;
                }
                if (t.confirms.Count > 1)
                {
                    twoC++;
                }
                if (t.confirms.Count > 2)
                {
                    threePlusC++;
                }

            }

            avg = sum / oneC;
            unconfavg = unconfsum / zeroC;
            double unconfP = ((double)zeroC / (double)total) * 100;
            double oneP = ((double)oneC / (double)total) * 100;
            double twoP = ((double)twoC / (double)total) * 100;
            double threeP = ((double)threePlusC / (double)total) * 100;

            label1.Text = "Total Transactions: " + total;
            label2.Text = "Unconfirmed Trans: " + zeroC + "  " + unconfP.ToString("G3") + "%";
            label9.Text = "Mean unconfirmed wait: " + unconfavg.ToString("G3");
            label3.Text = "1+ Confirms: " + oneC + "  " + oneP.ToString("G3") + "%";
            label4.Text = "2+ Confirms: " + twoC + "  " + twoP.ToString("G3") + "%";
            label5.Text = "3+ Confirms: " + threePlusC + "  " + threeP.ToString("G3") + "%";
            label6.Text = "Mean wait time: " + avg.ToString("G3");
            label7.Text = "Min wait: " + min.ToString("G3");
            label8.Text = "Max wait: " + max.ToString("G3");
        }

        public void calcTranInfo()
        {
            DateTime? lastTime = null;
            DateTime newTime;
            TimeSpan span;
            TangleTransaction last = null;
            foreach (TangleTransaction t in trans)
            {
                if (last != null)
                {
                    newTime = t.POWTimestamp1;
                    span = newTime.Subtract((DateTime)lastTime);
                    timePeriod.Add((int)span.TotalSeconds); 
                }
                last = t;
                lastTime = t.POWTimestamp1;
            }
            foreach (int time in timePeriod)
            {
                if (minTime == -1)
                {
                    maxTime = time;
                    minTime = time;
                }
                if (time > maxTime)
                {
                    maxTime = time;
                }
                if (time < minTime)
                {
                    minTime = time;
                }
            }
        }

        public void Display()
        {
            var chart = chart1.ChartAreas[0];
            chart.AxisX.IntervalType = DateTimeIntervalType.Number;


            chart.AxisX.Title = "Transactions";
            chart.AxisY.Title = "Time (Seconds)";
            if (isLog)
            {
                chart.AxisY.Title = "Logged Time (Seconds)";
            }
            


            chart.AxisX.Minimum = 1;
            chart.AxisY.Maximum = timePeriod.Count;

            chart.AxisY.Minimum = 0;
            chart.AxisY.Maximum = maxTime;

            chart.AxisX.Interval = 1;

            chart1.Series.Add("Times");
            chart1.Series["Times"].ChartType = SeriesChartType.Spline;
            chart1.Series["Times"].Color = Color.Red;
            chart1.Series[0].IsVisibleInLegend = false;
            chart1.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
            chart1.ChartAreas[0].AxisY.MajorGrid.Enabled = false;

            int counter = 1;

            foreach (int value in timePeriod)
            {
                if (isLog)
                {
                    chart1.Series["Times"].Points.AddXY(counter, logMax(value));
                }
                else
                {
                    chart1.Series["Times"].Points.AddXY(counter, value);
                }
                counter++;
            }

            label1.Text = "";
            label2.Text = "";
            label9.Text = "";
            label3.Text = "";
            label4.Text = "";
            label5.Text = "";
            label6.Text = "";
            label7.Text = "";
            label8.Text = "";
        }

        public void HashDisplay()
        {
            var chart = chart1.ChartAreas[0];
            chart.AxisX.IntervalType = DateTimeIntervalType.Number;


            chart.AxisX.Title = "Transactions";
            chart.AxisY.Title = "Hash attempts";
            if (isLog)
            {
                chart.AxisY.Title = "Logged Hash attempts";
            }


            chart.AxisX.Minimum = 0;
            //chart.AxisY.Maximum = timePeriod.Count;

            chart.AxisY.Minimum = 0;
            //chart.AxisY.Maximum = maxTime;

            chart.AxisX.Interval = 1;

            chart1.Series.Add("Hashes");
            chart1.Series["Hashes"].ChartType = SeriesChartType.Spline;
            chart1.Series["Hashes"].Color = Color.Red;
            chart1.Series[0].IsVisibleInLegend = false;
            chart1.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
            chart1.ChartAreas[0].AxisY.MajorGrid.Enabled = false;

            int counter = 0;

            double hashMin = -1;
            double hashMax = -1;
            double hashSum = 0;
            double hashAvg;
            double hashA = 0;


            foreach (TangleTransaction t in trans)
            {
                hashA = (t.nonce1 + t.nonce2);
                if (isLog)
                {
                    chart1.Series["Hashes"].Points.AddXY(counter, logMax(hashA));
                }
                else
                {
                    chart1.Series["Hashes"].Points.AddXY(counter, hashA);
                }
                
                counter++;
                hashSum += hashA;
                if (hashMin == -1)
                {
                    hashMin = hashA;
                    hashMax = hashA;
                }
                if (hashA > hashMax)
                {
                    hashMax = hashA;
                }
                if (hashA < hashMin)
                {
                    hashMin = hashA;
                }

            }

            hashAvg = hashSum / (double)counter;

            label1.Text = "Mean hashes: " + hashAvg.ToString("G3");
            label2.Text = "Min hashes: " + hashMin;
            label9.Text = "Max hashes: " + hashMax;
            label3.Text = "";
            label4.Text = "";
            label5.Text = "";
            label6.Text = "";
            label7.Text = "";
            label8.Text = "";
        }


        public double logMax(double number)
        {
            return Math.Max(0, Math.Log(number));
        }
    }

    
}
