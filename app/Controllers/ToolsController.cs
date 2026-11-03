using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Verademo.Models;

namespace Verademo.Controllers
{
    public class ToolsController : AuthControllerBase
    {
        protected readonly log4net.ILog logger;

        public ToolsController()
        {
            logger = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        }

        [HttpGet, ActionName("Tools")]
        public ActionResult GetTools()
        {
            if (IsUserLoggedIn() == false)
            {
                return RedirectToLogin(Request.QueryString.Value);
            }

            return View(new ToolViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Tools(string host, string fortuneFile)
        {
            if (IsUserLoggedIn() == false)
            {
                return RedirectToLogin(Request.QueryString.Value);
            }

            var viewModel = new ToolViewModel();
            viewModel.Host = host;
            viewModel.PingResult = Ping(host);
            viewModel.FortuneResult = Fortune(fortuneFile);

            return View("Tools", viewModel);
        }

        private string Ping(string host)
        {
            if (string.IsNullOrEmpty(host))
            {
                return "";
            }

            logger.Info("Pinging: " + WebUtility.UrlEncode(host));

            var output = new StringBuilder();
            try
            {
                // Validate and sanitize host input to prevent command injection
                var sanitizedHost = SanitizeHostInput(host);
                if (string.IsNullOrEmpty(sanitizedHost))
                {
                    return "Invalid host input";
                }

                var fileName = "bash";
                var arguments = "-c \"ping -c1 " + sanitizedHost + "\"";

                var proc = CreateStdOutProcess(fileName, arguments);

                proc.ErrorDataReceived += delegate (object sender, DataReceivedEventArgs e) { output.AppendLine(e.Data); };
                proc.OutputDataReceived += delegate (object sender, DataReceivedEventArgs e) { output.AppendLine(e.Data); };

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                proc.WaitForExit();
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }

            return output == null ? "" : output.ToString();
        }

        private string Fortune(string fortuneFile)
        {
            var output = new StringBuilder();

            if (string.IsNullOrEmpty(fortuneFile))
            {
                fortuneFile = "literature";
            }

            try
            {
                // Validate and sanitize fortuneFile input to prevent command injection
                var sanitizedFortuneFile = SanitizeFortuneFileInput(fortuneFile);
                if (string.IsNullOrEmpty(sanitizedFortuneFile))
                {
                    return "Invalid fortune file input";
                }

                var fileName = "/bin/bash";
                var arguments = $"-c \"/bin/fortune {sanitizedFortuneFile}\"";

                var proc = CreateStdOutProcess(fileName, arguments);

                proc.ErrorDataReceived += delegate (object sender, DataReceivedEventArgs e) { output.Append(e.Data); };
                proc.OutputDataReceived += delegate (object sender, DataReceivedEventArgs e) { output.Append(e.Data + "\n"); };

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                proc.WaitForExit();
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
            return output == null ? "" : output.ToString();
        }

        private static Process CreateStdOutProcess(string filename, string arguments)
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = filename,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            return proc;
        }

        private static string SanitizeHostInput(string host)
        {
            if (string.IsNullOrEmpty(host))
            {
                return null;
            }

            // Allow only valid hostname/IP characters: alphanumeric, dots, hyphens, and colons (for IPv6)
            // Remove any shell metacharacters that could be used for command injection
            var sanitized = Regex.Replace(host, @"[^a-zA-Z0-9.:\-]", "");

            // Additional validation: ensure it looks like a valid hostname or IP
            if (string.IsNullOrEmpty(sanitized) || sanitized.Length > 253)
            {
                return null;
            }

            return sanitized;
        }

        private static string SanitizeFortuneFileInput(string fortuneFile)
        {
            if (string.IsNullOrEmpty(fortuneFile))
            {
                return null;
            }

            // Allow only alphanumeric characters, underscores, and hyphens for fortune file names
            // Remove any shell metacharacters that could be used for command injection
            var sanitized = Regex.Replace(fortuneFile, @"[^a-zA-Z0-9_\-]", "");

            // Ensure the sanitized input is not empty and has reasonable length
            if (string.IsNullOrEmpty(sanitized) || sanitized.Length > 100)
            {
                return null;
            }

            return sanitized;
        }
    }
}