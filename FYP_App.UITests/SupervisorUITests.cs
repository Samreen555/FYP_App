using OpenQA.Selenium;

namespace FYP_App.UITests
{
    [TestFixture]
    public class SupervisorUITests : BaseUITest
    {
        [Test]
        public void LoginPage_SupervisorRole_Exists()
        {
            NavigateTo("/Account/Login");
            TakeScreenshot("Supervisor_LoginPage");
            Assert.That(Driver.PageSource, Does.Contain("Login").Or.Contain("FYP"));
        }

        [Test]
        public void SupervisorDashboard_RequiresAuthentication()
        {
            NavigateTo("/Supervisor/Index");
            System.Threading.Thread.Sleep(500);
            TakeScreenshot("Supervisor_Unauthenticated");
            Assert.That(Driver.Url.ToLower(), Does.Contain("login").Or.Contain("account"));
        }
    }
}