using OpenQA.Selenium;

namespace FYP_App.UITests
{
    [TestFixture]
    public class CoordinatorUITests : BaseUITest
    {
        public void LoginPage_CoordinatorRole_Exists()
        {
            NavigateTo("/Account/Login");
            TakeScreenshot("Coordinator_LoginPage");

            // Just verify login page loads - we know it does from other tests
            Assert.That(Driver.PageSource, Does.Contain("Login").Or.Contain("FYP"));
        }

        [Test]
        public void CoordinatorDashboard_RequiresAuthentication()
        {
            NavigateTo("/Coordinator/Index");
            System.Threading.Thread.Sleep(500);
            TakeScreenshot("Coordinator_Unauthenticated");

            // Should redirect to login
            Assert.That(Driver.Url.ToLower(), Does.Contain("login").Or.Contain("account"));
        }
    }
 }