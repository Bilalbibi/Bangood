using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows.Forms;

namespace banggood.com_scraper.Models
{
    public static class GetProductDetails
    {
        public static HttpCaller HttpCaller = new HttpCaller();
        public static MainForm mainform { get; set; }
        public static async Task ProductsList()
        {
            var urls = File.ReadAllLines("all products.txt").ToList();
            var products = new List<Product>();
            var tpl = new TransformBlock<(string url, int file), (Product product, string error)>
               (async x => await GetDetails(x.url, x.file).ConfigureAwait(false),
               new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1 });

            for (int i = 0; i < urls.Count; i++)
            {
                string url = urls[i];
                tpl.Post((url, i));
            }

            var nbr = 0;
            var query = new StringBuilder();
            var batch = 0;
            foreach (var url in urls)
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

        private static async Task<(Product product, string error)> GetDetails(string url, int file)
        {
            var product = new Product();
            try
            {
                var specs = new List<string>();
                //var res = await HttpCaller.GetDoc(url);
                //if (res.error != null) return (null, res.error);
                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(File.ReadAllText("products/" + file + ".html"));
                (HtmlAgilityPack.HtmlDocument doc,string error) res = (doc, null);

                var name = res.doc.DocumentNode?.SelectSingleNode("//strong[@class='title_strong']")?.InnerText.Trim();
                if (name == null)
                {
                    return (null, "item not found");
                }
                var price = res.doc.DocumentNode?.SelectSingleNode("//meta[@name='description']")?.GetAttributeValue("content", "");
                if (price.Contains("Only "))
                {
                    price = price.Replace("Only ", "");
                    price = price.Substring(0, price.IndexOf(','));
                }
                else
                {
                    price = res.doc.DocumentNode?.SelectSingleNode("/html/head/title")?.InnerText.Trim();
                    var title = price.Split('-');
                    price = title[1];
                }

                var imagesSrc = res.doc.DocumentNode?.SelectNodes("//ul[@class='wrapper']/li/img");
                var images = "";
                if (imagesSrc == null)
                {
                    images = "N/A";
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
                foreach (var line in desc.Split('\n'))
                {
                    var s = line.Trim();
                    if(s.ToLower().Equals(""))
                }

                Console.WriteLine(url);
                Console.WriteLine(specifications);

                product.product_url = url;
                product.Title = name;
                product.Price = price;
                product.Images = images;
                product.Specifications = specifications;
                product.Product_Details = details;
            }
            catch (Exception)
            {

                Console.WriteLine(url);
                Application.Exit();
            }
            return (product, null);
        }
    }
}
