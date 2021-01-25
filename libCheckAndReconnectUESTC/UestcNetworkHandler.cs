using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Net.Http;
using System.Diagnostics;

namespace libCheckAndReconnectUESTC
{
    public class UestcNetworkHandler
    {
        private string loginUsernameXPATH = @"//input[@name='username']";
        private string loginPasswordXPATH = @"//input[@name='password']";
        private string loginButtonXPATH = @"//button[@id='school']";
        private ChromeOptions webOptions;
        private ChromeDriverService webService;
        private IWebDriver webDriver;
        private HttpClient httpClient;
        public UestcNetworkHandler()
        {
            httpClient = new HttpClient();

            webOptions = new ChromeOptions();
            webOptions.AddArgument("no-sandbox");
            webOptions.AddArgument("--disable-dev-shm-usage");
            webOptions.AddArgument("--headless");
            webOptions.AddArgument("--disable-extensions");
            webOptions.AddArgument("--disable-gpu");
            webOptions.AddArgument("--disable-setuid-sandbox");
            webOptions.AddArgument("disable-infobars");

            webService = ChromeDriverService.CreateDefaultService();
            webService.SuppressInitialDiagnosticInformation = true;
            webService.HideCommandPromptWindow = true;
        }
        public async Task<bool> CheckNetwork(string host = "https://www.baidu.com")
        {
            Uri uri = new Uri(host);
            try
            {
                var isConnectSuccess = await httpClient.GetAsync(uri);
                if (isConnectSuccess.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }

        }
        public bool reconnectNetworkUestc(
            string loginSite, string username, string password, bool stargPageOnlyNoConnect=false)
        {
            //Debug.WriteLine("Open login page using selenium");
            webDriver = new ChromeDriver(webService,webOptions);
            webDriver.Navigate().GoToUrl(loginSite);
            IWebElement usernameElement = webDriver.FindElement(By.XPath(loginUsernameXPATH));
            IWebElement passwordElement = webDriver.FindElement(By.XPath(loginPasswordXPATH));
            IWebElement buttomElement = webDriver.FindElement(By.XPath(loginButtonXPATH));
            usernameElement.SendKeys(username);
            passwordElement.SendKeys(password);
            Thread.Sleep(TimeSpan.FromSeconds(6));
            if (!stargPageOnlyNoConnect)
            {
                buttomElement.Click();
            }
            Thread.Sleep(TimeSpan.FromSeconds(6));
            webDriver.Quit();
            return true; 
        }

    }
}