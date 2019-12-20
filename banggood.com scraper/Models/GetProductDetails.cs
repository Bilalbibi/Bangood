using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows.Forms;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using OpenQA.Selenium.Chrome;
using Newtonsoft.Json.Linq;

namespace banggood.com_scraper.Models
{
    public static class GetProductDetails
    {
        public static Random rnd = new Random();
        public static HttpCaller HttpCaller = new HttpCaller();
        //public static ChromeDriver driver = new ChromeDriver();
        public static MainForm mainform { get; set; }
        public static async Task ProductsList()
        {
            string fileName = "x.html";
            FileInfo f = new FileInfo(fileName);
            var fullname = f.FullName;
            ChromeDriverService service = ChromeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;
            var options = new ChromeOptions();
            options.AddArgument("--window-position=-32000,-32000");
            var driver = new ChromeDriver(service, options);
            driver.Navigate().GoToUrl(fullname);

            var urls = File.ReadAllLines("all products.txt").ToList();
            var products = new List<Product>();
            var tpl = new TransformBlock<(string url, int file), (Product product, string error)>
               (async x => await GetDetails(x.url, x.file).ConfigureAwait(false),
               new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1 });

            for (int i = 200; i < 400; i++)
            {
                string url = urls[rnd.Next(0, urls.Count)];
                tpl.Post((url, i));
            }

            var nbr = 0;
            var query = new StringBuilder();
            var batch = 0;
            for (var i = 0; i < 200; i++)
            {
                var res = await tpl.ReceiveAsync().ConfigureAwait(false);
                nbr++;
                mainform.Display($"collected {nbr} / {urls.Count} listing");
                if (res.error != null)
                {
                    mainform.ErrorLog(res.error);
                    continue;
                }
                //var sb = new StringBuilder("insert into products (");
                //var sb2 = new StringBuilder("");
                //foreach (var prop in typeof(Product).GetProperties())
                //{
                //    sb.Append($"{prop.Name},");
                //    sb2.Append($"'{MySqlHelper.EscapeString((string)prop.GetValue(res.product, null))}',");
                //}
                //sb.Remove(sb.Length - 1, 1);
                //sb2.Remove(sb2.Length - 1, 1);
                //sb.Append($") values ({sb2}) ;\r\n");
                //query.Append(sb);

                //if (batch==50)
                //{
                //    var r = await Utility.ExecuteBatch($"{query}").ConfigureAwait(false);
                //    if (r != null)
                //    {
                //        mainform.ErrorLog($"Error inserting into db {r}");
                //        return;
                //    }
                //    query.Clear();
                //    batch = 0;
                //}
                //batch++;
            }
        }

        public static async Task<(Product product, string error)> GetDetails(string url, int file)
        {
            Console.WriteLine(url);
            var product = new Product();
            try
            {
                var specs = new List<string>();
                var res = await HttpCaller.GetDoc(url);
                if (res.error != null) return (null, res.error);
                //HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                //doc.LoadHtml(File.ReadAllText("products/" + file + ".html"));
                //(HtmlAgilityPack.HtmlDocument doc, string error) res = (doc, null);
                // res.doc.Save(file + ".html");
                var name = res.doc.DocumentNode?.SelectSingleNode("//strong[@class='title_strong']")?.InnerText.Trim();
                if (name == null)
                {
                    return (null, "item not found");
                }
                res = await HttpCaller.GetDoc(url);
                var WHouse = res.doc.DocumentNode.SelectSingleNode("//a[@data-house]").GetAttributeValue("data-house]", "").Trim();
                var products_id = res.doc.DocumentNode.SelectSingleNode("//input[@id='products_id']").GetAttributeValue("value", "").Trim();
                ChromeDriverService service = ChromeDriverService.CreateDefaultService();
                service.HideCommandPromptWindow = true;
                var options = new ChromeOptions();
                options.AddArgument("--window-position=-32000,-32000");
                var driver = new ChromeDriver(service, options);
                string fileName = "x.html";
                FileInfo f = new FileInfo(fileName);
                var fullname = f.FullName;
                driver.Navigate().GoToUrl(fullname);
                var sqParameter = (string)driver.ExecuteScript("return encrypt(\"products_id=" + products_id + "&warehouse=" + WHouse + "\");");
                var json = await HttpCaller.GetHtml("https://www.banggood.com/load/product/ajaxProduct.html?sq=" + sqParameter);
                if (json.error != null) { mainform.ErrorLog(json.error); return (null, json.error); }
                driver.Quit();
                var objetc = JObject.Parse(json.html);
                //var price = res.doc.DocumentNode?.SelectSingleNode("//meta[@name='description']")?.GetAttributeValue("content", "");
                var price = "US$" + (double)objetc.SelectToken("final_price");
                var shippingPrice = (string)objetc.SelectToken("defaultShip.shipCost");
                Console.WriteLine("price: " + price);
                Console.WriteLine("shippingPrice: " + shippingPrice);
                return (null, null);
                var imagesSrc = res.doc.DocumentNode?.SelectNodes("//ul[@class='wrapper']/li/img");
                var images = "";
                if (imagesSrc == null)
                {
                    images = "NO images for this item";
                }
                else
                {
                    var list = new List<string>();
                    foreach (var imageSrc in imagesSrc)
                    {
                        list.Add(imageSrc.GetAttributeValue("src", ""));
                    }
                    images = string.Join("\r\n", list);
                }
                var detailsNodes = res.doc.DocumentNode?.SelectNodes("//div[@class='item_warehouse']/following-sibling::div");
                var details = "";
                foreach (var detailsNode in detailsNodes)
                {
                    if (detailsNode.InnerText.Contains("Shipping"))
                    {
                        break;
                    }
                    details = details + detailsNode.OuterHtml;
                }
                var desc = res.doc.DocumentNode?.SelectSingleNode("//div[@class='box good_tabs_box jsPolytypeContWrap']").InnerText.Trim();
                var specifications = new List<KeyValuePair<string, string>>();
                var specificTable = res.doc.DocumentNode.SelectSingleNode("//span[text()='Specification:']/../../..//../following-sibling::table");
                if (specificTable == null) specificTable = res.doc.DocumentNode.SelectSingleNode("//strong[text()='Specifications:']/following-sibling::*/following-sibling::table");
                if (specificTable == null)
                {
                    var potentialTables = res.doc.DocumentNode.SelectNodes("//div[@id='jspromsgwrap']//table");
                    if (potentialTables != null)
                    {
                        foreach (var potentialTable in potentialTables)
                        {
                            var tds = potentialTable.SelectNodes(".//tr[last()]//td");
                            if (tds == null) continue;
                            if (tds.Count == 2)
                            {
                                specificTable = tds[0].SelectSingleNode("./../..");
                                break;
                            }
                        }
                    }

                }


                if (specificTable != null)
                {
                    Console.WriteLine("We have a specific table");
                    var trs = specificTable.SelectNodes(".//tr");
                    bool keyValueCell = false;
                    for (var i = 0; i < trs.Count; i++)
                    {
                        var tr = trs[i];
                        var tds = tr.SelectNodes(".//td");
                        if (tds == null || tds.Count != 2) continue;
                        var key = tds[0].InnerText.Trim();
                        var value = tds[1].InnerText.Trim();
                        if (key.Equals("") || value.Equals("")) continue;
                        if (i == 0 && key.Contains(":") && !key.Split(':')[1].Trim().Equals(""))
                            keyValueCell = true;
                        if (keyValueCell)
                        {
                            if (!key.Contains(":") || !value.Contains(":"))
                                Console.WriteLine("damn, something new");
                            else
                            {
                                var keyValue1 = key.Split(':');
                                var keyValue2 = value.Split(':');
                                Console.WriteLine(keyValue1[0] + " === " + keyValue1[1]);
                                Console.WriteLine(keyValue2[0] + " === " + keyValue2[1]);
                                specifications.Add(new KeyValuePair<string, string>(keyValue1[0], keyValue1[1]));
                                specifications.Add(new KeyValuePair<string, string>(keyValue2[0], keyValue2[1]));
                            }
                        }
                        else
                        {
                            specifications.Add(new KeyValuePair<string, string>(key, value));
                            Console.WriteLine(key + " => => " + value);
                        }
                    }
                }
                if (specifications.Count == 0)
                {
                    //res.doc.Save("new format.html");
                    var tries = 0;
                    var foundSpecifics = false;
                    foreach (var line in desc.Split('\n'))
                    {
                        var s = line.Trim();
                        Console.WriteLine(s);
                        if (s.Equals("")) continue;
                        if (!foundSpecifics)
                        {
                            if (s.ToLower().StartsWith("specification"))
                            {
                                Console.WriteLine("Found a specification text");
                                foundSpecifics = true;
                                continue;
                            }
                        }

                        if (foundSpecifics)
                        {
                            if (s.Contains(":"))
                            {
                                tries++;
                                var x = s.Split(':');
                                var key = x[0].Trim();
                                var value = x[1].Trim();
                                if (value.Equals("")) continue;
                                Console.WriteLine(key + " => " + value);
                                specifications.Add(new KeyValuePair<string, string>(key, value));
                            }
                            else
                                foundSpecifics = false;
                        }
                    }

                }
                product.product_url = url;
                product.Title = name;
                product.Price = price;
                product.Images = images;
                product.Specifications = "";
                product.Product_Details = details;
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex);
                Application.Exit();
            }
            return (product, null);
        }
    }
}
