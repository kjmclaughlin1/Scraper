using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Threading;

namespace Scraper.Class
{
    class FinalHtml
    {
        /// <summary>
        /// Get the page after pulling the scrollbar
        /// </summary>
        /// <param name="url">web site </param>
        /// <param name="sectionNum">scroll several times </param>
        /// <returns>html string </returns>
        public static string GetFinalHtml(string url, int sectionNum)
        {
            //Do not start the chrome window
            ChromeOptions options = new ChromeOptions();
            options.AddArgument("headless");

            //Close the Chrome Driver console
            ChromeDriverService driverService = ChromeDriverService.CreateDefaultService();
            driverService.HideCommandPromptWindow = true;


            ChromeDriver driver = new ChromeDriver(driverService, options);

            driver.Navigate().GoToUrl(url);

            for (int i = 1; i <= sectionNum; i++)
            {
                string jsCode = "window.scrollTo({top: document.body.scrollHeight / " + sectionNum + " * " + i + ", behavior: \"smooth\"});";
                IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                js.ExecuteScript(jsCode);
                Thread.Sleep(1000);
            }

            string html = driver.PageSource;
            driver.Quit();

            return html;
        }
    }
}
