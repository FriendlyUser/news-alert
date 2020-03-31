using Xunit;
using Xunit.Abstractions;
using System.Xml;
namespace news_test
{
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
        public void FetchDataTest()
        {
            Program p1 = new Program();  //a object of class  
            string data = Program.FetchData("nextech ar");
            Assert.True(data.Length > 100, "The data was not greater than 100 characters");
            // make sure the data is valid xml
        }
        [Fact]
        public void DiscordMessageTest()
        {
            Program p1 = new Program();  //a object of class  
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<book>" +
            "  <title>Oberon's Legacy</title>" +
            "  <price>5.95</price>" +
            "</book>");
            XmlNode newElem = doc.CreateNode("element", "pages", "");
            Program.SendItemToDiscord(newElem);
            Assert.True(true, "The data was not greater than 100 characters");
            // make sure the data is valid xml
        }
    }
}