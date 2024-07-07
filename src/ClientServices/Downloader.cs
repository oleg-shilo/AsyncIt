namespace ClientServices
{
    public class Downloader
    {
        public string DownloadFile(string url)
        {
            Task.Delay(1000).Wait();
            return $"Downloaded file from {url}";
        }
    }
}