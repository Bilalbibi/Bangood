using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace banggood.com_scraper.Models
{
    static class Get_All_Product
    {
        public static HttpCaller HttpCaller = new HttpCaller();
        public static MainForm mainform;
        public static async Task Get_Products()
        {
            SpeechSynthesizer speak = new SpeechSynthesizer();
            speak.Speak("start scraping");
            var allCategoriesUrl = new List<string>();
            (HtmlAgilityPack.HtmlDocument doc, string error) res;
            if (mainform.CategoriesSelector.Enabled==false)
            {
                 res = await HttpCaller.GetDoc("https://www.banggood.com/");
                if (res.error != null) { mainform.ErrorLog(res.error); return; }
                var categoriesUrl = res.doc.DocumentNode.SelectNodes("//div[@data-id]/following-sibling::div/div//dt/following-sibling::dd/a");
                foreach (var url in categoriesUrl)
                {
                    allCategoriesUrl.Add(url.GetAttributeValue("href", "").Trim());
                }
            }
            else
            {
               
                foreach (var item in mainform.CategoriesSelector.CheckedItems)
                {
                    if (mainform.dictionary.Keys.Contains((string)item))
                    {
                        allCategoriesUrl.AddRange(mainform.dictionary[(string)item]);
                    }
                }
            }
            Console.WriteLine(allCategoriesUrl.Count);
            return;
            var productsUrl = new List<string>();
            var tpl = new TransformBlock<string, (List<string> urls, string error)>
               (async x => await GetAllProductAsync(x).ConfigureAwait(false),
               new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 20 });

            foreach (var UrlCatgory in allCategoriesUrl)
                tpl.Post(UrlCatgory);
            var nbr = 0;
            foreach (var UrlCatgory in allCategoriesUrl)
            {
                var resp = await tpl.ReceiveAsync().ConfigureAwait(false);
                nbr++;
                mainform.Display($"collected {nbr} / {allCategoriesUrl.Count} listing");
                if (resp.error != null)
                    continue;
                productsUrl.AddRange(resp.urls);
                Console.WriteLine(productsUrl.Count);
            }
            productsUrl = productsUrl.Distinct().ToList();
            File.WriteAllLines("all products.txt", productsUrl);
            speak.Speak("done");
        }
        static async Task<(List<string> urls, string error)> GetAllProductAsync(string url)
        {

            var itemsUrlOfcategory = new List<string>();
            try
            {
                do
                {
                    var res = await HttpCaller.GetDoc(url);
                    if (res.error != null)
                        return (null, res.error);

                    var items = res.doc.DocumentNode?.SelectNodes("//li[@data-products-id]/div/span[1]/a") ?? res.doc.DocumentNode?.SelectNodes("//li[@data-pid]/div/span[@class='img']/a")
                        ?? res.doc.DocumentNode?.SelectNodes("//li[@data-product-id]/span[1]/a[1]");
                    if (items == null)
                    {
                        int index = url.IndexOf("page");
                        if (index == -1)
                        {
                            //Console.WriteLine(url);
                            return (null, "no result");
                        }
                        var p = url.Substring(index).Replace(".html", "").Replace("page", "");
                        url = url.Replace(p, int.Parse(p) + 1 + "");
                        //Console.WriteLine("next url" + url);
                        continue;
                    }
                    var urlp = "";
                    foreach (var item in items)
                    {
                        urlp = item.GetAttributeValue("href", "");
                        if (urlp.Contains("similarId"))
                            continue;
                        urlp = urlp.Substring(0, urlp.IndexOf("html") + 4);
                        itemsUrlOfcategory.Add(item.GetAttributeValue("href", ""));
                    }
                    url = res.doc.DocumentNode.SelectSingleNode("//a[contains(@class,'nextPage')]")?.GetAttributeValue("href", "") ?? "https://www.banggood.com/" + res.doc.DocumentNode.SelectSingleNode("//a[@class='next']")?.GetAttributeValue("href", "");
                    if (url == null || url == "https://www.banggood.com/")
                        break;
                } while (true);
            }
            catch (Exception)
            {
                var synthesizer = new SpeechSynthesizer();
                synthesizer.Speak("error");
                //Console.WriteLine(url);
            }
            return (itemsUrlOfcategory, null);
        }

    }
}
