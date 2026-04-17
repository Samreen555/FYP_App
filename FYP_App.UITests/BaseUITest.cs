using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using System.Net.Http;

namespace FYP_App.UITests
{
    public abstract class BaseUITest
    {
        protected IWebDriver Driver;
        protected WebDriverWait Wait;
        protected string BaseUrl;

        [SetUp]
        public virtual void SetUp()
        {
            try
            {
                new DriverManager().SetUpDriver(new ChromeConfig());

                var options = new ChromeOptions();
                // options.AddArgument("--headless"); // Commented to see browser
                options.AddArgument("--no-sandbox");
                options.AddArgument("--disable-dev-shm-usage");
                options.AddArgument("--window-size=1920,1080");
                options.AddArgument("--ignore-certificate-errors");
                options.AddArgument("--ignore-ssl-errors");

                Driver = new ChromeDriver(options);
                Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
                Driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);

                Wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));

                // Make sure BaseUrl ends with a single slash
                BaseUrl = "https://localhost:7295";
                if (!BaseUrl.EndsWith("/"))
                {
                    BaseUrl += "/";
                }

                Console.WriteLine($"Base URL set to: {BaseUrl}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Setup failed: {ex.Message}");
                throw;
            }
        }

        [TearDown]
        public virtual void TearDown()
        {
            try { Driver?.Quit(); } catch { }
            try { Driver?.Dispose(); } catch { }
        }

        protected void NavigateTo(string relativeUrl = "")
        {
            string url;

            // If it's an absolute URL (contains ://), use it directly
            if (relativeUrl.Contains("://"))
            {
                url = relativeUrl;
            }
            else
            {
                // Remove leading slash if present to avoid double slashes
                if (relativeUrl.StartsWith("/"))
                {
                    relativeUrl = relativeUrl.Substring(1);
                }
                url = BaseUrl + relativeUrl;
            }

            Console.WriteLine($"Navigating to: {url}");
            Driver.Navigate().GoToUrl(url);
        }

        protected IWebElement WaitForElement(By by, int timeoutSeconds = 10)
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(timeoutSeconds));
            return wait.Until(d => d.FindElement(by));
        }

        protected void WaitForPageLoad()
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(30));
            wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));
        }

        protected void TakeScreenshot(string fileName)
        {
            try
            {
                var screenshot = ((ITakesScreenshot)Driver).GetScreenshot();
                var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Screenshots");
                Directory.CreateDirectory(path);
                var filePath = Path.Combine(path, $"{fileName}_{DateTime.Now:yyyyMMdd_HHmmss}.png");
                screenshot.SaveAsFile(filePath);
                Console.WriteLine($"Screenshot saved: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to take screenshot: {ex.Message}");
            }
        }

        protected bool IsAppRunning()
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(3);
                var response = client.GetAsync(BaseUrl).Result;
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
        protected void ScrollToElement(IWebElement element)
        {
            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].scrollIntoView(true);", element);
            System.Threading.Thread.Sleep(300);
        }

        protected void SafeClick(IWebElement element)
        {
            try
            {
                ScrollToElement(element);
                element.Click();
            }
            catch (ElementClickInterceptedException)
            {
                // Try JavaScript click as fallback
                ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].click();", element);
            }
        }

        protected IWebElement? FindElementSafe(params By[] bys)
        {
            foreach (var by in bys)
            {
                try
                {
                    var element = Driver.FindElement(by);
                    if (element.Displayed && element.Enabled)
                        return element;
                }
                catch { }
            }
            return null;
        }

    }
}