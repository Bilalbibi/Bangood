using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using Jint;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace banggood.com_scraper.Models
{
    public static class GetProductDetails
    {
        public static Random rnd = new Random();
        public static HttpCaller HttpCaller = new HttpCaller();
        public static string Script = File.ReadAllText("g.js");
        public static MainForm mainform { get; set; }
        public static async Task ProductsList()
        {
            List<string> urls = new List<string>();
            try
            {
                urls = File.ReadAllLines("all products.txt").ToList();
            }
            catch (Exception e)
            {

                mainform.ErrorLog(e.ToString()); return;
            }
            #region delete removed products from DB Region
            var localListingsResp = await Utility.GetUrls().ConfigureAwait(false);
            if (localListingsResp.error != null) { mainform.ErrorLog(localListingsResp.error); return; }
            Console.WriteLine(localListingsResp.urls.Count);
            if (localListingsResp.urls.Count > 0)
            {
                var collectedUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var url in urls)
                    collectedUrls.Add(url);
                var delteBatch = 0;
                var q = new StringBuilder("DELETE FROM products WHERE ProductUrl in (");
                foreach (var url in localListingsResp.urls)
                {
                    if (!collectedUrls.Contains(url))
                    {
                        delteBatch++;
                        q.Append($"'{MySqlHelper.EscapeString(url)}',");
                    }
                    if (delteBatch == 50)
                    {
                        q.Length--;
                        q.Append(");");
                        var respDb = await Utility.ExecuteBatch($"{q}").ConfigureAwait(false);
                        if (respDb != null)
                            mainform.ErrorLog(respDb);
                        q.Clear();
                        delteBatch = 0;
                    }
                }
                if (delteBatch > 0)
                {
                    q.Length--;
                    q.Append(");");
                    var respDb = await Utility.ExecuteBatch($"{q}").ConfigureAwait(false);
                    if (respDb != null)
                        mainform.ErrorLog(respDb);
                }
            }
            #endregion
            var products = new List<Product>();
            var tpl = new TransformBlock<string, (Product product, string error)>
               (async x => await GetDetails(x).ConfigureAwait(false),
               new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 20 });

            foreach (string url in urls)
            {
                tpl.Post(url);
            }

            var nbr = 0;
            var query = new StringBuilder();
            var batch = 0;
            for (var i = 0; i < urls.Count; i++)
            {
                var res = await tpl.ReceiveAsync().ConfigureAwait(false);
                nbr++;
                mainform.Display($"collected {nbr} / {urls.Count} product");
                if (res.error != null)
                {
                    mainform.ErrorLog(res.error);
                    continue;
                }
                var sb = new StringBuilder("insert into products (");
                var sb2 = new StringBuilder("");
                foreach (var prop in typeof(Product).GetProperties())
                {
                    sb.Append($"{prop.Name},");
                    sb2.Append($"'{MySqlHelper.EscapeString((string)prop.GetValue(res.product, null))}',");
                }
                sb.Remove(sb.Length - 1, 1);
                sb2.Remove(sb2.Length - 1, 1);
                sb.Append($") values ({sb2}) ;\r\n");
                query.Append(sb);

                if (batch == 50)
                {
                    var r = await Utility.ExecuteBatch($"{query}").ConfigureAwait(false);
                    if (r != null)
                    {
                        mainform.ErrorLog($"Error inserting into db {r}");
                        Console.WriteLine(query);
                        Application.Exit();
                    }
                    query.Clear();
                    batch = 0;
                }
                batch++;
            }
            if (batch > 0)
            {
                var r = await Utility.ExecuteBatch($"{query}").ConfigureAwait(false);
                if (r != null)
                {
                    mainform.ErrorLog($"Error inserting into db {r}");
                    Console.WriteLine(query);
                    Application.Exit();
                }
            }
        }

        public static async Task<(Product product, string error)> GetDetails(string url)
        {
            var product = new Product();
            try
            {
                var specs = new List<string>();
                var res = await HttpCaller.GetDoc(url);
                if (res.error != null) return (null, res.error);
                //HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                //doc.LoadHtml(File.ReadAllText("products/" + file + ".html"));
                //(HtmlAgilityPack.HtmlDocument doc, string error) res = (doc, null);
                //res.doc.Save(file + ".html");
                var name = res.doc.DocumentNode?.SelectSingleNode("//strong[@class='title_strong']")?.InnerText.Trim();
                if (name == null) return (null, "there is no product");

                var imagesSrc = res.doc.DocumentNode?.SelectNodes("//ul[@class='wrapper']/li/img");
                var images = "";
                if (imagesSrc == null)
                    images = "N/A";

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
                string Key = "";
                string valueText = "";
                var specificationsJs = "";
                var wareHouse = res.doc.DocumentNode.SelectSingleNode("//a[@data-house]").GetAttributeValue("data-house]", "").Trim();
                var productsId = res.doc.DocumentNode.SelectSingleNode("//input[@id='products_id']").GetAttributeValue("value", "").Trim();
                var JSEngine = new Engine();
                string script = File.ReadAllText("g.js");
                var engine = new Engine().Execute(script);
                var sq = engine.Invoke("encrypt", $"products_id={productsId}&warehouse={wareHouse}").AsString();
                var json = await HttpCaller.GetHtml("https://www.banggood.com/load/product/ajaxProduct.html?sq=" + sq);
                var dictionaryPrice = new Dictionary<string, PriceInfo>();
                var objetc = JObject.Parse(json.html);
                var price = "US$" + (double)objetc.SelectToken("final_price");
                var shippingPrice = (string)objetc.SelectToken("defaultShip.shipCost");

                #region variations Region
                var variations = new Dictionary<string, string>();
                var details = res.doc.DocumentNode.SelectNodes("//div[@class='item_warehouse']/following-sibling::div");
                var variatonsJs = "";
                if (details != null)
                {
                    if (!objetc.SelectToken("valueIds").HasValues)
                    {
                        variatonsJs = "No varations for this product";
                    }
                    else
                    {
                        var valueIds = ((JArray)objetc.SelectToken("valueIds"))?.ToList();

                        foreach (var detail in details)
                        {

                            Key = detail?.SelectSingleNode(".//span")?.InnerText.Trim(); //?? detail?.SelectSingleNode(".//em")?.InnerText.Trim()
                            if (string.IsNullOrEmpty(Key))
                                break;
                            Key = Key.Replace(":", "").Trim();
                            //Console.WriteLine("key: "+ Key);
                            if (!variations.Keys.Contains(Key))
                            {
                                variations.Add(Key, "");
                            }
                            if (detail.SelectNodes(".//ul[@class='clearfix']/li") == null)
                                continue;
                            foreach (var item in detail.SelectNodes(".//ul[@class='clearfix']/li"))
                            {
                                var valueId = item.SelectSingleNode("./a").GetAttributeValue("value_id", "").Trim();
                                if (valueIds.Contains(valueId))
                                {
                                    valueText = item?.GetAttributeValue("data-old-name", "");
                                    if (string.IsNullOrEmpty(valueText)) valueText = item?.GetAttributeValue("data-large", "");
                                    //Console.WriteLine(key + " ====> " + value);
                                    variations[Key] = valueText;
                                    break;
                                }
                            }
                            if (variations.Count == valueIds.Count) break;
                        }
                        variatonsJs = JsonConvert.SerializeObject(variations, Formatting.Indented);
                    }
                }
                #endregion
                #region Prices And shipping Prices Region
                var countries = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText("contries.txt"));
                foreach (var country in countries)
                {
                    json = await HttpCaller.GetHtml1(country.Value + "/load/product/ajaxProduct.html?sq=" + sq);
                    objetc = JObject.Parse(json.html);
                    price = "US$" + (double)objetc.SelectToken("final_price");
                    shippingPrice = (string)objetc.SelectToken("defaultShip.shipCost");
                    dictionaryPrice.Add(country.Key, new PriceInfo { FinalPrice = price, ShippingPrice = shippingPrice });
                }
                var PriceShippingPriceInfo = JsonConvert.SerializeObject(dictionaryPrice, Formatting.Indented);
                #endregion
                #region Specifications Region
                var desc = res.doc.DocumentNode?.SelectSingleNode("//div[@class='box good_tabs_box jsPolytypeContWrap']").InnerText.Trim();
                var specifications = new Dictionary<string, string>();
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
                    //Console.WriteLine("We have a specific table");
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
                            {
                                Console.WriteLine("damn, something new");
                                Console.WriteLine(url);
                            }
                            else
                            {
                                var keyValue1 = key.Split(':');
                                var keyValue2 = value.Split(':');
                                //Console.WriteLine(keyValue1[0] + " === " + keyValue1[1]);
                                //Console.WriteLine(keyValue2[0] + " === " + keyValue2[1]);
                                specifications.Add(keyValue1[0], "");
                                specifications[keyValue1[0]] = keyValue1[1];
                                specifications.Add(keyValue2[0], "");
                                specifications[keyValue2[0]] = keyValue2[1];
                            }
                        }
                        else
                        {
                            specifications.Add(key, "");
                            specifications[key] = value;
                            //Console.WriteLine(key + " => => " + value);
                        }
                    }
                }
                if (specifications.Count == 0 && desc.ToLower().Contains("specification"))
                {
                    var foundSpecifics = false;
                    foreach (var line in desc.Split('\n'))
                    {

                        var s = line.Trim();
                        if (s.Equals("")) continue;
                        if (s.ToLower().StartsWith("specification"))
                        {
                            Console.WriteLine("Found a specification text");
                            foundSpecifics = true; continue;
                        }

                        if (foundSpecifics)
                        {
                            if (s.Contains(":"))
                            {
                                var x = s.Split(':');
                                var key = x[0].Trim();
                                var value = x[1].Trim();
                                if (value.Equals("")) continue;
                                specifications.Add(key, "");
                                specifications[key] = value;
                            }
                            else
                                foundSpecifics = false;
                        }
                    }
                }
                if (specifications.Count > 0)
                    specificationsJs = JsonConvert.SerializeObject(specifications, Formatting.Indented);
                else
                    specificationsJs = "";
                #endregion
                #region description, package included and features Region
                string description = "";
                string packageIncluded = "";
                string features = "";
                var productDetails = res.doc.DocumentNode.SelectSingleNode("//div[@aria-cont='productdetails']")?.InnerHtml;
                var depart = productDetails.IndexOf("Description");
                var thirt = productDetails.IndexOf("Features");
                var second = productDetails.IndexOf("Package");
                if (depart == -1)
                {
                    depart = productDetails.IndexOf("DESCRIPTION");
                }
                if (thirt > 0)
                {
                    var productDetails1 = productDetails.Substring(thirt);
                    var hb = productDetails1.IndexOf(@"<strong>");
                    var descripti = productDetails1.Substring("Features:".Length, hb);
                    features = HtmlToPlainText(descripti);
                }
                if (second > 0)
                {
                    var productDetails1 = productDetails.Substring(second);
                    //var hb = productDetails1.IndexOf("</p>");
                    var descripti = productDetails1;
                    packageIncluded = HtmlToPlainText(descripti);
                }
                if (depart > 0)
                {
                    var productDetails1 = productDetails.Substring(depart);
                    var hb = productDetails1.IndexOf("</p>");
                    if (hb == -1)
                    {
                        hb = productDetails1.IndexOf("</table>");
                        if (hb == -1)
                        {
                            hb = productDetails1.IndexOf("Specification") - ("Specification").Length;
                        }
                    }
                    var dep = productDetails1.IndexOf("DESCRIPTIONS");
                    if (dep == -1)
                    {
                        dep = productDetails1.IndexOf("Description");
                    }
                    var descripti = productDetails1.Substring(12, hb);
                    description = HtmlToPlainText(descripti).Trim();


                }
                #endregion

                Console.WriteLine("****************************************");
                Console.WriteLine("description: " + description);
                Console.WriteLine("****************************************");
                Console.WriteLine("features: " + features);
                Console.WriteLine("****************************************");
                Console.WriteLine("packageIncluded: " + packageIncluded);
                Console.WriteLine("****************************************");
                Console.WriteLine("specifications: " + specificationsJs);
                Console.WriteLine("****************************************");
                Console.WriteLine("variatonsJs: " + variatonsJs);
                Console.WriteLine("****************************************");
                Console.WriteLine("PriceShippingPriceInfo: " + PriceShippingPriceInfo);

                product.DescriptionInfo = description;
                product.Features = features;
                product.PackageIncluded = packageIncluded;
                product.ProductUrl = url;
                product.Title = name;
                product.PriceInfos = PriceShippingPriceInfo;
                product.Images = images;
                product.Specifications = specificationsJs;
                product.ProductDetails = variatonsJs;
            }
            catch (Exception ex)
            {
                Console.WriteLine(url);
                Console.WriteLine(ex);
                return (null, "N/A");
            }
            return (product, null);
        }
        public static string HtmlToPlainText(string html)
        {
            const string tagWhiteSpace = @"(>|$)(\W|\n|\r)+<";//matches one or more (white space or line breaks) between '>' and '<'
            const string stripFormatting = @"<[^>]*(>|$)";//match any character between '<' and '>', even when end tag is missing
            const string lineBreak = @"<(br|BR)\s{0,1}\/{0,1}>";//matches: <br>,<br/>,<br />,<BR>,<BR/>,<BR />
            var lineBreakRegex = new Regex(lineBreak, RegexOptions.Multiline);
            var stripFormattingRegex = new Regex(stripFormatting, RegexOptions.Multiline);
            var tagWhiteSpaceRegex = new Regex(tagWhiteSpace, RegexOptions.Multiline);

            html = System.Net.WebUtility.HtmlDecode(html);

            html = tagWhiteSpaceRegex.Replace(html, "><");

            html = lineBreakRegex.Replace(html, Environment.NewLine);

            html = stripFormattingRegex.Replace(html, string.Empty);

            return html;
        }
    }
}
