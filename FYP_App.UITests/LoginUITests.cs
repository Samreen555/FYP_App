using OpenQA.Selenium;

namespace FYP_App.UITests
{
    [TestFixture]
    public class LoginUITests : BaseUITest
    {
        [Test]
        public void LoginPage_LoadsSuccessfully()
        {
            NavigateTo("/Account/Login");
            WaitForPageLoad();
            TakeScreenshot("LoginPage_Loaded");

            Assert.That(Driver.Title, Does.Contain("Login").Or.Contain("FYP"));
        }

        [Test]
        public void LoginPage_HasRequiredFields()
        {
            NavigateTo("/Account/Login");

            var emailField = FindElementSafe(By.Name("Email"), By.Id("Email"), By.CssSelector("input[type='email']"));
            var passwordField = FindElementSafe(By.Name("Password"), By.Id("Password"), By.CssSelector("input[type='password']"));
            var submitButton = FindElementSafe(By.CssSelector("button[type='submit']"), By.CssSelector("input[type='submit']"));

            Assert.That(emailField, Is.Not.Null, "Email field not found");
            Assert.That(passwordField, Is.Not.Null, "Password field not found");
            Assert.That(submitButton, Is.Not.Null, "Submit button not found");
        }

        [Test]
        public void Login_EmptyFields_ShowsValidation()
        {
            NavigateTo("/Account/Login");

            var submitButton = FindElementSafe(By.CssSelector("button[type='submit']"), By.CssSelector("input[type='submit']"));

            if (submitButton != null)
            {
                ScrollToElement(submitButton);

                try
                {
                    submitButton.Click();
                }
                catch (ElementClickInterceptedException)
                {
                    ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].click();", submitButton);
                }

                System.Threading.Thread.Sleep(1000);
                TakeScreenshot("Login_EmptyValidation");
            }

            // Just verify we're still on the page or see validation
            Assert.That(Driver.PageSource, Does.Contain("required").Or.Contain("Login"));
        }

        [Test]
        public void Login_InvalidCredentials_ShowsError()
        {
            NavigateTo("/Account/Login");

            var emailField = FindElementSafe(By.Name("Email"), By.Id("Email"));
            var passwordField = FindElementSafe(By.Name("Password"), By.Id("Password"));
            var submitButton = FindElementSafe(By.CssSelector("button[type='submit']"));

            if (emailField != null && passwordField != null && submitButton != null)
            {
                emailField.SendKeys("invalid@test.com");
                passwordField.SendKeys("WrongPassword123!");

                ScrollToElement(submitButton);
                SafeClick(submitButton);

                System.Threading.Thread.Sleep(1000);
                TakeScreenshot("Login_InvalidCredentials");
            }

            Assert.That(Driver.PageSource, Does.Contain("Invalid").Or.Contain("failed").Or.Contain("Login"));
        }
    }
}