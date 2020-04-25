namespace news_alert
{
    public class Post {
        public string guid { get; set; }
    }
    public class WebhookData
    {
        public string content { get; set; }
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

    public class StockData {
        // name for serverless golang
        public string ticker {get; set;}
        public double targetPrice{get; set;}
    }
}