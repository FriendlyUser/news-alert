using Xunit;
using Xunit.Abstractions;
using System.Xml;
using System.Net.Http;
using System;
using Elasticsearch.Net;
using System.IO;
using System.Threading.Tasks;
namespace news_alert
{
    public class Guid
    {
        public string guid { get; set; }
    }
    public class TestNotifications
    {
        private readonly ITestOutputHelper output;
        public TestNotifications(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void PassingTest()
        {
            Assert.Equal(4, Add(2, 2));
        }

        // [Fact]
        // public void FailingTest()
        // {
        //     Assert.Equal(5, Add(2, 2));
        // }

        int Add(int x, int y)
        {
            return x + y;
        }

        [Fact]
        public void FailXmlTest() {
            // Create the XmlDocument.
            XmlDocument doc = new XmlDocument();
            Assert.Throws<XmlException>(() => doc.LoadXml("<item><nam>wrench</name></item>"));
            // Assert.Throws(doc.LoadXml("<item><nam>wrench</name></item>"));
            output.WriteLine("This is output from");
        }
        [Fact]
        public async Task FetchDataTest()
        {
            News p1 = new News();  //a object of class  
            string data = await News.FetchData("nextech ar");
            Assert.True(data.Length > 100, "The data was not greater than 100 characters");
            // make sure the data is valid xml
        }
        [Fact]
        public void DiscordMessageTest()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<book>" +
            "  <title>Oberon's Legacy</title>" +
            "  <price>5.95</price>" +
            "</book>");
            XmlNode newElem = doc.CreateNode("element", "pages", "");
            News.SendItemToDiscord(newElem);
            Assert.True(true, "The data was not greater than 100 characters");
            // make sure the data is valid xml
        }
        [Fact]
        public async void AddDocToEsTest()
        {
          string esInstance = Environment.GetEnvironmentVariable("ES_INSTANCE");
          var settings = new ConnectionConfiguration(new Uri(esInstance))
            .RequestTimeout(TimeSpan.FromMinutes(2));

          var lowlevelClient = new ElasticLowLevelClient(settings);
          var sample_guid = new Guid
          {
              guid = "Martijn"
          };
          var asyncIndexResponse = await lowlevelClient.IndexAsync<StringResponse>("test_post", "1", PostData.Serializable(sample_guid)); 
          string responseString = asyncIndexResponse.Body;
          Console.WriteLine(responseString);
        }
    }
}