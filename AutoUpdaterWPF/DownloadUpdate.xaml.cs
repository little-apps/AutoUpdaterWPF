﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Threading;
using System.Windows;
using AutoUpdaterWPF.Exceptions;

namespace AutoUpdaterWPF
{
    /// <summary>
    ///     Interaction logic for DownloadUpdate.xaml
    /// </summary>
    internal partial class DownloadUpdate
    {
        private const uint MaxRedirects = 20;
        private readonly string _downloadUrl;

        private string _tempPath;

        public DownloadUpdate(string downloadUrl)
        {
            InitializeComponent();

            _downloadUrl = downloadUrl;
        }

        private void DownloadUpdateDialogLoad(object sender, RoutedEventArgs e)
        {
            var webClient = new WebClient();

            var uri = new Uri(_downloadUrl);

            string fileName;

            if (!string.IsNullOrEmpty(AutoUpdater.LocalFileName))
            {
                fileName = AutoUpdater.LocalFileName;
            }
            else
            {
                fileName = GetFileName(_downloadUrl, 0);

                if (string.IsNullOrEmpty(fileName))
                {
                    Close();

                    throw new DownloadFailedException(_downloadUrl, new Exception("Local filename couldn't be determined"));
                }
            }

            _tempPath = $@"{Path.GetTempPath()}{fileName}";

            webClient.DownloadProgressChanged += OnDownloadProgressChanged;
            webClient.DownloadFileCompleted += OnDownloadComplete;

            webClient.DownloadFileAsync(uri, _tempPath);
        }

        private void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            ProgressBar.Value = e.ProgressPercentage;
        }

        private void OnDownloadComplete(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                Close();

                throw new DownloadFailedException(AutoUpdater.DownloadUrl, e.Error);
            }

            var processStartInfoDownloadedFile = new ProcessStartInfo { FileName = _tempPath, UseShellExecute = true };

            Process.Start(processStartInfoDownloadedFile);

            if (Application.Current.Dispatcher.Thread == Thread.CurrentThread) // Check if were on the main thread
            {
                Application.Current.Shutdown();
            }
            else
            {
                Environment.Exit(1);
            }
        }

        private static string GetFileName(string url, uint count)
        {
            if (count == MaxRedirects)
                return null;

            var fileName = string.Empty;

            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
            httpWebRequest.Method = "HEAD";
            httpWebRequest.AllowAutoRedirect = false;

            HttpWebResponse httpWebResponse;

            try
            {
                httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            }
            catch (WebException)
            {
                return string.Empty;
            }

            if (httpWebResponse.StatusCode.Equals(HttpStatusCode.Redirect) ||
                httpWebResponse.StatusCode.Equals(HttpStatusCode.Moved) ||
                httpWebResponse.StatusCode.Equals(HttpStatusCode.MovedPermanently))
            {
                if (httpWebResponse.Headers["Location"] != null)
                {
                    var location = httpWebResponse.Headers["Location"];
                    fileName = GetFileName(location, ++count);
                    return fileName;
                }
            }

            var contentDisposition = httpWebResponse.Headers["content-disposition"];
            if (!string.IsNullOrEmpty(contentDisposition))
            {
                const string lookForFileName = "filename=";
                var index = contentDisposition.IndexOf(lookForFileName, StringComparison.CurrentCultureIgnoreCase);
                if (index >= 0)
                    fileName = contentDisposition.Substring(index + lookForFileName.Length);

                if (fileName.StartsWith("\"") && fileName.EndsWith("\""))
                    fileName = fileName.Substring(1, fileName.Length - 2);
            }

            if (!string.IsNullOrEmpty(fileName))
                return fileName;

            var uri = new Uri(url);

            fileName = Path.GetFileName(uri.LocalPath);

            return fileName;
        }
    }
}