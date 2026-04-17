using OpenQA.Selenium;

namespace FYP_App.UITests
{
    [TestFixture]
    public class PanelUITests : BaseUITest
    {
        [Test]
        public void LoginPage_PanelRole_Exists()
        {
            NavigateTo("/Account/Login");
            TakeScreenshot("Panel_LoginPage");
            Assert.That(Driver.PageSource, Does.Contain("Login").Or.Contain("FYP"));
        }

        [Test]
        public void PanelDashboard_RequiresAuthentication()
        {
            NavigateTo("/Panel/Index");
            System.Threading.Thread.Sleep(500);
            TakeScreenshot("Panel_Unauthenticated");
            Assert.That(Driver.Url.ToLower(), Does.Contain("login").Or.Contain("account"));
        }
    }
}