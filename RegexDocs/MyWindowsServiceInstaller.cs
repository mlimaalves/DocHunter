using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace RegexDocs
{
    [RunInstaller(true)]
    public class MyWindowsServiceInstaller : Installer
    {
        public MyWindowsServiceInstaller()
        {
            var processInstaller = new ServiceProcessInstaller();
            var serviceInstaller = new ServiceInstaller();

            //set the privileges
            processInstaller.Account = ServiceAccount.LocalSystem;

            serviceInstaller.DisplayName = "_RegexDocs";
            serviceInstaller.StartType = ServiceStartMode.Automatic;

            //must be the same as what was set in Program's constructor
            serviceInstaller.ServiceName = "_RegexDocs";
            Installers.Add(processInstaller);
            Installers.Add(serviceInstaller);
        }
    }
}