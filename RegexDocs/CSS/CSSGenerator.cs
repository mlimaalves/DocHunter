﻿using System.IO;
using System.Reflection;
using RegexDocs.General;
using RegexDocs.HTML;
using RegexDocs.XML;

namespace RegexDocs.CSS
{
    internal class CssGenerator
    {
        #region Diretório do Assembly do executável

        private static readonly string Assemblyfolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\";
        private static readonly Logging Logging = new Logging();
        private DirectoryInfo _d;
        private XmlConfigs.Xml Xml { get; set; } = new XmlConfigs.Xml();

        public void LoadXml(XmlConfigs.Xml xml)
        {
            this.Xml = xml;
            _d = new DirectoryInfo(xml.LocalFolder);
        }

        #endregion
        public void CopyCss(string htmlfolder)
        {
            string originalfolder = Assemblyfolder + @"CSS\";

            Copy(originalfolder, htmlfolder);
        }


        private static void Copy(string sourceDirectory, string targetDirectory)
        {
            DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
            DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);

            CopyAll(diSource, diTarget);
        }

        private static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.EnumerateFiles())
            {
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.EnumerateDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }
    }
}
