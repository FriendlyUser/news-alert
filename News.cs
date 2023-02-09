using System;
//Request library
using System.Net;
using System.IO;
using System.Web;
using System.Xml;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elasticsearch.Net;
using System.Net.Http;
namespace news_alert
{
    public class News
    {
        public static readonly HttpClient client = new HttpClient();
    
        static public void SendItemToDiscord(XmlNode item) {
            // set discord webhook as private env
            string webhook = Environment.GetEnvironmentVariable("DISCORD_WEBHOOK");
            string esInstance = Environment.GetEnvironmentVariable("ES_INSTANCE");
            if (webhook == null) {
                Console.WriteLine("GET A DISCORD WEBHOOK");
                return;
            }
            var request = (HttpWebRequest)WebRequest.Create(webhook);
            request.ContentType = "application/json";
            request.Method = "POST";
            DateTime pubDate = Convert.ToDateTime(item["pubDate"].InnerText);
            string postLink = item["link"].InnerText;
            string discordTemplate = "{0} \n {1} \n {2} ";
            string discordMessage = string.Format(discordTemplate, item["title"].InnerText,
                item["pubDate"].InnerText, postLink);
            // write message of data sent to discord
            var w = new WebhookData() { content = discordMessage };
            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                streamWriter.Write(JsonSerializer.Serialize<WebhookData>(w));
            }
            var httpResponse = (HttpWebResponse)request.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
            }
            // send data to es instance
            var settings = new ConnectionConfiguration(new Uri(esInstance))
                .RequestTimeout(TimeSpan.FromMinutes(2));
            var lowlevelClient = new ElasticLowLevelClient(settings);
            var post = new Post
            {
                guid = item["guid"].InnerText
            };
            var asyncIndexResponse = lowlevelClient.Index<StringResponse>("post", PostData.Serializable(post)); 
            string responseString = asyncIndexResponse.Body;
            Console.WriteLine(responseString);
        }

        static async Task Main(string[] args)
        {
            await checkNews();
            await Task.Run(() => checkPrices());
        }
        static async public Task checkPrices() {
            string stockUrl = Environment.GetEnvironmentVariable("STOCK_URL");
            if (stockUrl == null) {
                Console.WriteLine("GET A Stock URL WEBHOOK");
                return;
            }
            List<StockData> stocks = new List<StockData>();
            stocks.Add(new StockData() {ticker="NEXCF", targetPrice=1.60});
            foreach (StockData stock in stocks)
            {
                Console.WriteLine(stock.ticker);
                // query api
            }
            // either return a completed Task, or await for it (there is a difference!
            await Task.CompletedTask;
        }
        static async public Task checkNews() {
            var searchItems = new List<string> { "hydrogen",
                "analyticGPT", "natural gas", "recession",
                "Tech layoffs", "Bard AI"
            };
            foreach (string searchText in searchItems)
            {
                // line of items of cli list
                string xml = await FetchData(searchText);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml);
                // only print the first few elements
                XmlNodeList items = doc.GetElementsByTagName("item");
                DateTime utcDate = DateTime.UtcNow;
                for (int i=0; i < 5; i++)
                {   string pubDateStr = "";
                    try {
                        pubDateStr = items[i]["pubDate"].InnerText;
                    } catch (NullReferenceException e) {
                        Console.WriteLine("\nException Caught!");	
                        Console.WriteLine("Message :{0} ",e.Message);
                        continue;
                    }
                    DateTime pubDate = Convert.ToDateTime(pubDateStr);
                    // Console.WriteLine(pubDate);
                    if (utcDate.Subtract(pubDate).TotalHours < 24 * 2) {
                        Console.WriteLine("Within 2 days");
                        // check if id is in db
                        string esInstance = Environment.GetEnvironmentVariable("ES_INSTANCE");
                        var settings = new ConnectionConfiguration(new Uri(esInstance))
                            .RequestTimeout(TimeSpan.FromMinutes(2));
                        var lowlevelClient = new ElasticLowLevelClient(settings);
                        var searchResponse = lowlevelClient.Search<StringResponse>("post", PostData.Serializable(new
                        {
                            query = new
                            {
                                match = new
                                {
                                    guid = items[i]["guid"].InnerText// items[i]["guid"].InnerText
                                }
                            }
                        }));
                        var successful = searchResponse.Success;
                        var responseJson = searchResponse.Body;
                        SearchResult searchResult = JsonSerializer.Deserialize<SearchResult>(responseJson);
                        if (searchResult.hits.total.value == 0) {
                            SendItemToDiscord(items[i]);
                        } else {
                            Console.WriteLine("Match Already in DB, not going to print");
                        }
                    }
                }
            }
            await Task.CompletedTask;
        }
        static async public Task<string> FetchData(string searchText) {
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["q"] = searchText;
            string queryString = query.ToString();

            string htmlData = string.Empty;
            string urlTemplate = @"https://news.google.com/rss/search?{0}";
            string url = string.Format(urlTemplate, queryString);
            try	
            {
              string responseBody = await client.GetStringAsync(url);

              Console.WriteLine(responseBody);
              return await Task.FromResult(responseBody);
            }
            catch(HttpRequestException e)
            {
              Console.WriteLine("\nException Caught!");	
              Console.WriteLine("Message :{0} ",e.Message);
            }
            return await Task.FromResult("");
        }
    }
}
