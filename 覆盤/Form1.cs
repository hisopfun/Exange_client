using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System;
using System.IO;
using System.Threading;
using System.Reflection;
using NPlot;
using System.Net.Sockets;
using System.Data;
using System.Drawing;
using System.Linq;


//日線收斂尾  當沖容易亂竄  急拉急殺出量  
//percent profitable
//Rate of Ruturn
//Max strategy Drawdown
namespace 覆盤
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        object Lock;
        Thread T_Quote, T_GUI;
        TXF.K_data MKdata;
        TXF.K_data DKdata;
        Kline KL_1DK;
        string date;

        TIMES times;
        SOCKET orderSocket;
        SOCKET quoteSocket;
        TickEncoder TE = new TickEncoder();
        //TickEncoder tickGUI;
        int price = 10000;
        strategy plan10M, planMACD, planOSC, planNightReverse;

        public void write(string path, string content)
        {
            return;
            using (FileStream fs = new FileStream(path, FileMode.Append))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.Write(content);
                }
            }
        }

        private void DrawMat(strategy plan, string[] word, string BS)
        {
            if (BS != "X")
            {
                if (plan.dealPrice == 0)
                    plan.dealPrice = int.Parse(word[4]);
                else
                {
                    if (plan.last_bs == 0)
                        plan.dealPrice = plan.dealPrice + (int)plan.benefit;
                    else
                        plan.dealPrice = plan.dealPrice - (int)plan.benefit;
                }
                //if (plan10M.benefit != 0)
                //    stopLimitControl1.simu.MatList.Add(new Simulation.match(word[1], "TXF", BS, "1", (int.Parse(word[4]) + plan10M.benefit).ToString(), ""));
                //else
                stopLimitControl1.simu.MatList.Add(new Simulation.match(word[1], "TXF", BS, "1", plan.dealPrice.ToString(), ""));
                stopLimitControl1.simu.MatList[stopLimitControl1.simu.MatList.Count - 1].iTIME = MKdata.klist.Count;
                chartControl1.KL_1MK.DrawAllLpp(stopLimitControl1.simu);
            }
            if (plan.bs != -1 && BS != "X")
            {

                turn turn = new turn();
                Point x = new Point();
                if (BS == "B")
                    x = turn.BuyTurn(MKdata.klist);
                else if (BS == "S")
                    x = turn.SellTurn(MKdata.klist);

                plan.mitPrice = (int)x.price + ((plan.bs == 0) ? -1 : 1) * 3;

                if (x != null)
                {
                    NPlot.LabelPointPlot lpp = new NPlot.LabelPointPlot();
                    lpp.DataSource = new float[] { x.price };
                    lpp.AbscissaData = new int[] { x.iTime };
                    lpp.TextData = new string[] { "".ToString() };
                    lpp.LabelTextPosition = LabelPointPlot.LabelPositions.Below;
                    lpp.Marker.Size = 10;
                    lpp.Marker.Filled = true;
                    lpp.Marker.Color = System.Drawing.Color.Blue;
                    lpp.Marker.Type = Marker.MarkerType.Cross1;

                    chartControl1.KL_1MK.PS.Add(lpp);
                }
            }
        }

        public void OnMarketEncoded(object sender, ReportEventArgs e)
        {
            if (e.Report == "") return;


            if (e.Report.Contains("Mat")) {
                string[] tick = e.Report.Replace("\n", "").Replace("Mat:", "").Split(',');
                string time = tick[0].Substring(11,12).Replace(":","").Replace(".","");
                string pri = tick[4].Replace(".0", "");
                string qty = tick[3];
                string BS = tick[2];
                string stockNo = tick[1];
                TE.Encode($",{time},,,{pri},{qty},,,,,,{stockNo}");
            }

            if (e.Report.Contains("Ord"))
            {

                //stopLimitControl1.DeleteMarket();
                string[] marketLimit = e.Report.Replace("\n", "").Replace("Ord:", "").Replace(" ", "").Split('|');

                List<Simulation.match> dep = stopLimitControl1.simu.MarketList.ToList();
                for (int i = 0; i < dep.Count; i++)
                {
                    dep[i].Qty = "";
                }

                stopLimitControl1.simu.MarketList.RemoveRange(0, stopLimitControl1.simu.MarketList.Count);
                int BCount = 0, ACount = 0;
                foreach (string m in marketLimit) {
                    if (!m.Contains("B") && !m.Contains("S")) break;
                    string[] tick = m.Split(',');
                    string pri = tick[2].Replace(".0", "");
                    string qty = tick[3];
                    string BS = tick[1];
                    string stockNo = tick[0];

                    //count total market limit
                    if (tick[1] == "B")
                        BCount += int.Parse(tick[3]);
                    else if (tick[1] == "S")
                        ACount += int.Parse(tick[3]);



                    //change list
                    for (int i = 0; i < dep.Count; i++) {
                        if (pri == dep[i].Price) {
                            dep[i].Qty = qty;
                            break;
                        }
                        if (i == dep.Count - 1) {
                            dep.Add(new Simulation.match("", stockNo, BS, qty, pri, "0"));
                        }
                    }

                    //add marketlist
                    stopLimitControl1.simu.MarketList.Add(new Simulation.match("", stockNo, BS, qty, pri, "0"));
                }

                //update markdet limit
                for (int i = 0; i < dep.Count; i++)
                {
                    stopLimitControl1.setMarketValue(dep[i].Price, dep[i].BS, dep[i].Qty);
                }

                //total market limit
                stopLimitControl1.setMarketValue("0", "B", BCount.ToString());
                stopLimitControl1.setMarketValue("0", "S", ACount.ToString());
          
            }

        }
        public void OnReportEncoded(object sender, ReportEventArgs e) {
            if (e.Report == "") return;
            



            //mat report
            if (e.Report.Contains("Mat"))
            {
                string[] limit = e.Report.Replace("\n", "").Replace("Mat report:", "").Split(',');
                string time = limit[0].Substring(11, 12).Replace(":", "").Replace(".", "");
                string pri = limit[4].Replace(".0", "");
                string qty = limit[3];
                string BS = limit[2];
                string stockNo = limit[1];
                stopLimitControl1.simu.MatList.Add(new Simulation.match(time, stockNo, BS, qty, pri, "0"));
                linkLabel1.InvokeIfRequired(() =>
                {
                    if (BS == "B")
                    {
                        linkLabel1.Text = "Match: Buy " + qty + " " + pri;
                    }
                    else if (BS == "S")
                    {
                        linkLabel1.Text = "Match: Sell " + qty + " " + pri;
                    }
                });
            }

            //order report
            if (e.Report.Contains("Ord"))
            {
   
                //stopLimitControl1.DeleteLimit();
                string[] limit = e.Report.Replace("\n", "").Replace("Ord report:", "").Replace(" ", "").Split(',');
                string pri = limit[4].Replace(".0", "");
                string qty = limit[3];
                string BS = limit[2];
                stopLimitControl1.simu.Limit(limit[0], limit[1], limit[2], limit[3], pri);
       
                stopLimitControl1.setLimitValue(limit[4].Replace(".0", ""), limit[2],
                    stopLimitControl1.simu.LimList.Where(ee => ee.Price.ToString() == pri && ee.BS.ToString() == BS).Count().ToString());
             
            }

            //delete limit
            if (e.Report.Contains("Del"))
            {
                if (!e.Report.Contains("B") && !e.Report.Contains("S")) return;

                //stopLimitControl1.DeleteLimit();
                string[] limit = e.Report.Replace("\n", "").Replace("Del report:", "").Replace(" ", "").Split(',');
                string pri = limit[4].Replace(".0", "");
                string qty = limit[3];
                string BS = limit[2];
              
                stopLimitControl1.simu.DeleteOrder(stopLimitControl1.simu.LimList, limit[2], limit[4].Replace(".0", ""));
        
                stopLimitControl1.setLimitValue(limit[4].Replace(".0", ""), limit[2],
                            stopLimitControl1.simu.LimList.Where(ee => ee.Price.ToString() == pri && ee.BS.ToString() == BS).Count().ToString());
           
            }
        }

        public void OnOrderEncoded(object sender, OrderEventArgs e)
        {
            orderSocket.Send(orderSocket.Sclient, e.order);
        }

        public void OnTickEncoded(object sender, TicksEventArgs e)
        {
            string[] word = e.tick.Split(',');

            //Avoid PreOpen
            if (int.Parse(word[1].Substring(0, 4)) >= 0830 &&
                int.Parse(word[1].Substring(0, 4)) < 0845 ||
                int.Parse(word[1].Substring(0, 4)) >= 1450 &&
                int.Parse(word[1].Substring(0, 4)) < 1500)
                return;

            //StopLimit
            //stopLimitControl1.StopLimitDGV(word[4], word[2], word[3], word[5]);

            price = int.Parse(word[4]);

            //MK
            MKdata.Run(word[1], word[4], word[5]);

            //DK
            DKdata.Run("000000", word[4], word[5]);

            //MACD
            lock (Lock)
            {
                chartControl1.KL_1MK.mACD.macd(MKdata.klist);
                KL_1DK.mACD.macd(DKdata.klist);
            }

            //draw
            //MITandLIT(word);

        }

        public void MITandLIT(string[] word)
        {
            //Order
            List<string> Limits = stopLimitControl1.simu.MITToLimit(word[1], word[2], word[3], word[4]);
            List<string> Deals = stopLimitControl1.simu.DealInfo(word[1], word[2], word[3], word[4]);
            if (Deals != null && Deals.Count > 0)
            {
                //Draw MIT 
                stopLimitControl1.simu.MatList[stopLimitControl1.simu.MatList.Count - 1].iTIME = MKdata.klist.Count;
                chartControl1.KL_1MK.DrawAllLpp(stopLimitControl1.simu);
                chartControl1.KL_1MK.DrawAllHL(stopLimitControl1.simu);

                //Delete MIT DR
                stopLimitControl1.DeleteMIT(Limits);

                //Delete Lim DR
                stopLimitControl1.DeleteLimit(Deals);

                ////MatList
                dataGridView1.InvokeIfRequired(() =>
                {
                    dataGridView1.DataSource = null;
                    dataGridView1.DataSource = stopLimitControl1.simu.MatList;
                });
            }
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            KL_1DK = new Kline(plotSurface2D3, plotSurface2D4, plotSurface2D1, 1, 60);
            KL_1DK.KP = new CandleP(KL_1DK);

            radioButton1.Checked = true;
            chartControl1.InitChart(Kind.Line);
            tabControl1.TabPages.Remove(tabPage3);

            start("");
        }


        public void start(string date)
        {
            //comboBox1.Enabled = false;
            //button1.Enabled = false;
            //dateTimePicker1.Enabled = false;
            Lock = new object();
            dataGridView1.InvokeIfRequired(() =>
            {
                dataGridView1.DataSource = null;
            });

            button1.InvokeIfRequired(() =>
            {
                button1.BackColor = Color.FromArgb(255, 128, 128);
                button1.Enabled = false;
            });

            stopLimitControl1.Init();
            stopLimitControl1.OE.OrderEncoded += new OrderEncoder.OrderEncoderEventHandler(OnOrderEncoded);
            stopLimitControl1.AddPrice(10000);

            lock (Lock)
            {
                MKdata = new TXF.K_data();
                DKdata = new TXF.K_data();

                stopLimitControl1.simu = new Simulation(chartControl1.plotSurface2D1);
                chartControl1.KL_1MK.mACD = new Technical_analysis.MACD(chartControl1.KL_1MK.KLine_num);
                Kind kind = Kind.Both;
                radioButton1.InvokeIfRequired(() =>
                {
                    if (radioButton1.Checked)
                        kind = Kind.Line;
                });
                radioButton2.InvokeIfRequired(() =>
                {
                    if (radioButton2.Checked)
                        kind = Kind.Candle;
                });
                radioButton3.InvokeIfRequired(() =>
                {
                    if (radioButton3.Checked)
                        kind = Kind.Both;
                });
                chartControl1.InitChart(kind);
                //chartControl1.KL_1MK.InitMACDChart(chartControl1.KL_1MK.mACD = new Technical_analysis.MACD());

                plan10M = new strategy("plan10M", 0, 0, "100000", "134459", 20);
                plan10M.date = date;
                planMACD = new strategy("planMACD", 0, 0, "093000", "134459", 20);
                planOSC = new strategy("planOSC", 0, 0, "093000", "134459", 0);
                planNightReverse = new strategy("planNightReverse", 0, 0, "000000", "235959", 10);
                planNightReverse.date = date;
            }


            orderSocket = new SOCKET(date, "54.196.202.53", 3576);
            orderSocket.RE.ReportEncoded += new ReportEncoder.ReportEncoderEventHandler(OnReportEncoded);

            quoteSocket = new SOCKET(date, "174.129.45.152", 3575);
            quoteSocket.RE.ReportEncoded += new ReportEncoder.ReportEncoderEventHandler(OnMarketEncoded);

            TE.TickEncoded += new TickEncoder.TickEncoderEventHandler(OnTickEncoded);

            if (T_Quote != null)
                T_Quote.Abort();
            if (T_GUI != null)
                T_GUI.Abort();


            T_Quote = new Thread(quote);
            T_Quote.Start();
            
            T_GUI = new Thread(gui);
            T_GUI.Start();
        }



        private void button1_Click(object sender, EventArgs e)
        {
            start("");
            //Thread th = new Thread(manyDay);
            //th.Start();
        }


        public void WaitReopenData()
        {
        }

        public void quote()
        {

            //ticks
            while (orderSocket.t1.IsAlive || orderSocket.ticks.Count > 0)
            {

                
                if (orderSocket.ticks.Count > 0)
                {
                    string words = "";
                    lock (orderSocket.Lock)
                        words = orderSocket.ticks.Dequeue();
                    if (words == null) continue;
                    if (words == "") break;
                    string[] word = words.Split(',');

                    if (word.Length == 1) continue;
                    if (word[1].Length < 6) return;


                    //AM
                    if (int.Parse(word[1].Substring(0, 6)) < 084500 ||
                        int.Parse(word[1].Substring(0, 6)) > 134459)
                        continue;

                    ////Night
                    //if (int.Parse(word[1].Substring(0, 4)) >= 0830 &&
                    //    int.Parse(word[1].Substring(0, 4)) <= 1345)
                    //    continue;


                    //Avoid PreOpen
                    if (int.Parse(word[1].Substring(0, 4)) >= 0830 &&
                        int.Parse(word[1].Substring(0, 4)) < 0845 ||
                        int.Parse(word[1].Substring(0, 4)) >= 1450 &&
                        int.Parse(word[1].Substring(0, 4)) < 1500)
                        continue;


                    //Run All Ticks
                    orderSocket.RE.Encode(words);

                    //time interval
                    int ss = times.tDiff(word[1]);
                    if (ss > 0)
                        Thread.Sleep(ss);

                }
            }

            //Screenshot.CaptureMyScreen();


            button1.InvokeIfRequired(() =>
            {
                button1.Enabled = true;
                button1.BackColor = Color.FromArgb(128, 255, 128);
            });


            T_Quote.Abort();
        }

        //大量K畫線



        public void gui()
        {
            bool close = false;
            while (true)
            {
                Thread.Sleep(50);

                lock (Lock)
                {

                    stopLimitControl1.gui("10000", price);
                    if (MKdata.klist.Count > 0 && DKdata.klist.Count > 0)
                    {

                        //DGV
                        stopLimitControl1.AddPrice((int)MKdata.klist[MKdata.klist.Count - 1].close);

                        //StopLimit
                        

                        //time
                        if (MKdata.klist != null)
                        {
                            label4.InvokeIfRequired(() =>
                            {
                                label4.Text = MKdata.klist[MKdata.klist.Count - 1].time.Substring(0, 2).ToString() + ":" +
                                MKdata.klist[MKdata.klist.Count - 1].time.Substring(2, 2).ToString() + ":" +
                                MKdata.klist[MKdata.klist.Count - 1].time.Substring(4, 2).ToString();
                            });
                        }

                        //high - low 
                        if (DKdata.klist != null && DKdata.klist.Count > 0)
                        {
                            label13.InvokeIfRequired(() =>
                            {
                                label13.Text = (DKdata.klist[DKdata.klist.Count - 1].high - DKdata.klist[DKdata.klist.Count - 1].low).ToString();
                            });
                        }


                        //Qty
                        label3.InvokeIfRequired(() =>
                        {
                            label3.Text = stopLimitControl1.simu.Qty().ToString();
                        });

                        //Profit
                        label9.InvokeIfRequired(() =>
                        {
                            label9.Text = stopLimitControl1.simu.Profit(MKdata.klist[MKdata.klist.Count - 1].close.ToString());
                        });

                        //Entries
                        label11.InvokeIfRequired(() =>
                        {
                            label11.Text = stopLimitControl1.simu.Entries().ToString();
                        });

                        //MACD
                        chartControl1.KL_1MK.Adjust_MACD(chartControl1.KL_1MK.mACD);

                        ////MatList
                        //dataGridView1.InvokeIfRequired(() =>
                        //{
                        //    dataGridView1.DataSource = null;
                        //    dataGridView1.DataSource = stopLimitControl1.simu.MatList;
                        //});

                        lock (Lock)
                        {
                            chartControl1.KL_1MK.KP.refreshK(MKdata.klist);
                            KL_1DK.KP.refreshK(DKdata.klist);
                        }
                    }
                }

                string time = "";
                label4.InvokeIfRequired(() =>
                {
                    time = label4.Text.Replace(":", string.Empty);
                });

                if (time == "")
                    return;
                else if (time.Substring(0, 4) == "1100")
                {
                    linkLabel1.InvokeIfRequired(() =>
                    {
                        //textBox1.Text = "After 11:00, it can rise without large volume.";

                    });

                }

                if (close) T_GUI.Abort();
                if (T_Quote != null && !T_Quote.IsAlive) close = true;

            }
        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            label12.Text = "5AVG";
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            Lock = new object();
            lock (Lock)
            {
                chartControl1.InitChart(Kind.Line);
                if (stopLimitControl1.simu != null)
                {
                    chartControl1.KL_1MK.DrawAllLpp(stopLimitControl1.simu);
                    chartControl1.KL_1MK.DrawAllHL(stopLimitControl1.simu);
                }
            }
            if (T_GUI != null && !T_GUI.IsAlive)
            {
                T_GUI = new Thread(gui);
                T_GUI.Start();
            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            Lock = new object();
            lock (Lock)
            {
                chartControl1.InitChart(Kind.Candle);
                if (stopLimitControl1.simu != null)
                {
                    chartControl1.KL_1MK.DrawAllLpp(stopLimitControl1.simu);
                    chartControl1.KL_1MK.DrawAllHL(stopLimitControl1.simu);
                }
            }
            if (T_GUI != null && !T_GUI.IsAlive)
            {
                T_GUI = new Thread(gui);
                T_GUI.Start();
            }
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            Lock = new object();
            lock (Lock)
            {
                chartControl1.InitChart(Kind.Both);
                if (stopLimitControl1.simu != null)
                {
                    chartControl1.KL_1MK.DrawAllLpp(stopLimitControl1.simu);
                    chartControl1.KL_1MK.DrawAllHL(stopLimitControl1.simu);
                }
            }
            if (T_GUI != null && !T_GUI.IsAlive)
            {
                T_GUI = new Thread(gui);
                T_GUI.Start();
            }
        }


        private void button2_Click(object sender, EventArgs e)
        {
            tabControl1.Visible = !tabControl1.Visible;
            if (tabControl1.Visible)
                button2.BackColor = Color.Gold;
            else
                button2.BackColor = Color.Gray;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            //if (times != null)
            //    times.speed = int.Parse(comboBox1.Text);
        }

        private void comboBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != 8 && !Char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string link = "";
            if (linkLabel1.Text.Contains("https"))
            {
                int iStart = linkLabel1.Text.IndexOf("https");
                int iEnd = linkLabel1.Text.IndexOf(" ", iStart);

                link = linkLabel1.Text.Substring(iStart, iEnd - iStart + 1);
            }
            if (link != "")
                System.Diagnostics.Process.Start(link);
        }

        private void comboBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {

            //T_Quote.Start();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            T_Quote.Interrupt();
        }

        private void btn_B_Click(object sender, EventArgs e)
        {
            stopLimitControl1.simu.MatList.Add(new Simulation.match(label4.Text.Replace(":", string.Empty), "TXF", "B", "1", MKdata.klist[MKdata.klist.Count - 1].close.ToString(), ""));
            stopLimitControl1.simu.MatList[stopLimitControl1.simu.MatList.Count - 1].iTIME = MKdata.klist.Count;
            dataGridView1.DataSource = null;
            dataGridView1.DataSource = stopLimitControl1.simu.MatList;
            dataGridView1.Refresh();

            //draw on chart
            chartControl1.KL_1MK.DrawAllLpp(stopLimitControl1.simu);
        }

        private void btn_S_Click(object sender, EventArgs e)
        {
            stopLimitControl1.simu.MatList.Add(new Simulation.match(label4.Text.Replace(":", string.Empty), "TXF", "S", "1", MKdata.klist[MKdata.klist.Count - 1].close.ToString(), ""));
            stopLimitControl1.simu.MatList[stopLimitControl1.simu.MatList.Count - 1].iTIME = MKdata.klist.Count;
            dataGridView1.DataSource = null;
            dataGridView1.DataSource = stopLimitControl1.simu.MatList;
            dataGridView1.Refresh();

            //draw on chart
            chartControl1.KL_1MK.DrawAllLpp(stopLimitControl1.simu);
        }

        private void contextMenuStrip4_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (T_Quote != null)
                T_Quote.Abort();
            if (T_GUI != null)
                T_GUI.Abort();

            if (orderSocket != null)
                orderSocket.t1.Abort();

            if (quoteSocket != null)
                quoteSocket.t1.Abort();
        }



        private float AVG5()
        {
            float sum5 = 0;
            int  i = 5;
            while (i-- > 0) { 
                sum5 += DKdata.klist[DKdata.klist.Count - 1 - i].high - DKdata.klist[DKdata.klist.Count - i - 1].low;
            }
            return sum5 / 5;
        }
    }
    //擴充方法
    public static class Extension
    {
        //非同步委派更新UI
        public static void InvokeIfRequired(
            this Control control, MethodInvoker action)
        {
            try
            {
                if (control.InvokeRequired)//在非當前執行緒內 使用委派
                {
                    control.Invoke(action);
                }
                else
                {
                    action();
                }
            }
            catch (Exception ex) { }
        }
    }
    public static class ExtensionMethods
    {
        public static void DoubleBuffered(this object dgv, bool setting)
        {
            Type dgvType = dgv.GetType();
            PropertyInfo pi = dgvType.GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            pi.SetValue(dgv, setting, null);
        }
    }
}
