using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Serialization;
using AutoUpdaterWPF.Exceptions;

namespace AutoUpdaterWPF
{
    public enum RemindLaterFormat
    {
        Minutes,
        Hours,
        Days
    }

    /// <summary>
    ///     Main class that lets you auto update applications by setting some static fields and executing its Start method.
    /// </summary>
    public static class AutoUpdater
    {
        public static string DialogTitle = string.Empty;

        public static string ChangeLogUrl = string.Empty;

        public static string DownloadUrl = string.Empty;

        public static string LocalFileName = string.Empty;

        public static string RegistryLocation = string.Empty;

        public static string AppTitle = string.Empty;

        public static Version CurrentVersion;

        public static Version InstalledVersion;

        public static bool ForceCheck;

        public static Dispatcher MainDispatcher;

        public static bool Running;

        /// <summary>
        ///     URL of the xml file that contains information about latest version of the application.
        /// </summary>
        public static string AppCastUrl;

        /// <summary>
        ///     Opens the download url in default browser if true. Very usefull if you have portable application.
        /// </summary>
        public static bool OpenDownloadPage = false;

        /// <summary>
        ///     If this is true users see dialog where they can set remind later interval otherwise it will take the interval from
        ///     RemindLaterAt and RemindLaterTimeSpan fields.
        /// </summary>
        public static bool LetUserSelectRemindLater = true;

        /// <summary>
        ///     Remind Later interval after user should be reminded of update.
        /// </summary>
        public static int RemindLaterAt = 2;

        /// <summary>
        ///     Set if RemindLaterAt interval should be in Minutes, Hours or Days.
        /// </summary>
        public static RemindLaterFormat RemindLaterTimeSpan = RemindLaterFormat.Days;

        /// <summary>
        /// Proxy to use for download (or null for none)
        /// </summary>
        public static IWebProxy Proxy;

        /// <summary>
        /// Action to perform when update check is finished. Parameter sent in represents if update was found.
        /// </summary>
        public static Action<bool> UpdateCheckFinished;

        /// <summary>
        ///     Start checking for new version of application and display dialog to the user if update is available.
        /// </summary>
        /// <param name="forceUpdate">If true, ignores remind later and checks for update right away</param>
        public static Task Start(bool forceUpdate = false)
        {
            return Start(AppCastUrl, forceUpdate);
        }

        /// <summary>
        ///     Start checking for new version of application and display dialog to the user if update is available.
        /// </summary>
        /// <param name="appCast">URL of the xml file that contains information about latest version of the application.</param>
        /// <param name="forceUpdate">If true, ignores remind later and checks for update right away</param>
        public static Task Start(string appCast, bool forceUpdate = false)
        {
            if (Running)
                throw new AlreadyRunningException();

            AppCastUrl = appCast;
            ForceCheck = forceUpdate;
            Running = true;

            return CheckForUpdate().ContinueWith(task => Running = false);
        }

        private static async Task CheckForUpdate()
        {
            var mainAssembly = Assembly.GetEntryAssembly();
            var companyAttribute =
                (AssemblyCompanyAttribute)GetAttribute(mainAssembly, typeof(AssemblyCompanyAttribute));
            var titleAttribute = (AssemblyTitleAttribute)GetAttribute(mainAssembly, typeof(AssemblyTitleAttribute));
            AppTitle = titleAttribute != null ? titleAttribute.Title : mainAssembly.GetName().Name;
            var appCompany = companyAttribute != null ? companyAttribute.Company : "";

            RegistryLocation = !string.IsNullOrEmpty(appCompany)
                ? $@"Software\{appCompany}\{AppTitle}\AutoUpdater"
                : $@"Software\{AppTitle}\AutoUpdater";

            RegistryKey updateKey = null;
            object skip = null;
            object applicationVersion = null;
            object remindLaterTime = null;

            try
            {
                updateKey = Registry.CurrentUser.OpenSubKey(RegistryLocation);

                if (updateKey != null)
                {
                    skip = updateKey.GetValue("skip");
                    applicationVersion = updateKey.GetValue("version");
                    remindLaterTime = updateKey.GetValue("remindlater");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following exception occurred trying to retrieve update settings: " + ex.Message);
            }
            finally
            {
                updateKey?.Close();
            }

            if (ForceCheck == false && remindLaterTime != null)
            {
                var remindLater = Convert.ToDateTime(remindLaterTime.ToString(),
                    CultureInfo.CreateSpecificCulture("en-US"));

                var compareResult = DateTime.Compare(DateTime.Now, remindLater);

                if (compareResult < 0)
                {
                    var updateForm = new Update(true);
                    updateForm.SetTimer(remindLater);
                    return;
                }
            }

            var fileVersionAttribute =
                (AssemblyFileVersionAttribute)GetAttribute(mainAssembly, typeof(AssemblyFileVersionAttribute));
            InstalledVersion = new Version(fileVersionAttribute.Version);

            var webRequest = WebRequest.Create(AppCastUrl);
            webRequest.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);

            if (Proxy != null)
                webRequest.Proxy = Proxy;

            WebResponse webResponse;

            try
            {
                webResponse = await webRequest.GetResponseAsync();
            }
            catch (Exception ex)
            {
                if (MainDispatcher == null)
                    return;

                if (!ForceCheck)
                    return;

                // Only display errors if user requested update check

                if (ex is WebException)
                    throw new ConnectException("An error occurred connecting to the update server. Please check that you're connected to the internet and (if applicable) your proxy settings are correct.", ex);
                else
                    throw new ConnectException(ex);
            }

            UpdateXml updateXml;

            Stream appCastStream = null;
            XmlTextReader reader = null;

            try
            {
                appCastStream = webResponse.GetResponseStream();

                if (appCastStream == null)
                    throw new Exception("Response stream from update server was null.");

                var serializer = new XmlSerializer(typeof(UpdateXml));

                reader = new XmlTextReader(appCastStream);

                if (serializer.CanDeserialize(reader))
                    updateXml = (UpdateXml)serializer.Deserialize(reader);
                else
                    throw new Exception("Update file is in the wrong format.");
            }
            catch (Exception ex)
            {
                throw new ReadFileException(ex);
            }
            finally
            {
                reader.Close();

                appCastStream.Close();

                webResponse.Close();
            }

            foreach (var item in updateXml.Items)
            {
                if (item.Version != null)
                {
                    if (item.Version <= InstalledVersion)
                        continue;

                    CurrentVersion = item.Version;
                }
                else
                    continue;

                DialogTitle = item.Title;
                ChangeLogUrl = item.ChangeLog;
                DownloadUrl = item.Url;
                LocalFileName = item.FileName;
            }

            if (CurrentVersion != null && CurrentVersion > InstalledVersion)
            {
                if (skip != null && applicationVersion != null)
                {
                    var skipValue = skip.ToString();
                    var skipVersion = new Version(applicationVersion.ToString());

                    if (skipValue.Equals("1") && CurrentVersion <= skipVersion)
                        return;

                    if (CurrentVersion > skipVersion)
                    {
                        RegistryKey updateKeyWrite = null;

                        try
                        {
                            updateKeyWrite = Registry.CurrentUser.CreateSubKey(RegistryLocation);

                            if (updateKeyWrite != null)
                            {
                                updateKeyWrite.SetValue("version", CurrentVersion.ToString());
                                updateKeyWrite.SetValue("skip", 0);
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new SaveFileException(ex);
                        }
                        finally
                        {
                            updateKeyWrite?.Close();
                        }
                    }
                }

                UpdateCheckFinished?.Invoke(true);

                await Application.Current.Dispatcher.InvokeAsync(ShowUI);
            }
            else
            {
                UpdateCheckFinished?.Invoke(false);
            }
        }

        private static void ShowUI()
        {
            var updateForm = new Update();

            updateForm.Show();

            // Focus window (so it's not hidden behind main window)
            updateForm.Topmost = true;
            updateForm.Topmost = false;
            updateForm.Focus();
        }

        private static Attribute GetAttribute(ICustomAttributeProvider assembly, Type attributeType)
        {
            var attributes = assembly.GetCustomAttributes(attributeType, false);
            if (attributes.Length == 0)
            {
                return null;
            }
            return (Attribute)attributes[0];
        }
    }
}