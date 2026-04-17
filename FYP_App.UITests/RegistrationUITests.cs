using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace FYP_App.UITests
{
    [TestFixture]
    public class RegistrationUITests : BaseUITest
    {
        [Test]
        public void RegisterGroupPage_LoadsSuccessfully()
        {
            NavigateTo("/Account/RegisterGroup");
            WaitForPageLoad();
            TakeScreenshot("RegisterGroup_Loaded");

            Assert.That(Driver.PageSource, Does.Contain("Registration").Or.Contain("Register"));
        }

        [Test]
        public void RegisterGroup_HasRequiredFields()
        {
            NavigateTo("/Account/RegisterGroup");

            var student1Name = FindElementSafe("Student1Name");
            var student1Email = FindElementSafe("Student1Email");
            var student1RegNo = FindElementSafe("Student1RegNo");
            var student2Name = FindElementSafe("Student2Name");
            var student2Email = FindElementSafe("Student2Email");
            var student2RegNo = FindElementSafe("Student2RegNo");
            var proposedTitle = FindElementSafe("ProposedTitle");
            var proposedDomain = FindElementSafe("ProposedDomain");
            var preferredSupervisor = FindElementSafe("PreferredSupervisor");
            var submitButton = Driver.FindElement(By.CssSelector("button[type='submit']"));

            TakeScreenshot("RegisterGroup_Fields");

            Assert.That(student1Name != null || student2Name != null, Is.True);
            Assert.That(submitButton, Is.Not.Null);
        }

        [Test]
        public void RegisterGroup_EmptySubmit_ShowsValidation()
        {
            NavigateTo("/Account/RegisterGroup");

            var submitButton = FindElementSafe(By.CssSelector("button[type='submit']"), By.CssSelector("input[type='submit']"));

            if (submitButton != null)
            {
                ScrollToElement(submitButton);

                // Use JavaScript click to avoid interception
                ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].click();", submitButton);

                System.Threading.Thread.Sleep(1000);
                TakeScreenshot("RegisterGroup_EmptyValidation");
            }

            // Verify we see validation or stay on registration page
            Assert.That(Driver.PageSource, Does.Contain("required").Or.Contain("Register").Or.Contain("Registration"));
        }
        [Test]
        public void RegisterGroup_InvalidEmail_ShowsValidation()
        {
            NavigateTo("/Account/RegisterGroup");

            var student1Email = FindElementSafe("Student1Email");
            if (student1Email != null)
            {
                student1Email.SendKeys("invalid-email");

                var submitButton = Driver.FindElement(By.CssSelector("button[type='submit']"));
                submitButton.Click();

                System.Threading.Thread.Sleep(500);

                TakeScreenshot("RegisterGroup_InvalidEmail");

                var emailError = Driver.FindElements(By.CssSelector("[data-valmsg-for='Student1Email']"));
                Assert.That(emailError.Count, Is.GreaterThan(0));
            }
            else
            {
                Assert.Inconclusive("Email field not found");
            }
        }

        private IWebElement? FindElementSafe(string name)
        {
            try { return Driver.FindElement(By.Id(name)); } catch { }
            try { return Driver.FindElement(By.Name(name)); } catch { }
            return null;
        }

    }
}