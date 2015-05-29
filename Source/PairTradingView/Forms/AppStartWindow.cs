﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using PairTradingView.Data;
using PairTradingView.Data.CSVData;
using PairTradingView.Data.Entities;
using PairTradingView.Data.SqlData;
using PairTradingView.Synthetics;

namespace PairTradingView.Forms
{
    public partial class AppStartWindow : Form
    {
        private MainWindow mWindow = null;

        public AppStartWindow(MainWindow mainWindow)
        {
            InitializeComponent();

            this.mWindow = mainWindow;

            progressBar2.MarqueeAnimationSpeed = 0;

            radioSQL.Checked = true;
            radioCSV.Checked = false;

            priceIndexUpDown.Minimum = 5;
            volumeIndexUpDown.Minimum = 6;

            deltaTypeBox.Items.AddRange(new[] 
            {   "Ratio", 
                "RatioIncludingBeta", 
                "Spread", 
                "SpreadIncludingBeta" 
            });

            deltaTypeBox.Text = deltaTypeBox.Items[0].ToString();

            foreach (var item in SqlHelpers.GetSqlServerNames())
                serverNameBox.Items.Add(item);

            if (serverNameBox.Items.Count > 0)
                serverNameBox.Text = serverNameBox.Items[0].ToString();

            dataSaveInterval.Minimum = 1;
            dataSaveInterval.Maximum = decimal.MaxValue;
           
            dataUpdateInterval.Minimum = 1;
            dataUpdateInterval.Maximum = decimal.MaxValue;

            loadValuesCount.Minimum = 5;
            loadValuesCount.Maximum = decimal.MaxValue;

            dataSaveInterval.Value = mWindow.Cfg.DataSaveInterval;
            dataUpdateInterval.Value = mWindow.Cfg.DataUpdateInterval;
            loadValuesCount.Value = mWindow.Cfg.LoadingValuesCount;

            dataSaveInterval.ValueChanged += dataSaveInterval_ValueChanged;
            dataUpdateInterval.ValueChanged += dataUpdateInterval_ValueChanged;
            loadValuesCount.ValueChanged += loadValuesCount_ValueChanged;

            startTimeTxt.Text = mWindow.Cfg.StartTime.ToString("g");
            stopTimeTxt.Text = mWindow.Cfg.StopTime.ToString("g");

            csvSeparator.Items.AddRange(new[] { ",", ".", ";", ":", "\\", "|" });
            csvSeparator.Text = mWindow.Cfg.CsvFormat.Separator.ToString();
                      
        }

        #region EVENT_HANDLERS

        void loadValuesCount_ValueChanged(object sender, EventArgs e)
        {
            mWindow.Cfg.LoadingValuesCount = (int)loadValuesCount.Value;
        }

        void dataUpdateInterval_ValueChanged(object sender, EventArgs e)
        {

            mWindow.Cfg.DataUpdateInterval = (int)dataUpdateInterval.Value;
        }

        void dataSaveInterval_ValueChanged(object sender, EventArgs e)
        {
            mWindow.Cfg.DataSaveInterval = (int)dataSaveInterval.Value;
        }

        #endregion

        private void AppStartWindow_Load(object sender, EventArgs e)
        {

        }

        private void priceIndexUpDown_ValueChanged(object sender, EventArgs e)
        {
            mWindow.Cfg.CsvFormat.PriceIndex = (int)priceIndexUpDown.Value - 1;
        }

        private void volumeIndexUpDown_ValueChanged(object sender, EventArgs e)
        {
            mWindow.Cfg.CsvFormat.VolumeIndex = (int)volumeIndexUpDown.Value - 1;
        }

        private void Start_Click(object sender, EventArgs e)
        {
            if (radioCSV.Checked)
            {
                progressBar2.MarqueeAnimationSpeed = 5;

                Task t = new Task(() =>
                {
                    InitCsvLoad();
                });
                t.Start();
            }
            if (radioSQL.Checked)
            {
                progressBar2.MarqueeAnimationSpeed = 5;

                Task t = new Task(() =>
                {
                    InitSqlConnection();
                });
                t.Start();
            }
        }

        private void ContainsHeaderCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            mWindow.Cfg.CsvFormat.ContainsHeader = ContainsHeaderCheckBox.Checked;
        }

        private void openDownloaderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Downloader DlForm = new Downloader();
            DlForm.Show();
        }

        private void deltaTypeBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (deltaTypeBox.Text)
            {
                case "Ratio":
                    mWindow.Cfg.DeltaType = Synthetics.DeltaType.Ratio;
                    explanationLabel.Text = "if r_value >= 0: y / x \nelse: log(y) * log(x)";
                    break;

                case "RatioIncludingBeta":
                    mWindow.Cfg.DeltaType = Synthetics.DeltaType.RatioIncludingBeta;
                    explanationLabel.Text = "if r_value >= 0: y / (beta * x) \nelse: log(y) * log(beta * x)";
                    break;

                case "Spread":
                    mWindow.Cfg.DeltaType = Synthetics.DeltaType.Spread;
                    explanationLabel.Text = "if r_value >= 0: y - x \nelse: y + x";
                    break;

                case "SpreadIncludingBeta":
                    mWindow.Cfg.DeltaType = Synthetics.DeltaType.SpreadIncludingBeta;
                    explanationLabel.Text = "if r_value >= 0: y - beta * x \nelse: y + beta * x";
                    break;
            }
        }

        public void InitCsvLoad()
        {
            if (mWindow.Cfg.CsvFormat.PriceIndex == mWindow.Cfg.CsvFormat.VolumeIndex)
            {
                MessageBox.Show("Price and Volume indeces should have a different values!");
                return;
            }

            IDataProvider provider = new CSVDataProvider("MarketData/", mWindow.Cfg.CsvFormat);

            mWindow.PairsContainer = new PairsContainer(provider, mWindow.Cfg.DeltaType);

            BeginInvoke(new Action(() => { this.Close(); }));
        }

        public void InitSqlConnection()
        {
            BeginInvoke(new Action(() =>
            {
                if (serverNameBox.Text == string.Empty)
                    MessageBox.Show("Server not found.");

                mWindow.Cfg.SqlConnectionString = string.Format("Data Source={0};Initial Catalog=PairTradingViewDb;Integrated Security=True;MultipleActiveResultSets=True;", serverNameBox.Text);

            }));

            IDataProvider provider = new SqlDataProvider(mWindow.Cfg);

            mWindow.PairsContainer = new PairsContainer(provider, mWindow.Cfg.DeltaType);
            mWindow.Tasks.SetDataUpdateInterval(mWindow.Cfg.DataUpdateInterval);
            mWindow.Tasks.SetDataSaveInterval(mWindow.Cfg.DataSaveInterval);

            BeginInvoke(new Action(() => { this.Close(); }));
        }

        private void startTimeTxt_TextChanged(object sender, EventArgs e)
        {
            TimeSpan ts;

            if (TimeSpan.TryParseExact(startTimeTxt.Text, "g", CultureInfo.CurrentCulture, out ts))
            {
                startTimeTxt.ForeColor = Color.LimeGreen;
            }
            else
            {
                startTimeTxt.ForeColor = Color.Red;
            }
        }

        private void stopTimeTxt_TextChanged(object sender, EventArgs e)
        {
            TimeSpan ts;

            if (TimeSpan.TryParseExact(stopTimeTxt.Text, "g", CultureInfo.CurrentCulture, out ts))
            {
                stopTimeTxt.ForeColor = Color.LimeGreen;
            }
            else
            {
                stopTimeTxt.ForeColor = Color.Red;
            }

        }

        private void csvSeparator_SelectedIndexChanged(object sender, EventArgs e)
        {
            mWindow.Cfg.CsvFormat.Separator = csvSeparator.Text[0];
        }

        private void CsvToDb_Click(object sender, EventArgs e)
        {
            if (serverNameBox.Text == string.Empty)
                MessageBox.Show("Server not found.");

            string connectionStr = string.Format("Data Source={0};Initial Catalog=PairTradingViewDb;Integrated Security=True;MultipleActiveResultSets=True;", serverNameBox.Text);

            Task t = new Task(() =>
            {
                LoadCsvToDb(connectionStr);
            });

            t.Start();
        }

        public void ProgressBarInc(int count)
        {
            if (progressBar1.Value == 100) progressBar1.Value = 0;

            double value = (1.0 / count * 100.0);

            if (progressBar1.Value + value <= 100)
                progressBar1.Value += (int)value;
        }

        public void LoadCsvToDb(string connectionStr)
        {         
            if (mWindow.Cfg.CsvFormat.PriceIndex == mWindow.Cfg.CsvFormat.VolumeIndex)
            {
                MessageBox.Show("Price and Volume indeces should have a different values!");
                return;
            }

            IDataProvider provider = new CSVDataProvider("MarketData/", mWindow.Cfg.CsvFormat);

            try
            {
                using (var db = new StocksContext(connectionStr))
                {

                    var stocksd = provider.GetStocks();

                    foreach (var item in stocksd)
                    {
                        BeginInvoke(new Action(() => { this.ProgressBarInc(stocksd.Count); }));

                        foreach (var d in item.History)
                        {
                            d.DateTime = DateTime.Now;
                        }

                        var stock = db.Stocks.Find(item.Code);

                        if (stock != null)
                        {
                            stock.History.AddRange(item.History);
                        }
                        else
                        {
                            db.Stocks.Add(item);
                        }

                        db.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                BeginInvoke(new Action(() => { this.progressBar1.Value = 0; }));
                MessageBox.Show(ex.Message);
            }
            finally
            {
                BeginInvoke(new Action(() => { this.progressBar1.Value = 100; }));
                MessageBox.Show("CSV Data loaded.");
            }

        }

    }
}
