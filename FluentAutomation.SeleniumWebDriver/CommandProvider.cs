﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using FluentAutomation.Exceptions;
using FluentAutomation.Interfaces;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace FluentAutomation
{
    public class CommandProvider : BaseCommandProvider, ICommandProvider, IDisposable
    {
        private readonly IFileStoreProvider fileStoreProvider = null;
        private readonly Lazy<IWebDriver> lazyWebDriver = null;
        private IWebDriver webDriver
        {
            get
            {
                return lazyWebDriver.Value;
            }
        }

        private string mainWindowHandle = null;

        public CommandProvider(Func<IWebDriver> webDriverFactory, IFileStoreProvider fileStoreProvider)
        {
            FluentTest.ProviderInstance = null;

            this.lazyWebDriver = new Lazy<IWebDriver>(() =>
            {
                var webDriver = webDriverFactory();
                if (FluentTest.ProviderInstance == null)
                    FluentTest.ProviderInstance = webDriver;
                else
                    FluentTest.IsMultiBrowserTest = true;

                webDriver.Manage().Cookies.DeleteAllCookies();
                webDriver.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(10));

                if (FluentSettings.Current.WindowHeight.HasValue && FluentSettings.Current.WindowWidth.HasValue)
                {
                    webDriver.Manage().Window.Size = new Size(FluentSettings.Current.WindowWidth.Value, FluentSettings.Current.WindowHeight.Value);
                }

                this.mainWindowHandle = webDriver.CurrentWindowHandle;

                return webDriver;
            });

            this.fileStoreProvider = fileStoreProvider;
        }

        public Uri Url
        {
            get
            {
                return new Uri(this.webDriver.Url, UriKind.Absolute);
            }
        }

        public string Source
        {
            get
            {
                return this.webDriver.PageSource;
            }
        }

        public void Navigate(Uri url)
        {
            this.Act(CommandType.Action, () => this.webDriver.Navigate().GoToUrl(url));
        }

        public ElementProxy Find(string selector)
        {
            return new ElementProxy(this, () =>
            {
                try
                {
                    var webElement = this.webDriver.FindElement(Sizzle.Find(selector));
                    return new Element(webElement, selector);
                }
                catch (NoSuchElementException)
                {
                    throw new FluentException("Unable to find element with selector [{0}]", selector);
                }
            });
        }

        public ElementProxy FindMultiple(string selector)
        {
            var finalResult = new ElementProxy();

            finalResult.Children.Add(new Func<ElementProxy>(() =>
            {
                try
                {
                    var result = new ElementProxy();
                    var webElements = this.webDriver.FindElements(Sizzle.Find(selector));
                    foreach (var element in webElements)
                    {
                        result.Elements.Add(this, () => new Element(element, selector));
                    }

                    return result;
                }
                catch (NoSuchElementException)
                {
                    throw new FluentException("Unable to find element with selector [{0}]", selector);
                }
            }));

            return finalResult;
        }

        public void Click(int x, int y)
        {
            this.Act(CommandType.Action, () =>
            {
                var rootElement = this.Find("html").Element as Element;
                new Actions(this.webDriver)
                    .MoveToElement(rootElement.WebElement, x, y)
                    .Click()
                    .Perform();
            });
        }

        public void Click(ElementProxy element, int x, int y)
        {
            this.Act(CommandType.Action, () =>
            {
                var containerElement = element.Element as Element;
                new Actions(this.webDriver)
                    .MoveToElement(containerElement.WebElement, x, y)
                    .Click()
                    .Perform();
            });
        }

        public void Click(ElementProxy element)
        {
            this.Act(CommandType.Action, () =>
            {
                var containerElement = element.Element as Element;
                new Actions(this.webDriver)
                    .Click(containerElement.WebElement)
                    .Perform();
            });
        }

        public void DoubleClick(int x, int y)
        {
            this.Act(CommandType.Action, () =>
            {
                var rootElement = this.Find("html").Element as Element;
                new Actions(this.webDriver)
                    .MoveToElement(rootElement.WebElement, x, y)
                    .DoubleClick()
                    .Perform();
            });
        }

        public void DoubleClick(ElementProxy element, int x, int y)
        {
            this.Act(CommandType.Action, () =>
            {
                var containerElement = element.Element as Element;
                new Actions(this.webDriver)
                    .MoveToElement(containerElement.WebElement, x, y)
                    .DoubleClick()
                    .Perform();
            });
        }

        public void DoubleClick(ElementProxy element)
        {
            this.Act(CommandType.Action, () =>
            {
                var containerElement = element.Element as Element;
                new Actions(this.webDriver)
                    .DoubleClick(containerElement.WebElement)
                    .Perform();
            });
        }
        
        public void RightClick(ElementProxy element)
        {
            this.Act(CommandType.Action, () =>
            {
                var containerElement = element.Element as Element;
                new Actions(this.webDriver)
                    .ContextClick(containerElement.WebElement)
                    .Perform();
            });
        }

        public void Hover(int x, int y)
        {
            this.Act(CommandType.Action, () =>
            {
                var rootElement = this.Find("html").Element as Element;
                new Actions(this.webDriver)
                    .MoveToElement(rootElement.WebElement, x, y)
                    .Perform();
            });
        }

        public void Hover(ElementProxy element, int x, int y)
        {
            this.Act(CommandType.Action, () =>
            {
                var containerElement = element.Element as Element;
                new Actions(this.webDriver)
                    .MoveToElement(containerElement.WebElement, x, y)
                    .Perform();
            });
        }

        public void Hover(ElementProxy element)
        {
            this.Act(CommandType.Action, () =>
            {
                var unwrappedElement = element.Element as Element;
                new Actions(this.webDriver)
                    .MoveToElement(unwrappedElement.WebElement)
                    .Perform();
            });
        }

        public void Focus(ElementProxy element)
        {
            this.Act(CommandType.Action, () =>
            {
                var unwrappedElement = element.Element as Element;

                switch (unwrappedElement.WebElement.TagName)
                {
                    case "input":
                    case "select":
                    case "textarea":
                    case "a":
                    case "iframe":
                    case "button":
                        var executor = (IJavaScriptExecutor)this.webDriver;
                        executor.ExecuteScript("arguments[0].focus();", unwrappedElement.WebElement);
                        break;
                }
            });
        }

        public void DragAndDrop(int sourceX, int sourceY, int destinationX, int destinationY)
        {
            this.Act(CommandType.Action, () =>
            {
                var rootElement = this.Find("html").Element as Element;
                new Actions(this.webDriver)
                    .MoveToElement(rootElement.WebElement, sourceX, sourceY)
                    .ClickAndHold()
                    .MoveToElement(rootElement.WebElement, destinationX, destinationY)
                    .Release()
                    .Perform();
            });
        }

        public void DragAndDrop(ElementProxy source, int sourceOffsetX, int sourceOffsetY, ElementProxy target, int targetOffsetX, int targetOffsetY)
        {
            this.Act(CommandType.Action, () =>
            {
                var element = source.Element as Element;
                var targetElement = target.Element as Element;
                new Actions(this.webDriver)
                    .MoveToElement(element.WebElement, sourceOffsetX, sourceOffsetY)
                    .ClickAndHold()
                    .MoveToElement(targetElement.WebElement, targetOffsetX, targetOffsetY)
                    .Release()
                    .Perform();
            });
        }

        public void DragAndDrop(ElementProxy source, ElementProxy target)
        {
            this.Act(CommandType.Action, () =>
            {
                var unwrappedSource = source.Element as Element;
                var unwrappedTarget = target.Element as Element;

                new Actions(this.webDriver)
                    .DragAndDrop(unwrappedSource.WebElement, unwrappedTarget.WebElement)
                    .Perform();
            });
        }

        public void EnterText(ElementProxy element, string text)
        {
            this.Act(CommandType.Action, () =>
            {
                var unwrappedElement = element.Element as Element;

                unwrappedElement.WebElement.Clear();
                unwrappedElement.WebElement.SendKeys(text);
            });
        }

        public void EnterTextWithoutEvents(ElementProxy element, string text)
        {
            this.Act(CommandType.Action, () =>  
            {
                var unwrappedElement = element.Element as Element;

                ((IJavaScriptExecutor)this.webDriver).ExecuteScript(string.Format("if (typeof fluentjQuery != 'undefined') {{ fluentjQuery(\"{0}\").val(\"{1}\").trigger('change'); }}", unwrappedElement.Selector.Replace("\"", ""), text.Replace("\"", "")));
            });
        }

        public void AppendText(ElementProxy element, string text)
        {
            this.Act(CommandType.Action, () =>
            {
                var unwrappedElement = element.Element as Element;
                unwrappedElement.WebElement.SendKeys(text);
            });
        }

        public void AppendTextWithoutEvents(ElementProxy element, string text)
        {
            this.Act(CommandType.Action, () =>
            {
                var unwrappedElement = element.Element as Element;
                ((IJavaScriptExecutor)this.webDriver).ExecuteScript(string.Format("if (typeof fluentjQuery != 'undefined') {{ fluentjQuery(\"{0}\").val(fluentjQuery(\"{0}\").val() + \"{1}\").trigger('change'); }}", unwrappedElement.Selector.Replace("\"", ""), text.Replace("\"", "")));
            });
        }

        public void SelectText(ElementProxy element, string optionText)
        {
            this.Act(CommandType.Action, () =>
            {
                var unwrappedElement = element.Element as Element;

                SelectElement selectElement = new SelectElement(unwrappedElement.WebElement);
                if (selectElement.IsMultiple) selectElement.DeselectAll();
                selectElement.SelectByText(optionText);
            });
        }

        public void MultiSelectValue(ElementProxy element, string[] optionValues)
        {
            this.Act(CommandType.Action, () =>
            {
                var unwrappedElement = element.Element as Element;

                SelectElement selectElement = new SelectElement(unwrappedElement.WebElement);
                if (selectElement.IsMultiple) selectElement.DeselectAll();

                foreach (var optionValue in optionValues)
                {
                    selectElement.SelectByValue(optionValue);
                }
            });
        }

        public void MultiSelectIndex(ElementProxy element, int[] optionIndices)
        {
            this.Act(CommandType.Action, () =>
            {
                var unwrappedElement = element.Element as Element;

                SelectElement selectElement = new SelectElement(unwrappedElement.WebElement);
                if (selectElement.IsMultiple) selectElement.DeselectAll();

                foreach (var optionIndex in optionIndices)
                {
                    selectElement.SelectByIndex(optionIndex);
                }
            });
        }

        public void MultiSelectText(ElementProxy element, string[] optionTextCollection)
        {
            this.Act(CommandType.Action, () =>
            {
                var unwrappedElement = element.Element as Element;

                SelectElement selectElement = new SelectElement(unwrappedElement.WebElement);
                if (selectElement.IsMultiple) selectElement.DeselectAll();

                foreach (var optionText in optionTextCollection)
                {
                    selectElement.SelectByText(optionText);
                }
            });
        }

        public void SelectValue(ElementProxy element, string optionValue)
        {
            this.Act(CommandType.Action, () =>
            {
                var unwrappedElement = element.Element as Element;

                SelectElement selectElement = new SelectElement(unwrappedElement.WebElement);
                if (selectElement.IsMultiple) selectElement.DeselectAll();
                selectElement.SelectByValue(optionValue);
            });
        }

        public void SelectIndex(ElementProxy element, int optionIndex)
        {
            this.Act(CommandType.Action, () =>
            {
                var unwrappedElement = element.Element as Element;

                SelectElement selectElement = new SelectElement(unwrappedElement.WebElement);
                if (selectElement.IsMultiple) selectElement.DeselectAll();
                selectElement.SelectByIndex(optionIndex);
            });
        }

        public override void TakeScreenshot(string screenshotName)
        {
            this.Act(CommandType.Action, () =>
            {
                // get raw screenshot
                var screenshotDriver = (ITakesScreenshot)this.webDriver;
                var tmpImagePath = Path.Combine(FluentSettings.Current.UserTempDirectory, screenshotName);
                screenshotDriver.GetScreenshot().SaveAsFile(tmpImagePath, ImageFormat.Png);

                // save to file store
                this.fileStoreProvider.SaveScreenshot(File.ReadAllBytes(tmpImagePath), screenshotName);
                File.Delete(tmpImagePath);
            });
        }

        public void UploadFile(ElementProxy element, int x, int y, string fileName)
        {
            this.Act(CommandType.Action, () =>
            {
                // wait before typing in the field
                var task = Task.Factory.StartNew(() =>
                {
                    this.Type(fileName);
                });

                if (x == 0 && y == 0)
                {
                    this.Click(element);
                }
                else
                {
                    this.Click(element, x, y);
                }

                task.Wait();
                this.Wait(TimeSpan.FromMilliseconds(1500));
            });
        }

        public void Press(string keys)
        {
            this.Act(CommandType.Action, () => System.Windows.Forms.SendKeys.SendWait(keys));
        }

        public void Type(string text)
        {
            this.Act(CommandType.Action, () =>
            {
                foreach (var character in text)
                {
                    System.Windows.Forms.SendKeys.SendWait(character.ToString());
                    this.Wait(TimeSpan.FromMilliseconds(20));
                }
            });
        }

        public void SwitchToWindow(string windowName)
        {
            this.Act(CommandType.Action, () =>
            {
                if (windowName == string.Empty)
                {
                    this.webDriver.SwitchTo().Window(this.mainWindowHandle);
                    return;
                }

                var matchFound = false;
                foreach (var windowHandle in this.webDriver.WindowHandles)
                {
                    this.webDriver.SwitchTo().Window(windowHandle);

                    if (this.webDriver.Title == windowName || this.webDriver.Url.EndsWith(windowName))
                    {
                        matchFound = true;
                        break;
                    }
                }

                if (!matchFound)
                {
                    throw new FluentException("No window with a title or URL matching [{0}] could be found.", windowName);
                }
            });
        }

        public void SwitchToFrame(string frameNameOrSelector)
        {
            this.Act(CommandType.Action, () =>
            {
                if (frameNameOrSelector == string.Empty)
                {
                    this.webDriver.SwitchTo().DefaultContent();
                    return;
                }

                // try to locate frame using argument as a selector, if that fails pass it into Frame so it can be
                // evaluated as a name by Selenium
                IWebElement frameBySelector = null;
                try
                {
                    frameBySelector = this.webDriver.FindElement(Sizzle.Find(frameNameOrSelector));
                }
                catch (NoSuchElementException)
                {
                }

                if (frameBySelector == null)
                    this.webDriver.SwitchTo().Frame(frameNameOrSelector);
                else
                    this.webDriver.SwitchTo().Frame(frameBySelector);
            });
        }

        public void SwitchToFrame(ElementProxy frameElement)
        {
            this.Act(CommandType.Action, () =>
            {
                this.webDriver.SwitchTo().Frame((frameElement.Element as Element).WebElement);
            });
        }

        public void AlertClick(Alert accessor)
        {
            var alert = this.webDriver.SwitchTo().Alert();

            if (accessor.Field == AlertField.OKButton)
                alert.Accept();
            else if (accessor.Field == AlertField.CancelButton)
                alert.Dismiss();
            else
                throw new FluentException("FluentAutomation only supports clicking on OK or Cancel in alerts or prompts.");
        }

        public void AlertText(Action<string> matchFunc)
        {
            var alert = this.webDriver.SwitchTo().Alert();
            matchFunc(alert.Text);
        }

        public void AlertEnterText(string text)
        {
            var alert = this.webDriver.SwitchTo().Alert();
            alert.SendKeys(text);
        }

        public void Dispose()
        {
            try
            {
                this.webDriver.Manage().Cookies.DeleteAllCookies();
                this.webDriver.Quit();
                this.webDriver.Dispose();
            }
            catch (Exception) { }
        }
    }
}
