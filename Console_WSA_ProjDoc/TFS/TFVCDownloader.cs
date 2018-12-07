using System;
using System.IO;
using System.Linq;
using System.Net;
using Console_WSA_ProjDoc.General;
using Console_WSA_ProjDoc.XML;
using Microsoft.TeamFoundation;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace Console_WSA_ProjDoc.TFS
{
    public class TFVCDownloader
    {
        private static readonly Logging Logging = new Logging();
        private static readonly string Workspacename = "WorkspaceName";
        private static BasicAuthCredential _basicCred;
        private static TfsClientCredentials _TFVCcred;
        private static TfsTeamProjectCollection _TFVC;

        private NetworkCredential _netCred;

        /* AO UTILIZAR SERVIDOR PROXY, CERTIFICAR QUE O MESMO ESTÁ DISPONÍVEL PARA USO.
         * CASO HAJA INDISPONIBILIDADE, a autenticação e WorkItemStore ficarão travados*/
        private XmlConfigs.Xml Xml { get; set; } = new XmlConfigs.Xml();

        public void LoadXml(XmlConfigs.Xml xml)
        {
            this.Xml = xml;
        }

        public bool GetProject(int nId, bool changesets)
        {
            var getProjectOk = false;
            try
            {
                getProjectOk = ConnectTfs();
                if (getProjectOk) getProjectOk = DownloadTFVCFromTFS();
                if (getProjectOk && changesets) getProjectOk = DownloadTFVCChangesets();

                getProjectOk = true;
            }
            catch (TeamFoundationServiceUnavailableException e)
            {
                Logging.WriteLog(e.Message);
            }
            catch (TeamFoundationServerUnauthorizedException e)
            {
                Logging.WriteLog(
                    "An TFS Authentication has occurred. 1) Make sure that the Server URL, Username and Password are correct; " +
                    "2) Make sure that the 'Alternate Authentication Credentials' was enabled in the server page; 3) If there is a Proxy Server, try to deactivate it: \n" +
                    e.Message);
            }
            catch (UnauthorizedAccessException e)
            {
                Logging.WriteLog(
                    @"An Unauthorized Access Exception has occurred. 1) Make sure that the current user has access in each folder; 2) Make sure that the Username has access in the TFVC Workspace/Project: \n" +
                    e.Message);
            }
            catch (IOException e)
            {
                Logging.WriteLog("An I/O Exception has occurred during the TFS Authentitcation: \n" + e);
            }
            catch (Exception e)
            {
                Logging.WriteLog("An Exception has occurred during the TFS Authentication: \n" + e);
            }
            finally
            {
                if (!getProjectOk)
                {
                    Logging.WriteLog("FATAL ERROR. THE EXECUTION WILL BE STOPPED");
                    Environment.Exit(0);
                }
            }
            return getProjectOk;
        }

        private bool ConnectTfs()
        {
            var breturn = false;

            #region Tentativa de conexão ao TFS

            Logging.WriteLog("Starting the TFS Server Connection...");
            _netCred = new NetworkCredential(Xml.TfsUsername, Xml.TfsPassword);

            _basicCred = new BasicAuthCredential(_netCred);
            _TFVCcred = new TfsClientCredentials(_basicCred) { AllowInteractive = false };
            _TFVC = new TfsTeamProjectCollection(new Uri(Xml.TfsServerUrl), _TFVCcred);
            _TFVC.Authenticate();

            breturn = true;

            #endregion

            return breturn;
        }

        private bool DownloadTFVCFromTFS()
        {
            var breturn = false;

            Logging.WriteLog("Currend Instance ID: " + _TFVC.InstanceId);

            #region Download do Workspace Local

            Logging.WriteLog("Starting the Local Workspace Update...");
            var versioncontrols = (VersionControlServer)_TFVC.GetService(typeof(VersionControlServer));
            var workspace = versioncontrols.QueryWorkspaces(Workspacename, Xml.TfsUsername, Environment.MachineName)
                .SingleOrDefault();

            if (workspace != null) // Recriar o workspace local, caso o mesmo já exista
                versioncontrols.DeleteWorkspace(Workspacename, Xml.TfsUsername);
            workspace = versioncontrols.CreateWorkspace(Workspacename, Xml.TfsUsername);
            var workfolder = new WorkingFolder(Xml.TfsProjectName, Xml.LocalFolder);
            workspace.CreateMapping(workfolder);

            Logging.WriteLog("Local Folder: " + Xml.LocalFolder + ".");
            if (!Directory.Exists(Xml.LocalFolder)) Directory.CreateDirectory(Xml.LocalFolder);

            Logging.WriteLog("Starting the Project Download: " + Xml.TfsProjectName + "...");
            workspace.Get();
            Logging.WriteLog("Download statis: CONCLUDED.");

            breturn = true;

            #endregion

            return breturn;
        }

        private bool DownloadTFVCChangesets()
        {
            var breturn = false;
            var d = new DirectoryInfo(Xml.LocalFolder);
            var vcs = (VersionControlServer)_TFVC.GetService(typeof(VersionControlServer));
            var tp = vcs.GetTeamProject(Xml.ProjectTitle);
            var path = "";
            var serveritem = tp.ServerItem;
            var folderstring = "";
            var counter = 0;
            var current = 0;

            Logging.WriteLog("Starting the Changesets update...");

            #region identificar ID do último changeset realizado
            var changes = vcs.QueryHistory(serveritem, VersionSpec.Latest, 0, RecursionType.Full, null,
                VersionSpec.Latest, VersionSpec.Latest, int.MaxValue, true, true, false, false);
            var latest = changes.Cast<Changeset>().First();
            var id = latest.ChangesetId;
            #endregion

            #region criar registro .#tfvc dos arquivos
            Logging.WriteLog("Creating .#tfvc files...");
            Logging.WriteLog("");
            current = 0;
            counter = d.GetFiles("*" + Xml.Extension, SearchOption.AllDirectories).Length;
            foreach (var file in d.GetFiles("*" + Xml.Extension, SearchOption.AllDirectories))
            {
                current++;
                Logging.WriteLog(current + " of " + counter + " changesets processed.", true);
                if (!file.FullName.Contains("$"))
                {
                    // Se não existe o arquivo texto do changeset, cria arquivo texto e inputa histórico do changeset
                    path = file.DirectoryName + @"\" + file.Name.Replace(file.Extension, "") + ".#tfvc";
                    if (!File.Exists(path)) // Se o arquivo não existir, baixa o changeset do mesmo.
                    {
                        File.WriteAllText(path, "@SHA1: " + Environment.NewLine); // Salva a primeira linha para que o Hash SHA1 seja gravado através da classe FileHash
                    }
                    else
                    {
                        var fileContent = File.ReadLines(path).ToList();
                        File.WriteAllText(path, fileContent[0] + Environment.NewLine); // Salva apenas o conteúdo da primeira linha, que contém o último hash. Será utilizado para comparar se houve modificação no conteúdo do arquivo
                    }
                    if (Xml.TfsChangesets == "true")
                    {
                        folderstring = file.FullName.Replace(Xml.LocalFolder, "").Replace("\\", "/");
                        var changesetpath = tp.ServerItem + folderstring;

                        changes = vcs.QueryHistory(
                            changesetpath,
                            VersionSpec.Latest,
                            0,
                            RecursionType.Full,
                            null,
                            VersionSpec.ParseSingleSpec("C001", null), // Primeiro changeset
                            VersionSpec.ParseSingleSpec("C" + id, null), // Último changeset
                            int.MaxValue,
                            true,
                            false);
                    }
                    foreach (Changeset change in changes)
                    {
                        var texto = "";
                        texto = "@CreationDate: " + change.CreationDate + Environment.NewLine;
                        texto += "@Comment: " + change.Comment + Environment.NewLine;
                        texto += "@Changeset: " + "#" + change.ChangesetId + ", " + change.CommitterDisplayName + Environment.NewLine;

                        File.AppendAllText(path, texto);
                    }
                }
            }
            #endregion

            breturn = true;
            return breturn;
        }
    }
}