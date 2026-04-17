using OpenQA.Selenium;

namespace FYP_App.UITests
{
    [TestFixture]
    public class HODUITests : BaseUITest
    {
        [Test]
        public void LoginPage_HODRole_Exists()
        {
            NavigateTo("/Account/Login");
            TakeScreenshot("HOD_LoginPage");
            Assert.That(Driver.PageSource, Does.Contain("Login").Or.Contain("FYP"));
        }

        [Test]
        public void HODDashboard_RequiresAuthentication()
        {
            NavigateTo("/HOD/Index");
            System.Threading.Thread.Sleep(500);
            TakeScreenshot("HOD_Unauthenticated");
            Assert.That(Driver.Url.ToLower(), Does.Contain("login").Or.Contain("account"));
        }
    }
}