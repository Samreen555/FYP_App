using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace FYP_App.UITests
{
    public static class UITestHelper
    {
        public static bool IsElementPresent(IWebDriver driver, By by)
        {
            try
            {
                driver.FindElement(by);
                return true;
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        }

        public static void SafeClick(IWebDriver driver, By by)
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            var element = wait.Until(d => d.FindElement(by));
            element.Click();
        }

        public static void SafeSendKeys(IWebDriver driver, By by, string text)
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            var element = wait.Until(d => d.FindElement(by));
            element.Clear();
            element.SendKeys(text);
        }

        public static string GenerateUniqueEmail()
        {
            return $"test_{Guid.NewGuid():N}@fyp-test.edu";
        }

        public static string GenerateUniqueRegNo()
        {
            return $"REG{Guid.NewGuid():N}".Substring(0, 12);
        }
    }
}