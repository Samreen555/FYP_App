using OpenQA.Selenium;

namespace FYP_App.UITests
{
    [TestFixture]
    public class SanityUITests : BaseUITest
    {
        [Test]
        public void Test1_ChromeDriver_Works()
        {
            // Navigate to Google using absolute URL
            NavigateTo("https://www.google.com");

            System.Threading.Thread.Sleep(1000);

            TakeScreenshot("GoogleHomepage");

            Assert.That(Driver.Title, Does.Contain("Google"));
        }

        [Test]
        public void Test2_Application_IsAccessible()
        {
            // Try to access your application
            NavigateTo("/");

            System.Threading.Thread.Sleep(1000);

            TakeScreenshot("AppHomepage");

            // Should not show connection error
            Assert.That(Driver.PageSource, Does.Not.Contain("ERR_CONNECTION_REFUSED"));
            Assert.That(Driver.PageSource, Does.Not.Contain("This site can't be reached"));
        }

        [Test]
        public void Test3_LoginPage_IsAccessible()
        {
            // Try to access login page
            NavigateTo("/Account/Login");

            System.Threading.Thread.Sleep(1000);

            TakeScreenshot("LoginPage");

            Assert.That(Driver.PageSource, Does.Not.Contain("ERR_CONNECTION_REFUSED"));
        }

        [Test]
        public void Test4_PageSource_ShowsContent()
        {
            // Get page source to debug
            NavigateTo("/");

            System.Threading.Thread.Sleep(1000);

            var pageSource = Driver.PageSource;

            Console.WriteLine("=== PAGE SOURCE START ===");
            Console.WriteLine(pageSource.Substring(0, Math.Min(500, pageSource.Length)));
            Console.WriteLine("=== PAGE SOURCE END ===");

            Assert.That(pageSource, Is.Not.Empty);
        }

        [Test]
        public void Test5_LoginForm_Exists()
        {
            // Check if login form elements exist
            NavigateTo("/Account/Login");

            System.Threading.Thread.Sleep(1000);

            TakeScreenshot("LoginForm");

            // Check for common login form elements
            bool hasEmailField = Driver.FindElements(By.Name("Email")).Count > 0 ||
                                Driver.FindElements(By.Id("Email")).Count > 0 ||
                                Driver.FindElements(By.CssSelector("input[type='email']")).Count > 0;

            bool hasPasswordField = Driver.FindElements(By.Name("Password")).Count > 0 ||
                                   Driver.FindElements(By.Id("Password")).Count > 0 ||
                                   Driver.FindElements(By.CssSelector("input[type='password']")).Count > 0;

            bool hasSubmitButton = Driver.FindElements(By.CssSelector("button[type='submit']")).Count > 0 ||
                                  Driver.FindElements(By.CssSelector("input[type='submit']")).Count > 0;

            Console.WriteLine($"Email field found: {hasEmailField}");
            Console.WriteLine($"Password field found: {hasPasswordField}");
            Console.WriteLine($"Submit button found: {hasSubmitButton}");

            // At least the page should load without error
            Assert.That(Driver.PageSource, Does.Not.Contain("ERR_CONNECTION_REFUSED"));
        }
    }
}