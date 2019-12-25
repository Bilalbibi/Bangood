using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MetroFramework.Forms;
using Newtonsoft.Json.Linq;
using banggood.com_scraper.Models;
using Jint;

namespace banggood.com_scraper
{
    public partial class MainForm : MetroForm
    {
        public bool LogToUi = true;
        public bool LogToFile = true;
        Random rnd = new Random();
        private readonly string _path = Application.StartupPath;
        public HttpCaller HttpCaller = new HttpCaller();
        public Dictionary<string, List<string>> dictionary = new Dictionary<string, List<string>>();
        public string Script = File.ReadAllText("g.js");
        public MainForm()
        {
            InitializeComponent();
        }

        int delayMin, delayMax;
        string user, pass;

        private async Task MainWork()
        {
            await Task.Delay(3000);
        }

        private async void Form1_LoadAsync(object sender, EventArgs e)
        {
            ServicePointManager.DefaultConnectionLimit = 65000;
            Directory.CreateDirectory("data");
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Utility.CreateDb();
            //Utility.LoadConfig();
            //var x=   await Utility.ExecuteBatch("create table IF NOT EXISTS `products` (`id` INT(11) NOT NULL AUTO_INCREMENT,`Product_url` VARCHAR(255) NOT NULL,`Title` VARCHAR(255) NOT NULL,`ProductDetails` long NULL,`Price` VARCHAR(255) NOT NULL,`Images` TEXT NOT NULL,PRIMARY KEY(id),`Specifications` long NOT NULL,UNIQUE(ProductUrl);").ConfigureAwait(false);
            //   if (x!=null)
            //   {
            //       Console.WriteLine(x);
            //   }
            //   return;
            Utility.InitCntrl(this);
            var res = await HttpCaller.GetDoc("https://www.banggood.com/");
            if (res.error != null) { ErrorLog(res.error); return; }
            var categories = res.doc.DocumentNode?.SelectNodes("//li[@class='cate-item']/div[@class='cate-title']");
            if (categories == null) { ErrorLog("there is no categories to scrape for now"); return; }
            foreach (var category in categories)
            {
                var key = category.InnerText.Trim();
                dictionary.Add(key, null);
                var subCategories = category.SelectNodes("./following-sibling::div//dd/a");
                var urls = new List<string>();
                foreach (var subCategory in subCategories)
                {
                    urls.Add(subCategory.GetAttributeValue("href", ""));
                }
                dictionary[key] = urls;
            }
            foreach (var dictionar in dictionary.Keys)
            {
                CategoriesSelector.Items.Add(dictionar);
            }
        }

        static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.ToString(), @"Unhandled Thread Exception");
        }
        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show((e.ExceptionObject as Exception)?.ToString(), @"Unhandled UI Exception");
        }
        #region UIFunctions
        public delegate void WriteToLogD(string s, Color c);
        public void WriteToLog(string s, Color c)
        {
            try
            {
                if (InvokeRequired)
                {
                    Invoke(new WriteToLogD(WriteToLog), s, c);
                    return;
                }
                if (LogToUi)
                {
                    if (DebugT.Lines.Length > 5000)
                    {
                        DebugT.Text = "";
                    }
                    DebugT.SelectionStart = DebugT.Text.Length;
                    DebugT.SelectionColor = c;
                    DebugT.AppendText(DateTime.Now.ToString(Utility.SimpleDateFormat) + " : " + s + Environment.NewLine);
                }
                Console.WriteLine(DateTime.Now.ToString(Utility.SimpleDateFormat) + @" : " + s);
                if (LogToFile)
                {
                    File.AppendAllText(_path + "/data/log.txt", DateTime.Now.ToString(Utility.SimpleDateFormat) + @" : " + s + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        public void NormalLog(string s)
        {
            WriteToLog(s, Color.Black);
        }
        public void ErrorLog(string s)
        {
            WriteToLog(s, Color.Red);
        }
        public void SuccessLog(string s)
        {
            WriteToLog(s, Color.Green);
        }
        public void CommandLog(string s)
        {
            WriteToLog(s, Color.Blue);
        }

        public delegate void SetProgressD(int x);
        public void SetProgress(int x)
        {
            if (InvokeRequired)
            {
                Invoke(new SetProgressD(SetProgress), x);
                return;
            }
            if ((x <= 100))
            {
                ProgressB.Value = x;
            }
        }
        public delegate void DisplayD(string s);
        public void Display(string s)
        {
            if (InvokeRequired)
            {
                Invoke(new DisplayD(Display), s);
                return;
            }
            displayT.Text = s;
        }

        #endregion
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Utility.Config = new Dictionary<string, string>();
            Utility.SaveCntrl(this);
            Utility.SaveConfig();
        }
        private async void startB_Click(object sender, EventArgs e)
        {
            await MainWork();

        }
        private void loadInputB_Click_1(object sender, EventArgs e)
        {
            OpenFileDialog o = new OpenFileDialog { Filter = @"xlsx|*.xlsx", InitialDirectory = _path };
            if (o.ShowDialog() == DialogResult.OK)
            {
            }
        }
        private void openInputB_Click_1(object sender, EventArgs e)
        {
            try
            {
            }
            catch (Exception ex)
            {
                ErrorLog(ex.ToString());
            }
        }
        private void openOutputB_Click_1(object sender, EventArgs e)
        {
            try
            {
            }
            catch (Exception ex)
            {
                ErrorLog(ex.ToString());
            }
        }

        private async void startB_Click_1Async(object sender, EventArgs e)
        {
            if (CategoriesSelector.CheckedItems.Count == 0)
            {
                Display("please select one category at least");
                return;
            }
            startB.Enabled = false;
            Get_All_Product.mainform = this;
            await Get_All_Product.Get_Products();
            //GetProductDetails.mainform = this;
            //await GetProductDetails.GetDetails("https://www.banggood.com/Women-Special-Colorful-DIY-Lamb-Hair-Bag-Crossbody-Bag-For-Daily-Outdoor-p-1596479.html?rmmds=category&ID=6157241&cur_warehouse=CN");
            //await GetProductDetails.ProductsList();
            startB.Enabled = true;

        }

        private void AllCategories_CheckedChanged(object sender, EventArgs e)
        {
            if (AllCategories.Checked)
            {
                for (int i = 0; i < CategoriesSelector.Items.Count; i++)
                {
                    CategoriesSelector.SetItemChecked(i, true);
                }
            }
            else
            {
                for (int i = 0; i < CategoriesSelector.Items.Count; i++)
                {
                    CategoriesSelector.SetItemChecked(i, false);
                }
            }
        }

        private void loadOutputB_Click_1(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog
            {
                Filter = @"xlsx file|*.xlsx",
                Title = @"Select the output location"
            };
            saveFileDialog1.ShowDialog();
            if (saveFileDialog1.FileName != "")
            {
                //outputI.Text = saveFileDialog1.FileName;
            }
        }

        async Task GetShippingDetails(string id, string warehouse)
        {
            var engine = new Engine().Execute(Script);
            var sq = engine.Invoke("encrypt", $"products_id={id}&warehouse={warehouse}").AsString();
            Console.WriteLine(sq);
            var json = await HttpCaller.GetHtml("https://www.banggood.com/load/product/ajaxProduct.html?sq=" + sq);
            if (json.error != null) { ErrorLog(json.error); }
            var objetc = JObject.Parse(json.html);
            var price = "US$" + (double)objetc.SelectToken("final_price");
            var shippingPrice = (string)objetc.SelectToken("defaultShip.shipCost");
            var valueIds = ((JArray)objetc.SelectToken("valueIds")).ToList();
            foreach (var valueId in valueIds)
            {
                Console.WriteLine(valueId);
            }

        }
    }
}
