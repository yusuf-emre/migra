using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Twilio;
using Twilio.Types;
using Twilio.Rest.Api.V2010.Account;
using System.Net;
using System.Net.Mail;

namespace Migra
{
    public class Migra
    {
        private readonly ILogger _logger;

        public Migra(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Migra>();
        }

        [Function("Migra")]
        public void Run([TimerTrigger("0 0 * * * *")] TimerInfo myTimer)
        {
            string driverPath = Path.Combine(Environment.CurrentDirectory, "Drivers");

            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--headless");

            // Initialize the ChromeDriver
            IWebDriver driver = new ChromeDriver(driverPath, options);

            try
            {
                string caseNumberOne = Environment.GetEnvironmentVariable("CASE_NUMBER_ONE");
                string caseNumberTwo = Environment.GetEnvironmentVariable("CASE_NUMBER_TWO");

                string[] caseNumbers = { caseNumberOne, caseNumberTwo };

                foreach (string caseNumber in caseNumbers)
                {
                    // Navigate to the web page
                    driver.Navigate().GoToUrl("https://www.migrationsverket.se/Kontakta-oss/Kontrollera-din-ansokan-utan-att-logga-in.html");

                    // Find element, click and type in
                    IWebElement caseNumberInput = driver.FindElement(By.CssSelector("[class='env-form-input env-border--r-0-tablet-up env-border-radius--r-0-tablet-up']"));
                    caseNumberInput.Click();
                    caseNumberInput.SendKeys(caseNumber);

                    // Submit the form
                    IWebElement submitButton = driver.FindElement(By.ClassName("env-button--primary"));
                    submitButton.Click();

                    // Wait for the result page to load
                    System.Threading.Thread.Sleep(1000);

                    // Extract the text of a specific element on the result page
                    IWebElement parentDiv = driver.FindElement(By.ClassName("message-box"));
                    IWebElement ulElement = parentDiv.FindElement(By.TagName("ul"));
                    IList<IWebElement> liElements = ulElement.FindElements(By.TagName("li"));
                    string resultText = liElements[1].Text;
                    resultText = resultText.Remove(resultText.Length - 1, 1) + (caseNumber == caseNumberOne ? " Asaf" : " Yusuf");

                    // if (resultText != "Migrationsverket har inte fattat ett beslut i din ansökan.")
                    // {
                    // Send the result
                    SendSMS(resultText);
                    // SendEmail(resultText);
                    // }
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Error: " + ex.Message);
            }
            finally
            {
                // Close the browser
                driver.Quit();
            }

            void SendSMS(string resultText)
            {
                // Twilio account credentials
                string accountSid = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID");
                string authToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN");

                string fromPhoneNumber = Environment.GetEnvironmentVariable("FROM_PHONE_NUMBER");
                string toPhoneNumber = Environment.GetEnvironmentVariable("TO_PHONE_NUMBER");

                // Initialize the Twilio client
                TwilioClient.Init(accountSid, authToken);

                // Send the SMS
                MessageResource.Create(
                    body: resultText,
                    from: new PhoneNumber(fromPhoneNumber),
                    to: new PhoneNumber(toPhoneNumber)
                );
            }

            void SendEmail(string resultText)
            {
                string smtpServer = "smtp.gmail.com";
                int smtpPort = 587;
                string senderEmail = Environment.GetEnvironmentVariable("SENDER_EMAIL");
                string senderPassword = Environment.GetEnvironmentVariable("SENDER_EMAIL_PASSWORD");
                string recipientEmail = Environment.GetEnvironmentVariable("RECIPIENT_EMAIL");

                // Create a new email message
                MailMessage mail = new(senderEmail, recipientEmail)
                {
                    Subject = resultText
                };

                SmtpClient client = new(smtpServer)
                {
                    Port = smtpPort,
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = true
                };

                try
                {
                    client.Send(mail);
                    _logger.LogInformation("Email sent successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogInformation("Email Error: " + ex.Message);
                }
            }

            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }
    }
}
