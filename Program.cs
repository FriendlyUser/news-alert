using System;
//Request library
using System.Net;
using System.IO;
using System.Web;
using System.Xml;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using Elasticsearch.Net;
namespace news_test
{
    public class Post {
        public string guid { get; set; }
        public string url { get; set; }
        public string title { get; set; }
    }
    // webhook format
    // https://birdie0.github.io/discord-webhooks-guide/structure/embeds.html
    public class WebhookData
    {
        public string content { get; set; }
        public WebhookEmbeds[] embeds {get; set;}
    }

    public class WebhookEmbeds 
    {
        public string url { get; set; }
        public string title { get; set; }
        public string description { get; set; }
    }

    public class SearchResult {
        public Hits hits {get; set;}
    }
    public class Hits {
        public Total total {get; set;}
    }
    public class Total {
        public int value {get; set;}
        public string relation {get; set;}
    }
    public class Program
    {
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
            string postTitle = item["title"].InnerText;
            string postDescription = item["description"].InnerText;
            string discordMessage = string.Format(discordTemplate, postTitle,
                item["pubDate"].InnerText, postLink);
            // write message of data sent to discord
            // object converted to json
            var w = new WebhookData() { content = discordMessage };

            // using custom object here
            var discordMessageObj = new
            {
                content = discordMessage,
                embeds = new [] {
                    new {
                        url = postLink,
                        title = postTitle,
                        description = postDescription
                    }
                }
            };
            Console.WriteLine(discordMessageObj);
            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                // serializing a class
                // streamWriter.Write(JsonSerializer.Serialize<WebhookData>(w));

                // serialize an object
                streamWriter.Write(JsonSerializer.Serialize(discordMessageObj));   
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
                guid = item["guid"].InnerText,
                url = postLink,
                title = postTitle

            };
            var asyncIndexResponse = lowlevelClient.Index<StringResponse>("post", PostData.Serializable(post)); 
            string responseString = asyncIndexResponse.Body;
            Console.WriteLine(responseString);
        }

        // add command flags, espeically one to change the index being used
        // would be good for testing
        static void Main(string[] args)
        {
            var searchItems = new List<string> { "nextech ar", "hive blockchain stock", "aurora cannabis", "trump putin oil" };
            foreach (string searchText in searchItems)
            {
                Console.WriteLine(searchText);
                // line of items of cli list
                string xml = FetchData(searchText);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml);
                // only print the first few elements
                //Display all the book titles.
                XmlNodeList items = doc.GetElementsByTagName("item");
                // grab first 10 entries, check if they happened within an hour and 
                // notify me that stock is selected
                // grab pubDate
                // grab title
                // grab link
                DateTime utcDate = DateTime.UtcNow;
                for (int i=0; i < 5; i++)
                {   
                    // Console.WriteLine(items[i].InnerXml);
                    // Console.WriteLine(items[i]["title"].OuterXml);
                    // Console.WriteLine(items[i]["title"].InnerText);
                    DateTime pubDate = Convert.ToDateTime(items[i]["pubDate"].InnerText);
                    // Console.WriteLine(pubDate);
                    if (utcDate.Subtract(pubDate).TotalHours < 24 * 3) {
                        Console.WriteLine("Within 3 days");
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
                        // Console.WriteLine(responseJson);
                        SearchResult searchResult = JsonSerializer.Deserialize<SearchResult>(responseJson);
                        Console.WriteLine(searchResult.hits);
                        if (searchResult.hits.total.value == 0) {
                            SendItemToDiscord(items[i]);
                        } else {
                            Console.WriteLine("Match Already in DB, not going to print");
                        }
                    }
                }
            }
  
        }
        
        static public string FetchData(string searchText) {
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["q"] = searchText;
            string queryString = query.ToString();

            string htmlData = string.Empty;
            string urlTemplate = @"https://news.google.com/rss/search?{0}";
            string url = string.Format(urlTemplate, queryString);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.AutomaticDecompression = DecompressionMethods.GZip;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                htmlData = reader.ReadToEnd();
            }
            return htmlData;
        }
    }
}
