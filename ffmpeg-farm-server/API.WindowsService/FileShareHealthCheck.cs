using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DR.Common.Monitoring.Models;

namespace API.WindowsService
{
    public class FileShareHealthCheck : CommonHealthCheck
    {
        private readonly string[] _files;
        private readonly string[] _folders;
        public FileShareHealthCheck(IEnumerable<string> files, IEnumerable<string> folders) :
            base($"OD3.FfmpegFarm.{nameof(FileShareHealthCheck)}",
            descriptionText: "Check files and folder on network shares")
        {
            _files = files.ToArray();
            _folders = folders.ToArray();
        }

        private void CheckLogic(IEnumerable<string> list, Func<string, bool> check, StatusBuilder statusBuilder)
        {
            foreach (var item in list)
            {
                if (!check(item))
                {
                    statusBuilder.Passed = false;
                    statusBuilder.MessageBuilder.AppendLine($" {item} NOT found! ");
                }
                else
                {
                    statusBuilder.MessageBuilder.AppendLine($" {item} OK! ");
                }
            }
        }

        protected override void RunTest(StatusBuilder statusBuilder)
        {
            statusBuilder.Passed = true;
            CheckLogic(_files, File.Exists, statusBuilder);
            CheckLogic(_folders, Directory.Exists, statusBuilder);
        }
    }
}
