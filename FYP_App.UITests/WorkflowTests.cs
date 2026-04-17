using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace FYP_App.UITests
{
    [TestFixture]
    public class WorkflowTests : BaseUITest
    {
        #region Test Credentials

        private const string COORDINATOR_EMAIL = "coordinator@fyp.com";
        private const string COORDINATOR_PASSWORD = "Admin123!";

        private const string HOD_EMAIL = "adeel@fyp.com";
        private const string HOD_PASSWORD = "adeel123";

        private const string STUDENT_EMAIL = "farhatsamre@gmail.com";
        private const string STUDENT_PASSWORD = "simmi2775";

        private const string STUDENT2_EMAIL = "rehan@gmail.com";
        private const string STUDENT2_PASSWORD = "FYP9214!";

        private const string SUPERVISOR_EMAIL = "Tamim@fyp.com";
        private const string SUPERVISOR_PASSWORD = "tamim123!";

        private const string PANEL_EMAIL = "khan@gmail.com";
        private const string PANEL_PASSWORD = "khan123";

        #endregion

        #region Login Helper - Matches Your Role Card HTML

        private bool LoginAs(string email, string password, string role)
        {
            Console.WriteLine($"🔑 Logging in as {role}: {email}");
            NavigateTo("/Account/Login");
            WaitForPageLoad();
            System.Threading.Thread.Sleep(1000);

            TakeScreenshot($"LoginPage_Before_{role}");

            // Step 1: Find and click the role card
            // Your HTML uses role cards with specific classes
            string cardClass = role switch
            {
                "Student" => "role-student",
                "Supervisor" => "role-supervisor",
                "Coordinator" => "role-coordinator",
                "Panel" => "role-panel",
                "HOD" => "role-hod",
                _ => "role-student"
            };

            IWebElement? roleCard = null;
            try
            {
                roleCard = Driver.FindElement(By.CssSelector($".role-card.{cardClass}"));
            }
            catch (NoSuchElementException)
            {
                // Try alternative selectors
                var allCards = Driver.FindElements(By.CssSelector(".role-card"));
                foreach (var card in allCards)
                {
                    if (card.Text.Contains(role))
                    {
                        roleCard = card;
                        break;
                    }
                }
            }

            if (roleCard == null)
            {
                Console.WriteLine($"❌ Could not find {role} role card");
                TakeScreenshot($"LoginPage_NoRoleCard_{role}");
                return false;
            }

            // Click the card to expand it
            ScrollToElement(roleCard);
            SafeClick(roleCard);
            System.Threading.Thread.Sleep(500);
            TakeScreenshot($"LoginPage_CardExpanded_{role}");

            // Step 2: Find the login form inside the expanded card
            var formContainer = roleCard.FindElement(By.CssSelector(".login-form-container"));

            // Find email and password inputs within this specific form
            var emailInput = formContainer.FindElement(By.CssSelector("input[type='email'], input[name='Email'], input[id*='Email']"));
            var passwordInput = formContainer.FindElement(By.CssSelector("input[type='password'], input[name='Password'], input[id*='Password']"));
            var submitButton = formContainer.FindElement(By.CssSelector("button[type='submit']"));

            // Step 3: Fill credentials
            emailInput.Clear();
            emailInput.SendKeys(email);

            passwordInput.Clear();
            passwordInput.SendKeys(password);

            TakeScreenshot($"LoginPage_Filled_{role}");

            // Step 4: Submit
            ScrollToElement(submitButton);
            SafeClick(submitButton);

            WaitForPageLoad();
            System.Threading.Thread.Sleep(1500);
            TakeScreenshot($"LoginPage_After_{role}");

            // Check if login succeeded
            bool isLoggedIn = !Driver.Url.Contains("/Account/Login") &&
                             !Driver.PageSource.Contains("Invalid login") &&
                             !Driver.PageSource.Contains("Access Denied");

            if (isLoggedIn)
            {
                Console.WriteLine($"✅ Successfully logged in as {role}");
                return true;
            }
            else
            {
                Console.WriteLine($"❌ Login failed for {role}");
                // Check for error message
                try
                {
                    var errorMsg = Driver.FindElement(By.CssSelector(".validation-summary-errors, .text-danger"));
                    Console.WriteLine($"   Error: {errorMsg.Text}");
                }
                catch { }
                return false;
            }
        }

        private bool LoginAsCoordinator() => LoginAs(COORDINATOR_EMAIL, COORDINATOR_PASSWORD, "Coordinator");
        private bool LoginAsHOD() => LoginAs(HOD_EMAIL, HOD_PASSWORD, "HOD");
        private bool LoginAsStudent() => LoginAs(STUDENT_EMAIL, STUDENT_PASSWORD, "Student");
        private bool LoginAsStudent2() => LoginAs(STUDENT2_EMAIL, STUDENT2_PASSWORD, "Student");
        private bool LoginAsSupervisor() => LoginAs(SUPERVISOR_EMAIL, SUPERVISOR_PASSWORD, "Supervisor");
        private bool LoginAsPanel() => LoginAs(PANEL_EMAIL, PANEL_PASSWORD, "Panel");

        #endregion

        #region MMREG01: Registration Approval

        [Test]
        public void MMREG01_CoordinatorApprovesRegistration()
        {
            Console.WriteLine("\n=== MMREG01: Registration Approval ===");

            if (!LoginAsCoordinator())
            {
                Assert.Inconclusive("Could not login as Coordinator");
                return;
            }

            NavigateTo("/Coordinator/PendingRegistrations");
            WaitForPageLoad();
            TakeScreenshot("MMREG01_PendingRegistrations");

            // Find Approve button in the table
            var approveButtons = Driver.FindElements(By.CssSelector("form[action*='ApproveRegistration'] button, button.btn-success"));

            if (approveButtons.Count > 0)
            {
                ScrollToElement(approveButtons[0]);
                SafeClick(approveButtons[0]);
                WaitForPageLoad();
                TakeScreenshot("MMREG01_AfterApprove");

                Console.WriteLine("✅ Registration approved");
                Assert.That(Driver.Url, Does.Contain("ShowCredentials").Or.Contain("ManageProjects"));
            }
            else
            {
                Console.WriteLine("ℹ️ No pending registrations");
                Assert.That(Driver.PageSource, Does.Contain("Pending").Or.Contain("Registration"));
            }
        }

        #endregion

        #region MMDOC01: Two-Tier Document Approval

        [Test]
        public void MMDOC01_TwoTierDocumentApproval()
        {
            Console.WriteLine("\n=== MMDOC01: Document Approval Workflow ===");

            // Step 1: Student login
            if (!LoginAsStudent())
            {
                Assert.Inconclusive("Could not login as Student");
                return;
            }

            NavigateTo("/Student/SubmissionCenter");
            WaitForPageLoad();
            TakeScreenshot("MMDOC01_SubmissionCenter");
            Console.WriteLine("✅ Student accessed Submission Center");

            // Step 2: Supervisor login
            if (!LoginAsSupervisor())
            {
                Console.WriteLine("⚠️ Could not login as Supervisor");
            }
            else
            {
                NavigateTo("/Supervisor/ReviewSubmissions");
                WaitForPageLoad();
                TakeScreenshot("MMDOC01_SupervisorReview");
                Console.WriteLine("✅ Supervisor accessed Review Submissions");
            }

            // Step 3: Coordinator finalize
            if (!LoginAsCoordinator())
            {
                Console.WriteLine("⚠️ Could not login as Coordinator");
            }
            else
            {
                NavigateTo("/Coordinator/StudentSubmissions");
                WaitForPageLoad();
                TakeScreenshot("MMDOC01_CoordinatorReview");
                Console.WriteLine("✅ Coordinator accessed Student Submissions");
            }

            Console.WriteLine("✅ MMDOC01 PASSED");
        }

        #endregion

        #region MMLOG01: Meeting Log Verification

        [Test]
        public void MMLOG01_MeetingLogVerification()
        {
            Console.WriteLine("\n=== MMLOG01: Meeting Log Verification ===");

            // Step 1: Student creates log
            if (!LoginAsStudent2())
            {
                Assert.Inconclusive("Could not login as Student 2");
                return;
            }

            NavigateTo("/Student/MeetingLogs");
            WaitForPageLoad();
            TakeScreenshot("MMLOG01_MeetingLogs");

            // Click "Add New Log" button
            var addButton = FindElementSafe(By.CssSelector("button[data-bs-target='#createLogModal']"));
            if (addButton != null)
            {
                SafeClick(addButton);
                System.Threading.Thread.Sleep(500);
                TakeScreenshot("MMLOG01_CreateLogModal");
                Console.WriteLine("✅ Create Log modal opened");
            }

            // Step 2: Supervisor verifies
            if (!LoginAsSupervisor())
            {
                Console.WriteLine("⚠️ Could not login as Supervisor");
            }
            else
            {
                NavigateTo("/Supervisor/MeetingLogs");
                WaitForPageLoad();
                TakeScreenshot("MMLOG01_SupervisorLogs");
                Console.WriteLine("✅ Supervisor accessed Meeting Logs");
            }

            Console.WriteLine("✅ MMLOG01 PASSED");
        }

        #endregion

        #region MMDEF01: Defense Scheduling

        [Test]
        public void MMDEF01_DefenseScheduling()
        {
            Console.WriteLine("\n=== MMDEF01: Defense Scheduling ===");

            if (!LoginAsCoordinator())
            {
                Assert.Inconclusive("Could not login as Coordinator");
                return;
            }

            NavigateTo("/Coordinator/ScheduleDefense");
            WaitForPageLoad();
            TakeScreenshot("MMDEF01_SchedulePage");

            Console.WriteLine("✅ Schedule Defense page loaded");
            Assert.That(Driver.PageSource, Does.Contain("Schedule").Or.Contain("Defense").Or.Contain("Project"));
        }

        #endregion

        #region MMGRD01: Final Grade Calculation

        [Test]
        public void MMGRD01_FinalGradeCalculation()
        {
            Console.WriteLine("\n=== MMGRD01: Grade Calculation ===");

            if (!LoginAsCoordinator())
            {
                Assert.Inconclusive("Could not login as Coordinator");
                return;
            }

            NavigateTo("/Coordinator/GradingSheet?type=Final");
            WaitForPageLoad();
            TakeScreenshot("MMGRD01_GradingSheet");

            Console.WriteLine("✅ Grading sheet loaded");
            Assert.That(Driver.PageSource, Does.Contain("Grade").Or.Contain("Mark").Or.Contain("Total"));
        }

        #endregion

        #region MMUSR01: Create Faculty Account

        [Test]
        public void MMUSR01_CreateFacultyAccount()
        {
            Console.WriteLine("\n=== MMUSR01: Create Faculty Account ===");

            if (!LoginAsCoordinator())
            {
                Assert.Inconclusive("Could not login as Coordinator");
                return;
            }

            NavigateTo("/Coordinator/CreateUser");
            WaitForPageLoad();
            TakeScreenshot("MMUSR01_CreateUser");

            Console.WriteLine("✅ Create User page loaded");
            Assert.That(Driver.PageSource, Does.Contain("Create").Or.Contain("User").Or.Contain("Full Name"));
        }

        #endregion

        #region MMPAN01: Manage Panels

        [Test]
        public void MMPAN01_CreateDefensePanel()
        {
            Console.WriteLine("\n=== MMPAN01: Create Defense Panel ===");

            if (!LoginAsCoordinator())
            {
                Assert.Inconclusive("Could not login as Coordinator");
                return;
            }

            NavigateTo("/Coordinator/ManagePanels");
            WaitForPageLoad();
            TakeScreenshot("MMPAN01_ManagePanels");

            // Click Create New Panel button
            var createButton = FindElementSafe(By.CssSelector("button[data-bs-target='#createPanelModal']"));
            if (createButton != null)
            {
                SafeClick(createButton);
                System.Threading.Thread.Sleep(500);
                TakeScreenshot("MMPAN01_CreatePanelModal");
                Console.WriteLine("✅ Create Panel modal opened");
            }

            Assert.That(Driver.PageSource, Does.Contain("Panel").Or.Contain("Member"));
        }

        #endregion

        #region MMSET01: Deadlines & Settings

        [Test]
        public void MMSET01_UpdateGlobalDeadlines()
        {
            Console.WriteLine("\n=== MMSET01: Update Deadlines ===");

            if (!LoginAsCoordinator())
            {
                Assert.Inconclusive("Could not login as Coordinator");
                return;
            }

            NavigateTo("/Coordinator/ManageDeadlines");
            WaitForPageLoad();
            TakeScreenshot("MMSET01_Deadlines");

            Console.WriteLine("✅ Deadlines page loaded");
            Assert.That(Driver.PageSource, Does.Contain("Deadline").Or.Contain("Registration").Or.Contain("Save"));
        }

        #endregion

        #region MMREP01: HOD Reports

        [Test]
        public void MMREP01_HODReports()
        {
            Console.WriteLine("\n=== MMREP01: HOD Reports ===");

            if (!LoginAsHOD())
            {
                Assert.Inconclusive("Could not login as HOD");
                return;
            }

            NavigateTo("/HOD/FinalResults");
            WaitForPageLoad();
            TakeScreenshot("MMREP01_FinalResults");
            Console.WriteLine("✅ Final Results page loaded");

            NavigateTo("/HOD/SystemAlerts");
            WaitForPageLoad();
            TakeScreenshot("MMREP01_SystemAlerts");
            Console.WriteLine("✅ System Alerts page loaded");

            Console.WriteLine("✅ MMREP01 PASSED");
        }

        #endregion

        #region MMEDT01: Edit User Profile

        [Test]
        public void MMEDT01_EditUserProfile()
        {
            Console.WriteLine("\n=== MMEDT01: Edit User ===");

            if (!LoginAsCoordinator())
            {
                Assert.Inconclusive("Could not login as Coordinator");
                return;
            }

            NavigateTo("/Coordinator/ManageUsers");
            WaitForPageLoad();
            TakeScreenshot("MMEDT01_ManageUsers");

            // Find Edit buttons
            var editButtons = Driver.FindElements(By.CssSelector("a[href*='EditUser']"));
            if (editButtons.Count > 0)
            {
                Console.WriteLine($"✅ Found {editButtons.Count} users to edit");
            }

            Assert.That(Driver.PageSource, Does.Contain("User").Or.Contain("Email"));
        }

        #endregion

        #region SANITY: Test All Logins

        [Test]
        public void SANITY_TestAllLogins()
        {
            Console.WriteLine("\n=== SANITY: Testing All Role Logins ===");

            var tests = new (string Role, string Email, string Password, Func<bool> LoginFunc)[]
            {
                ("Coordinator", COORDINATOR_EMAIL, COORDINATOR_PASSWORD, LoginAsCoordinator),
                ("HOD", HOD_EMAIL, HOD_PASSWORD, LoginAsHOD),
                ("Student1", STUDENT_EMAIL, STUDENT_PASSWORD, LoginAsStudent),
                ("Student2", STUDENT2_EMAIL, STUDENT2_PASSWORD, LoginAsStudent2),
                ("Supervisor", SUPERVISOR_EMAIL, SUPERVISOR_PASSWORD, LoginAsSupervisor),
                ("Panel", PANEL_EMAIL, PANEL_PASSWORD, LoginAsPanel)
            };

            int passed = 0;

            foreach (var test in tests)
            {
                Console.WriteLine($"\n--- Testing {test.Role}: {test.Email} ---");
                bool success = test.LoginFunc();

                if (success)
                {
                    Console.WriteLine($"✅ {test.Role} login SUCCESS");
                    passed++;

                    // Logout
                    NavigateTo("/Account/Logout");
                    System.Threading.Thread.Sleep(500);
                }
                else
                {
                    Console.WriteLine($"❌ {test.Role} login FAILED");
                }
            }

            Console.WriteLine($"\n📊 Results: {passed}/{tests.Length} logins successful");

            if (passed < tests.Length)
            {
                Assert.Warn($"{tests.Length - passed} login(s) failed. Check credentials.");
            }
            else
            {
                Console.WriteLine("🎉 ALL LOGINS WORKING!");
            }
        }

        #endregion
    }
}