using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace 覆盤
{
    public partial class StopLimitControl : UserControl
    {
        public StopLimitControl()
        {
            InitializeComponent();
        }

        public static class BidAsk
        {
            public static String StopBid { get { return "SBid"; } }
            public static String StopAsk { get { return "SAsk"; } }
            public static String LimitBid { get { return "LBid"; } }
            public static String LimitAsk { get { return "LAsk"; } }
            public static String MarketBid { get { return "Bid"; } }
            public static String MarketAsk { get { return "Ask"; } }
        }

        enum ScrollPosition { 
            Top, Mid, Down
        }

        public OrderEncoder OE;
        public DataTable dt;
        private int Ref, upPri, dnPri, lastPri;
        public string lastTime;
        public object Lock;
        public Simulation simu;
        public bool start = false;
        public bool autoScroll = true;

        public void Init() {
            InitVar();
            InitStopLimitDGV();
        }
        private void InitVar()
        {
            Ref = 0;
            upPri = 0;
            dnPri = 0;
            lastPri = 0;
            lastTime = "";
            start = false;
            Lock = new object();
            OE  = new OrderEncoder();
        }
        private void InitStopLimitDGV()
        {
            DGV_StopLimit.InvokeIfRequired(() =>
            {
                DGV_StopLimit.DataSource = null;
            });
            ExtensionMethods.DoubleBuffered(DGV_StopLimit, true);


            //Add DataColumn
            this.dt = new DataTable("QuoteTable");
            DataColumn Col1 = new DataColumn("nPrice", System.Type.GetType("System.String"));
            DataColumn Col6 = new DataColumn("SBid", System.Type.GetType("System.String"));
            DataColumn Col4 = new DataColumn("LBid", System.Type.GetType("System.String"));
            DataColumn Col2 = new DataColumn("Bid", System.Type.GetType("System.String"));
            DataColumn Col0 = new DataColumn("Price", System.Type.GetType("System.String"));
            DataColumn Col3 = new DataColumn("Ask", System.Type.GetType("System.String"));
            DataColumn Col5 = new DataColumn("LAsk", System.Type.GetType("System.String"));
            DataColumn Col7 = new DataColumn("SAsk", System.Type.GetType("System.String"));
            this.dt.Columns.Add(Col1);
            this.dt.Columns.Add(Col6);
            this.dt.Columns.Add(Col4);
            this.dt.Columns.Add(Col2);
            this.dt.Columns.Add(Col0);
            this.dt.Columns.Add(Col3);
            this.dt.Columns.Add(Col5);
            this.dt.Columns.Add(Col7);
            this.dt.PrimaryKey = new DataColumn[] { this.dt.Columns["nPrice"] };

            DGV_StopLimit.InvokeIfRequired(() => { 
                DGV_StopLimit.DataSource = this.dt;
                DGV_StopLimit.DefaultCellStyle.Font = new Font("Consolas", 12, FontStyle.Bold);
                
                DGV_StopLimit.ColumnHeadersDefaultCellStyle.Font = new Font("Consolas", 12, FontStyle.Bold);
                //DGV_StopLimit.DefaultCellStyle.Font.Style = FontStyle.
                DGV_StopLimit.DefaultCellStyle.BackColor = Color.FromArgb(64, 64, 64); 

                //visible
                DGV_StopLimit.Columns[0].Visible = false;
                //DGV_StopLimit.Columns[3].Visible = false;
                //DGV_StopLimit.Columns[5].Visible = false;
                
                //Not Allow Sort
                DGV_StopLimit.Columns[4].SortMode = DataGridViewColumnSortMode.NotSortable;

                //Buy Limit
                DGV_StopLimit.Columns[1].HeaderCell.Style.BackColor = Color.FromArgb(255, 192, 192);
                DGV_StopLimit.Columns[2].HeaderCell.Style.BackColor = Color.FromArgb(255, 192, 192);

                //Sell Limit
                DGV_StopLimit.Columns[6].HeaderCell.Style.BackColor = Color.FromArgb(192, 255, 192);
                DGV_StopLimit.Columns[7].HeaderCell.Style.BackColor = Color.FromArgb(192, 255, 192);

                //Pri
                DGV_StopLimit.Columns[4].HeaderCell.Style.BackColor = Color.FromArgb(255, 255, 192);

                //Adjust width
                DGV_StopLimit.Columns[1].Width = 38;
                DGV_StopLimit.Columns[2].Width = 38;
                DGV_StopLimit.Columns[3].Width = 38;
                DGV_StopLimit.Columns[4].Width = 72;
                DGV_StopLimit.Columns[5].Width = 38;
                DGV_StopLimit.Columns[6].Width = 38;
                DGV_StopLimit.Columns[7].Width = 38;//300
            });
        }


        public void StopLimitDGV(string nMatPri, string nBid, string nAsk, string nQty)
        {

            //return;
            if (start)
            {
                DataRow DR = this.dt.Rows.Find(nMatPri);
                DGV_StopLimit.Enabled = true;
                if (DR != null)
                {
                    lock (Lock)
                        DR["Price"] = $"{nMatPri} ({nQty})";

                    DR.EndEdit();
                    DR.AcceptChanges();
                }
            }
        }
        public void AddPrice(int nRef)
        {
            //return;
            //Add Price DataRow
            DGV_StopLimit.InvokeIfRequired(() =>
            {
                DGV_StopLimit.Enabled = false;
                if (start == false)
                {
                    //blank row
                    DataRow DR = this.dt.NewRow();
                    lock (Lock)
                    {
                        DR["nPrice"] = "0";
                    }
                    this.dt.Rows.Add(DR);


                    upPri = Convert.ToInt32(Math.Floor(nRef * 1.1));
                    dnPri = Convert.ToInt32(Math.Ceiling(nRef * 0.9));
                    
                    int i;
                    for (i = upPri; i >= dnPri; i--)
                    {
                        DR = this.dt.NewRow();
                        lock (Lock)
                        {
                            DR["nPrice"] = i.ToString();
                            DR["Price"] = i.ToString();
                        }
                        this.dt.Rows.Add(DR);
                    }
                    DGV_StopLimit.Rows[0].Frozen = true;
                }
                DGV_StopLimit.Enabled = true;
            });


            start = true;
        }

        private void DGV_StopLimit_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            string Pri = "";
            if (e == null || e.RowIndex < 0) return;
            Pri = DGV_StopLimit[0, e.RowIndex].Value.ToString();

            //DGV_StopLimit.InvokeIfRequired(() =>
            //{
            //    DGV_StopLimit.Enabled = false;
            //    DeleteMIT();
            //    DeleteLimit();

            //    DGV_StopLimit.ClearSelection();
            //    DGV_StopLimit.Enabled = true;
            //});
            if (e.Button == MouseButtons.Left)
            {
                if (e.RowIndex == 0) return;
                if (e.ColumnIndex != 4)
                {

                    //Limit Buy 
                    if (e.ColumnIndex == 2)
                    {
                        //simu.Limit(lastTime, "TXF", "B", "1", Pri);
                        OE.Encode(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff")+",TXF,B,"+ Pri+",1");
                    }

                    //Limit Buy 
                    if (e.ColumnIndex == 6)
                    {
                        //simu.Limit(lastTime, "TXF", "S", "1", Pri);
                        OE.Encode(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff") + ",TXF,S," + Pri + ",1");
                    }
                }
            }
        }

        private void DGV_StopLimit_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            string Pri = "";
            if (e == null || e.RowIndex < 0) return;
            Pri = DGV_StopLimit[0, e.RowIndex].Value.ToString();

            //DGV_StopLimit.InvokeIfRequired(() =>
            //{
            //    DGV_StopLimit.Enabled = false;
            //    DeleteMIT();
            //    DeleteLimit();
                
           

            //    DGV_StopLimit.ClearSelection();
            //    DGV_StopLimit.Enabled = true;
            //});
            if (e.Button == MouseButtons.Left)
            {
                if (e.RowIndex == 0) return;
                if (e.ColumnIndex != 4)
                {

                    //Limit Buy 
                    if (e.ColumnIndex == 2)
                    {
                        //simu.Limit(lastTime, "TXF", "B", "1", Pri);
                        OE.Encode(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff") + ",TXF,B," + Pri + ",1");
                    }

                    //Limit Buy 
                    if (e.ColumnIndex == 6)
                    {
                        //simu.Limit(lastTime, "TXF", "S", "1", Pri);
                        OE.Encode(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff") + ",TXF,S," + Pri + ",1");
                    }
                }
            }
        }

        public void gui(string time, int pri) {
            DGV_StopLimit.InvokeIfRequired(() =>
            {
                DGV_StopLimit.Enabled = false;
                if (start)
                {

                    //Display MatPri
                    if (upPri != 0 && dnPri != 0)
                    {
                        if (lastPri != 0)
                        {
                            DGV_StopLimit[4, upPri - lastPri + 1].Style.BackColor = Color.FromArgb(64, 64, 64);
                        }
                        lastPri = pri;
                        lastTime = time;

                        DGV_StopLimit[4, upPri - lastPri + 1].Style.BackColor = Color.Orange;
                    }

                    //Run MIT and Limit
                    RUN_MIT_Limit();

                    if (autoScroll)
                    {
                        ScrollingTo(DGV_StopLimit, upPri - lastPri + 1, false);
                    }

                }
                DGV_StopLimit.Enabled = true;
            });
        }

        public void DeleteMarket()
        {
            for (int i = 0; i < simu.MarketList.Count; i++)
            {
                //string BS = (simu.MITList[i].BS == "B") ? BidAsk.StopBid : BidAsk.StopAsk;
                //DataRow DR = this.dt.Rows.Find(simu.MITList[i].Price);
                //lock (Lock)
                //    DR[BS] = "";
                setMarketValue(simu.MarketList[i].Price, simu.MarketList[i].BS, "");
            }
            setMarketValue("0", "B", "");
            setMarketValue("0", "S", "");
        }

        public void DeleteMIT() 
        {
            for(int i = 0; i < simu.MITList.Count; i++) 
            {
                //string BS = (simu.MITList[i].BS == "B") ? BidAsk.StopBid : BidAsk.StopAsk;
                //DataRow DR = this.dt.Rows.Find(simu.MITList[i].Price);
                //lock (Lock)
                //    DR[BS] = "";
                setMITValue(simu.MITList[i].Price, simu.MITList[i].BS, "");
            }
            setMITValue("0", "B", "");
            setMITValue("0", "S", "");
        }

        public void DeleteLimit()
        {
            for (int i = 0; i < simu.LimList.Count; i++)
            {
                if (simu.LimList[i].Price == "M") continue;
                //string BS = (simu.LimList[i].BS == "B") ? BidAsk.LimitBid : BidAsk.LimitAsk;
                //DataRow DR = this.dt.Rows.Find(simu.LimList[i].Price);
                //lock (Lock)
                //    DR[BS] = "";
                setLimitValue(simu.LimList[i].Price, simu.LimList[i].BS, "");
            }

            setLimitValue("0", "B", "");
            setLimitValue("0", "S", "");


        }
        public void DeleteMIT(List<string> Limits)
        {
            foreach (string lim in Limits)
            {
                string price = lim.Split(',')[2];
                //string BS = (ord.Split(',')[1] == "B") ? BidAsk.StopBid : BidAsk.StopAsk;
                //DataRow DR = this.dt.Rows.Find(price);
                //lock (Lock)
                //    DR[BS] = "";
                setMITValue(lim.Split(',')[2], lim.Split(',')[1], "");
            }
            setMITValue("0", "B", "");
            setMITValue("0", "S", "");
        }

        public void DeleteLimit(List<string> Deals)
        {
            foreach (string mat in Deals)
            {
                string price = mat.Split(',')[2];
                if (price == "M") continue;
                //string BS = (mat.Split(',')[1] == "B") ? BidAsk.LimitBid : BidAsk.LimitAsk;
                //DataRow DR = this.dt.Rows.Find(price);
                //lock (Lock)
                //    DR[BS] = "";
                setLimitValue(mat.Split(',')[2], mat.Split(',')[1], "");
            }
            setLimitValue("0", "B", "");
            setLimitValue("0", "S", "");

            //if OI is not equall 0, change color
            int OI = simu.Qty();
            if (OI > 0)
            {
                DGV_StopLimit.DefaultCellStyle.BackColor = Color.FromArgb(255, 192, 192);
                DGV_StopLimit.Columns[4].DefaultCellStyle.BackColor = Color.FromArgb(64, 64, 64); ;
            }
            else if (OI < 0)
            {
                DGV_StopLimit.DefaultCellStyle.BackColor = Color.FromArgb(192, 255, 192);
                DGV_StopLimit.Columns[4].DefaultCellStyle.BackColor = Color.FromArgb(64, 64, 64); ;
            }
            else
            {
                DGV_StopLimit.DefaultCellStyle.BackColor = Color.FromArgb(64, 64, 64); ;
            }
        }

        public void setMarketValue(string Price, string BS, string Qty)
        {
            DataRow DR = this.dt.Rows.Find(Price);
            if (DR == null) return;
            BS = (BS == "B") ? BidAsk.MarketBid : BidAsk.MarketAsk;
            Qty = Qty == "0" ? "" : Qty;
            lock (Lock)
            {
                DR[BS] = Qty;

                DR.EndEdit();
                DR.AcceptChanges();
            }
        }

        public void setLimitValue(string Price, string BS, string Qty) {
            DataRow DR = this.dt.Rows.Find(Price);
            if (DR == null) return;
            BS = (BS == "B") ? BidAsk.LimitBid : BidAsk.LimitAsk;
            Qty = Qty == "0" ? "" : Qty;
            lock (Lock)
            {
                DR[BS] = Qty;

                DR.EndEdit();
                DR.AcceptChanges();
            }
        }

        private void setMITValue(string Price, string BS, string Qty)
        {
            DataRow DR = this.dt.Rows.Find(Price);
            if (DR == null) return;
            BS = (BS == "B") ? BidAsk.StopBid : BidAsk.StopAsk;
            Qty = Qty == "0" ? "" : Qty;
            lock (Lock)
            {
                DR[BS] = Qty;

                DR.EndEdit();
                DR.AcceptChanges();
            }
        }

        private void DGV_StopLimit_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
        {
            autoScroll = false;
        }

        private void DGV_StopLimit_CellMouseLeave(object sender, DataGridViewCellEventArgs e)
        {
            autoScroll = true;
        }

        private void DGV_StopLimit_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void RUN_MIT_Limit() {

            //int? xxx = null; //TEST
            List<Simulation.match> seen = new List<Simulation.match>();
            int BCount = 0, ACount = 0;
            lock (simu.Lock)
            {

                for (int i = 0; i < simu.MarketList.Count; i++)
                    setMarketValue(simu.MarketList[i].Price, simu.MarketList[i].BS, simu.MarketList[i].Qty.ToString());

                for (int i = 0; i < simu.LimList.Count; i++)
                {

                    //Avoid
                    string Pri = simu.LimList[i].Price;
                    if (simu.LimList[i].Price == "M") continue;

                    //Total Bid Ask Count
                    if (simu.LimList[i].BS == "B")
                        BCount++;
                    else
                        ACount++;

                    //Calculate seen Count
                    int find = (from data in seen
                                where data.Price == simu.LimList[i].Price && data.BS == simu.LimList[i].BS
                                select data).Count();

                    //Set Value
                    //setLimitValue(simu.LimList[i].Price, simu.LimList[i].BS, (find + 1).ToString());

                    //Add to seen list
                    seen.Add(simu.LimList[i]);
                }

                setLimitValue("0", "B", BCount.ToString());
                setLimitValue("0", "S", ACount.ToString());


                BCount = 0; ACount = 0;
                seen = new List<Simulation.match>();
                for (int i = 0; i < simu.MITList.Count; i++)
                {

                    //Total Bid Ask Count
                    if (simu.MITList[i].BS == "B")
                        BCount++;
                    else
                        ACount++;

                    //Calculate seen Count
                    int find = (from data in seen
                                where data.Price == simu.MITList[i].Price && data.BS == simu.MITList[i].BS
                                select data).Count();

                    //Set Value
                    setMITValue(simu.MITList[i].Price, simu.MITList[i].BS, (find + 1).ToString());

                    //Add to seen list
                    seen.Add(simu.MITList[i]);
                }
                if (seen.Count > 0)
                {
                    setMITValue("0", "B", BCount.ToString());
                    setMITValue("0", "S", ACount.ToString());
                }
            }
        }


        
        public void DGV_StopLimit_DataError(object sender, DataGridViewDataErrorEventArgs anError) {
            return;
            using (FileStream fs = new FileStream(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + "//DataErr.TXT", FileMode.Append)) {
                using (StreamWriter sw = new StreamWriter(fs)) {
                    sw.WriteLine(anError.ToString());
                }
            }
        }



        private void ScrollingTo(DataGridView view, int rowToShow, bool Mid)
        {
 
            view.InvokeIfRequired(() =>
            {
                if (Mid)
                {
                    if (rowToShow >= 0 && rowToShow < view.RowCount)
                    {
                        var countVisible = view.DisplayedRowCount(false);
                        var firstVisible = view.FirstDisplayedScrollingRowIndex;
                        int mid = Math.Max(0, rowToShow - countVisible / 2);
                        lock(Lock)
                            view.FirstDisplayedScrollingRowIndex = mid;
                        return;
                    }
                }

                if (!Mid)
                {
                    if (rowToShow >= 0 && rowToShow < view.RowCount)
                    {
                        int countVisible = view.DisplayedRowCount(false);
                        int firstVisible = view.FirstDisplayedScrollingRowIndex;

                        if (rowToShow < view.FirstDisplayedScrollingRowIndex)
                        {
                            //using (FileStream fs = new FileStream(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + "//test.TXT", FileMode.Append))
                            //{
                            //    using (StreamWriter sw = new StreamWriter(fs))
                            //    {
                            //        sw.WriteLine(lastPri + "," + rowToShow + "," + firstVisible + "," + (firstVisible + countVisible) + "," + "TOP");
                            //    }
                            //}
                            lock(Lock)
                                view.FirstDisplayedScrollingRowIndex = Math.Max(0, rowToShow);

                        }
                        else if (rowToShow >= firstVisible + countVisible)
                        {
                            //using (FileStream fs = new FileStream(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + "//test.TXT", FileMode.Append))
                            //{
                            //    using (StreamWriter sw = new StreamWriter(fs))
                            //    {
                            //        sw.WriteLine(lastPri + "," + rowToShow + "," + firstVisible + "," + (firstVisible + countVisible) + "," + "Down");
                            //    }
                            //}
                            lock(Lock)
                                view.FirstDisplayedScrollingRowIndex = Math.Max(0, rowToShow - countVisible + 1);

                        }


                        return;
                    }

                }
            });

        }
    }
}
