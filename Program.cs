using System;
//Request library
using System.Net;
using System.IO;
using System.Web;
using System.Xml;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
namespace news_test
{
    public class WebhookData
    {
        public string content { get; set; }
    }
    public class Program
    {
        static public void SendItemToDiscord(XmlNode item) {
            // set discord webhook as private env
            string webhook = Environment.GetEnvironmentVariable("DISCORD_WEBHOOK");
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
            Console.WriteLine(discordMessage);
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
        }
        static void Main(string[] args)
        {
            var searchItems = new List<string> { "nextech ar", "hive blockchain stock", "aurora cannabis" };
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
                        SendItemToDiscord(items[i]);
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
