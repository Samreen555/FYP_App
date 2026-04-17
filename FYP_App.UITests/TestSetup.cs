using System.Diagnostics;

namespace FYP_App.UITests
{
    [SetUpFixture]
    public class TestSetup
    {
        private static Process? _appProcess; // Made nullable

        [OneTimeSetUp]
        public void StartApplication()
        {
            // Try to find the application path
            var solutionDir = FindSolutionDir();
            if (solutionDir != null)
            {
                var appPath = Path.Combine(solutionDir, "FYP_App", "bin", "Debug", "net8.0", "FYP_App.exe");

                if (File.Exists(appPath))
                {
                    _appProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = appPath,
                            UseShellExecute = true,
                            CreateNoWindow = false
                        }
                    };
                    _appProcess.Start();

                    // Wait for app to start
                    System.Threading.Thread.Sleep(5000);
                }
                else
                {
                    Console.WriteLine($"App not found at: {appPath}");
                    Console.WriteLine("Please start the application manually before running tests.");
                }
            }
        }

        [OneTimeTearDown]
        public void StopApplication()
        {
            try
            {
                _appProcess?.Kill();
                _appProcess?.Dispose();
            }
            catch { }
        }

        private string? FindSolutionDir()
        {
            var dir = TestContext.CurrentContext.TestDirectory;
            while (!string.IsNullOrEmpty(dir) && !Directory.GetFiles(dir, "*.sln").Any())
            {
                dir = Path.GetDirectoryName(dir);
            }
            return dir;
        }
    }
}