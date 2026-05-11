using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using FluentAssertions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace DatesAndStuff.Web.Tests;

[TestFixture]
public class BlazeDemo
{
    private IWebDriver driver;
    private StringBuilder verificationErrors;
    private const string BaseURL = "https://blazedemo.com";
    private bool acceptNextAlert = true;

    private Process? _blazorProcess;

    [OneTimeSetUp]
    public void StartBlazorServer()
    {
        var webProjectPath = Path.GetFullPath(Path.Combine(
            Assembly.GetExecutingAssembly().Location,
            "../../../../../../src/DatesAndStuff.Web/DatesAndStuff.Web.csproj"
            ));

        var webProjFolderPath = Path.GetDirectoryName(webProjectPath);

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            //Arguments = $"run --project \"{webProjectPath}\"",
            Arguments = "dotnet run --no-build",
            WorkingDirectory = webProjFolderPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        _blazorProcess = Process.Start(startInfo);

        // Wait for the app to become available
        var client = new HttpClient();
        var timeout = TimeSpan.FromSeconds(30);
        var start = DateTime.Now;

        while (DateTime.Now - start < timeout)
        {
            try
            {
                var result = client.GetAsync(BaseURL).Result;
                if (result.IsSuccessStatusCode)
                {
                    break;
                }
            }
            catch (Exception e)
            {
                Thread.Sleep(1000);
            }
        }
    }

    [OneTimeTearDown]
    public void StopBlazorServer()
    {
        if (_blazorProcess != null && !_blazorProcess.HasExited)
        {
            _blazorProcess.Kill(true);
            _blazorProcess.Dispose();
        }
    }

    [SetUp]
    public void SetupTest()
    {
        driver = new ChromeDriver();
        verificationErrors = new StringBuilder();
    }

    [TearDown]
    public void TeardownTest()
    {
        try
        {
            driver.Quit();
            driver.Dispose();
        }
        catch (Exception)
        {
            // Ignore errors if unable to close the browser
        }
        Assert.That(verificationErrors.ToString(), Is.EqualTo(""));
    }

    [Test]
    public void FlightSearch_BetweenMexicoCityAndDublin_ShouldHaveAtLeastThreeFlights()
    {
        // Arrange
        driver.Navigate().GoToUrl(BaseURL);
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        
        var fromPort = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("fromPort")));
        fromPort.Click();
        fromPort.FindElement(By.XPath("//option[@value='Mexico City']")).Click();
        
        var toPort = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("toPort")));
        toPort.Click();
        toPort.FindElement(By.XPath("//option[@value='Dublin']")).Click();
        
        var submitButton = driver.FindElement(By.CssSelector("input.btn-primary"));
        submitButton.Click();

        wait.Until(ExpectedConditions.ElementIsVisible(By.TagName("table")));
        var flightRows = driver.FindElements(By.CssSelector("table.table tbody tr"));

        int flightCount = flightRows.Count;
        flightCount.Should().BeGreaterThanOrEqualTo(3, $"there should be 3 flights between Mexico City and Dublin, but found {flightCount}");
    }
}