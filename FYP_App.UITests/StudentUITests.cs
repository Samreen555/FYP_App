using OpenQA.Selenium;

namespace FYP_App.UITests
{
    [TestFixture]
    public class StudentUITests : BaseUITest
    {
        [Test]
        public void LoginPage_StudentRole_Exists()
        {
            NavigateTo("/Account/Login");
            TakeScreenshot("Student_LoginPage");
            Assert.That(Driver.PageSource, Does.Contain("Login").Or.Contain("FYP"));
        }

        [Test]
        public void StudentDashboard_RequiresAuthentication()
        {
            NavigateTo("/Student/Index");
            System.Threading.Thread.Sleep(500);
            TakeScreenshot("Student_Unauthenticated");
            Assert.That(Driver.Url.ToLower(), Does.Contain("login").Or.Contain("account"));
        }

        [Test]
        public void RegisterGroup_IsAccessibleToPublic()
        {
            NavigateTo("/Account/RegisterGroup");
            WaitForPageLoad();
            TakeScreenshot("Student_RegisterGroup");
            Assert.That(Driver.PageSource, Does.Contain("Registration"));
        }
    }
}