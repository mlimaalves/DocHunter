using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace Console_WSA_ProjDoc
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

            serviceInstaller.DisplayName = "_Console_WSA";
            serviceInstaller.StartType = ServiceStartMode.Automatic;

            //must be the same as what was set in Program's constructor
            serviceInstaller.ServiceName = "_Console_WSA";
            Installers.Add(processInstaller);
            Installers.Add(serviceInstaller);
        }
    }
}