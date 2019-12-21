using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace banggood.com_scraper.Models
{
    public class HttpCaller
    {
        HttpClient _httpClient;
        HttpClient _httpClient1;
        public string proxy;
        readonly HttpClientHandler _httpClientHandler = new HttpClientHandler()
        {
            CookieContainer = new CookieContainer(),
            UseCookies = false,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            UseProxy = true,
        };
        public HttpCaller()
        {
            _httpClient = new HttpClient(_httpClientHandler);
            _httpClient1 = new HttpClient(_httpClientHandler);
            _httpClient.DefaultRequestHeaders.Add("cookie", "_ga=GA1.2.1507663406.1575663153; _gid=GA1.2.593741015.1575663153; __bgcookie=0|; cto_lwid=46d27783-2be1-464a-a3ef-bacba38084da; _gcl_au=1.1.734610738.1575663154; rec_uid=1170364846|1575663155; _ym_uid=1575539479192991994; _ym_d=1575663154; banggood_SID=3aaa35b5c44e2990600743633228c53c; _fbp=fb.1.1575663162325.1505967872; _bg_w_c=f979ef1f5e48551efbc44c5394376010; currency=USD; _bgLang=en-GB; _bgCK=04e4adf37ffcbe95bf074adf5cbd1d1b; f_webp_lossy=1; countryCookie=%7B%22code%22%3A%22TN%22%2C%22name%22%3A%22Tunisia%22%2C%22currency%22%3A%22USD%22%7D; banggoodSffix=com; _abck=8A0CB77749AAC060AA9552A1A3BA6EE4~0~YAAQsV6MT5ajhcxuAQAAOHfl3APfb2cqoiOZN4g7V8R4Fh9mliTPp2ISxgXBFtvFEjfLH5WdKaXmObAgtiv6o0GNdsaXqvs/E/G/mgpENs8FcHtQSHRieMqeH8biw2cMB1ezbup5/OPi8DzFR0YX+MRTaJ4JdRSMtmgE8EL25qgQXziyli7YcohLfZiHowE0g40JRJ6DZDveq2yd0MAJDGr1/Yww0/VRKNhQnbPIcymlDTCJYWuLH/HgV27CNSKQREOixpTlB4XlSHwkT4MGU+B9sDdTOaT179sQa7DIAGTzHXE3jKgRC66T3wTmaxhrXsI92NzleGmD~-1~-1~-1; _scid=f7f164eb-90f2-4fb0-8eb1-3182ed8e0571; pw_status_e924faaced168336f02f222c66d47f50a81954df26d071f4d30bfad270283120=deny; pw_deviceid=e1315e62-3dcb-44e1-8817-73e71c747317; _sctr=1|1575586800000; ab_footer_pay=2; searchHistory=a%3A1%3A%7Bi%3A0%3Bs%3A20%3A%22tops+and+pants+women%22%3B%7D; __bgresource=direct; _ym_isad=2; show_messenger=3; abversion=2; __bgqueue=1575788338459|direct|none|-|-|0|0|0|; __bguser=1575788338459|3205580058|3201480422|1575663154532; bm_sz=2AF6B7D3CEB590822E69956A2DBDE612~YAAQsV6MTzdKh8xuAQAAIABO5AYFqV7+KWKjXHTKot5Mdg1FwKjdLdERxIE7FbpBhpFt/4kBAUMTXebFf7NehEwlZsPz20izs18rHtYxP1lX33UKQNlxO8Zozo1k//B+eA2XuRLnC8AnU96oKNM0/GoBlq+G9R9rYTd8lwUS6dfiKrE9M7bUYFKWeVXS86mj6pc=; bg_email=undefined; COOKIE_ID=34; AKFWDDC=bS5JARNdKCvhm6zsc9YY2/AHikkXbxlw9WusXEJmHRA=; cookie_warehouse=CN; test_version=warehouseupdate02%2Crec2; index_un_login_pop=1; CategoryWare0c9ebb2ded806d7ffda75cd0b95eb70c=WyJ1c2EiLCJ1ayIsImF1Il0%3D; customer_view_products=a%3A30%3A%7Bi%3A0%3Bi%3A1156376%3Bi%3A1%3Bi%3A1568663%3Bi%3A2%3Bi%3A1561500%3Bi%3A3%3Bi%3A1594466%3Bi%3A4%3Bi%3A1166769%3Bi%3A5%3Bi%3A1407831%3Bi%3A6%3Bi%3A1308137%3Bi%3A7%3Bi%3A1016892%3Bi%3A8%3Bi%3A1469040%3Bi%3A9%3Bi%3A1218063%3Bi%3A10%3Bi%3A1261337%3Bi%3A11%3Bi%3A1299193%3Bi%3A12%3Bi%3A1128281%3Bi%3A13%3Bi%3A1298002%3Bi%3A14%3Bi%3A1449328%3Bi%3A15%3Bi%3A1259285%3Bi%3A16%3Bi%3A1482504%3Bi%3A17%3Bi%3A1527933%3Bi%3A18%3Bi%3A1370349%3Bi%3A19%3Bi%3A1479077%3Bi%3A20%3Bi%3A1462862%3Bi%3A21%3Bi%3A1378655%3Bi%3A22%3Bi%3A1326774%3Bi%3A23%3Bi%3A1220860%3Bi%3A24%3Bi%3A1053069%3Bi%3A25%3Bi%3A1349111%3Bi%3A26%3Bi%3A1391890%3Bi%3A27%3Bi%3A1359706%3Bi%3A28%3Bi%3A1296117%3Bi%3A29%3Bi%3A1361733%3B%7D; rec_sid=2792457586|1575791212; __bgvisit=1575791810237|direct|none|-|-|0|0|null; access_initDeals_times=1; featured_pids=%7B%22pid%22%3A%5B1440143%2C1571985%2C1259022%2C1507266%2C1016398%2C1063303%5D%7D; wcs_bt=s_125414200a53:1575791811; _gat=1; _derived_epik=dj0yJnU9dzhKbkR0N3JTbHY1d21IUXliNE9QWkJXSDRRdHY5LS0mbj10UEpVVGluSnFWWGVKbEZjUG5rOW93Jm09NyZ0PUFBQUFBRjNzck1V");
        }
        public async Task<(HtmlDocument doc, string error)> GetDoc(string url, int maxAttempts = 1)
        {
            var resp = await GetHtml(url, maxAttempts);
            if (resp.error != null) return (null, resp.error);
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(resp.html);
            return (doc, null);
        }
        public async Task<(string html, string error)> GetHtml(string url, int maxAttempts = 5)
        {
            int tries = 0;
            do
            {
                try
                {
                    var response = await _httpClient.GetAsync(url);
                    string html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
                    return (html, null);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    tries++;
                    if (tries == maxAttempts)
                    {
                        return (null, ex.ToString());
                    }
                    await Task.Delay(2000);
                }
            } while (true);
        }
        public async Task<(string html, string error)> GetHtml1(string url, int maxAttempts = 5)
        {
            int tries = 0;
            do
            {
                try
                {
                    var response = await _httpClient1.GetAsync(url);
                    string html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
                    return (html, null);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    tries++;
                    if (tries == maxAttempts)
                    {
                        return (null, ex.ToString());
                    }
                    await Task.Delay(2000);
                }
            } while (true);
        }
        public async Task<(string json, string error)> PostJson(string url, string json, int maxAttempts = 1)
        {
            int tries = 0;
            do
            {
                try
                {
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    // content.Headers.Add("x-appeagle-authentication", Token);
                    var r = await _httpClient.PostAsync(url, content);
                    var s = await r.Content.ReadAsStringAsync();
                    return (s, null);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    tries++;
                    if (tries == maxAttempts)
                    {
                        return (null, e.ToString());
                    }
                    await Task.Delay(2000);
                }
            } while (true);

        }
        public async Task<(string html, string error)> PostFormData(string url, List<KeyValuePair<string, string>> formData, int maxAttempts = 1)
        {
            var formContent = new FormUrlEncodedContent(formData);
            int tries = 0;
            do
            {
                try
                {
                    var response = await _httpClient.PostAsync(url, formContent);
                    string html = await response.Content.ReadAsStringAsync();
                    return (html, null);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    tries++;
                    if (tries == maxAttempts)
                    {
                        return (null, ex.ToString());
                    }
                    await Task.Delay(2000);
                }
            } while (true);
        }
    }
}
