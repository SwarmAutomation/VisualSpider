﻿using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VSEngine.Data;

namespace VSEngine
{
    /// <summary>
    /// The tread that takes care of the heavy lifting
    /// </summary>
    public class NavThread
    {
        // navigate to url
        // collect all links
        // take screen shot
        NavUnit UnitToPreform { get; set; }
        public ResolvedNavUnit UnitToPassBack { get; set; }
        public Config configRef { get; set; }
        public bool CollectLinks { get; set; }

        public NavThread(NavUnit unit)
        {
            UnitToPreform = unit;
            CollectLinks = true;
        }

        public void Navigate()
        {
            //if config is chrome
            //ChromeOptions Coptions = new ChromeOptions();
            //Coptions.AddArgument("--silent");
            //Coptions.SetLoggingPreference(LogType.Driver, LogLevel.Off);
            IWebDriver Driver = new FirefoxDriver();

            // Navigate to the Starting URL
            Driver.Navigate().GoToUrl(UnitToPreform.Address);
            Uri resolvedURL = new Uri(Driver.Url);
            byte[] screenShoot = ((ITakesScreenshot)Driver).GetScreenshot().AsByteArray;
            string contentMD5 = NavUnit.CalculateMD5Hash(Driver.PageSource);

            // generate a new resolved unit
            ResolvedNavUnit resolvedUnit = new ResolvedNavUnit(UnitToPreform, resolvedURL, screenShoot, contentMD5);

            // gather links on the page 
            if (CollectLinks)
            {
                List<IWebElement> urls = Driver.FindElements(By.CssSelector("a[href]")).ToList();


                // fill the resolved units url results
                foreach (IWebElement currentEle in urls)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(currentEle.GetAttribute("href")))
                        {
                            resolvedUnit.URLSFound.Add(currentEle.GetAttribute("href"));
                        }
                    }
                    catch (Exception e)
                    {
                        resolvedUnit.NavigationErrors.Add("Webdriver error on " + resolvedUnit.Address + ": " + e.ToString());
                    }
                }
            }

            // assign the resolved unit to pass back
            UnitToPassBack = resolvedUnit;

            // close down the browser and clean up
            Driver.Close();
        }
    }
}
