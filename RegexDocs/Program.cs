using System;
using System.Configuration.Install;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using RegexDocs.General;
using RegexDocs.HTML;
using RegexDocs.TFS;
using RegexDocs.XML;

namespace RegexDocs
{
    internal class Program : ServiceBase
    {
        private static TFVC TFVC;
        //private static ExecuteQuery Azure_DevOps = new ExecuteQuery();
        private static readonly XmlConfigs.Xml Xml = new XmlConfigs.Xml();
        private static readonly Logging Logging = new Logging();

        public Program()
        {
            ServiceName = Xml.ServiceName;
        }

        private static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;
            try
            {
                var parameter = string.Concat(args);
                var projects = 1;
                string projecttype = null;
                switch (parameter)
                {
                    /*
                     PARÂMETROS DO InstallUtil.exe:
                     /LogToConsole={true|false} : true exibirá a saída no console. false suprimirá a saída no console.
                     /u                         : Desinstala os assemblies especificados. 
                     */
                    case "-install":
                        ManagedInstallerClass.InstallHelper(new[] { Assembly.GetExecutingAssembly().Location });
                        break;
                    case "-uninstall":
                        ManagedInstallerClass.InstallHelper(new[] { "/u", Assembly.GetExecutingAssembly().Location });
                        break;
                    default:
                        var UpdateHTML = true;
                        while (projects < Xml.Projects + 1)
                        {
                            //
                            Logging.WriteLog("Starting project " + projects + "...");
                            projecttype = Xml.GetProjectType(projects);
                            if (projecttype == "tfvc")
                            {
                                Xml.GetTfsElements(projects);
                                if (!Directory.Exists(Xml.LocalFolder)) Directory.CreateDirectory(Xml.LocalFolder);
                                TFVC = new TFVC(Xml);
                                TFVC.GetProject(projects);
                            }
                            else if (projecttype == "local")
                            {
                                Xml.GetLocalElements(projects);
                            }
                            else UpdateHTML = false;
                            if (UpdateHTML)
                            {
                                if (!Directory.Exists(Xml.HtmlFolder)) Directory.CreateDirectory(Xml.HtmlFolder);

                                // Mudança de Idioma
                                Thread.CurrentThread.CurrentUICulture = new CultureInfo(Xml.Language);
                                Xml.LoadDictionaryList();
                                Xml.LoadRegexList();
                                //
                                var html = new HtmlGenerator();
                                html.LoadXml(Xml);
                                html.Start();
                                Logging.WriteLog("Project " + projects + " Documentation Status: CONCLUDED.");
                            }
                            else Logging.WriteLog("The project type [" + projecttype + "] is not valid.");
                            projects++;
                        }
                        Logging.WriteLog(@"The Update of every project was executed.");

                        break;
                }
            }
            catch (System.Xml.XmlException e)
            {
                Logging.WriteLog(@"A XML Exception occurred during the current execution: " + e);
            }
            catch (Exception e)
            {
                Logging.WriteLog(@"An Exception occurred during the current execution: " + e);
            }
        }

        private static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e) => Logging.WriteLog(((Exception)e.ExceptionObject).Message + ((Exception)e.ExceptionObject).InnerException.Message);

        protected override void OnStart(string[] args)
        {
            Logging.WriteLog("The Service Execution has started...");

            base.OnStart(args);
        }

        protected override void OnStop()
        {
            base.OnStop();

            Logging.WriteLog("The Service Execution has stopped...");
        }

        private void StartUp()
        {
            if (!File.Exists(Xml.XmlFileName))
                Logging.WriteLog("The configuration file does not exists at: " + Xml.AssemblyFolder + Xml.XmlFileName);
        }
    }
}