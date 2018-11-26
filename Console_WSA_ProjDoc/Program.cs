using System;
using System.Configuration.Install;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using Console_WSA_ProjDoc.General;
using Console_WSA_ProjDoc.HTML;
using Console_WSA_ProjDoc.TFS;
using Console_WSA_ProjDoc.XML;

namespace Console_WSA_ProjDoc
{
    internal class Program : ServiceBase
    {
        private static TfsDownloader TFSDownloader = new TfsDownloader();
        private static readonly XmlConfigs.Xml Xml = new XmlConfigs.Xml();
        private static readonly Logging Logging = new Logging();

        public Program()
        {
            ServiceName = Xml.ServiceName;
        }

        private static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;
            if (Environment.UserInteractive)
            {
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
                            ManagedInstallerClass.InstallHelper(new[] {Assembly.GetExecutingAssembly().Location});
                            break;
                        case "-uninstall":
                            ManagedInstallerClass.InstallHelper(new[] {"/u", Assembly.GetExecutingAssembly().Location});
                            break;
                        default:
                            Logging.WriteLog(@"Execução em modo console.");
                            while (projects < Xml.Projects)
                            {
                                //
                                Logging.WriteLog("Iniciando projeto " + projects);
                                projecttype = Xml.GetProjectType(projects);
                                if (projecttype == "TFS")
                                {
                                    Xml.GetTfsElements(projects);
                                    TFSDownloader.LoadXml(Xml);
                                    TFSDownloader.GetProject(projects, Xml.TfsChangesets == "show");
                                }
                                if (projecttype == "Local")
                                {
                                    Xml.GetLocalElements(projects);
                                }
                                // Mudança de Idioma
                                Thread.CurrentThread.CurrentUICulture = new CultureInfo(Xml.Language);
                                Xml.LoadDictionary();
                                //
                                var html = new HtmlGenerator();
                                html.LoadXml(Xml);
                                html.Start();
                                Logging.WriteLog("Projeto " + projects + " finalizado.");
                                projects++;
                            }

                            break;
                    }
                }
                catch (System.Xml.XmlException e)
                {
                    Logging.WriteLog(@"Falha durante a manutenção do XML: " + e);
                }
                catch (Exception e)
                {
                    Logging.WriteLog(@"Falha durante a manutenção do serviço: " + e);
                }
            }
            else
            {
                Logging.WriteLog(@"Execução fora do modo de debug e console");
                Run(new Program());
            }
        }

        private static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            File.AppendAllText(@"C:\Temp\error.txt",
                ((Exception) e.ExceptionObject).Message + ((Exception) e.ExceptionObject).InnerException.Message);
        }

        protected override void OnStart(string[] args)
        {
            Logging.WriteLog("A execução do serviço foi iniciada.");

            base.OnStart(args);
        }

        protected override void OnStop()
        {
            base.OnStop();

            Logging.WriteLog("A execução do serviço foi finalizada.");
        }

        private void StartUp()
        {
            if (!File.Exists(Xml.XmlFileName))
                Logging.WriteLog("O arquivo de configuração não existe em:" + Xml.AssemblyFolder + Xml.XmlFileName);
        }
    }
}