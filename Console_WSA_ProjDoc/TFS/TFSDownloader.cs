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
    public class TfsDownloader
    {
        private static readonly Logging Logging = new Logging();
        private static readonly string Workspacename = "WorkspaceName";
        private static BasicAuthCredential _basicCred;
        private static TfsClientCredentials _tfsCred;
        private static TfsTeamProjectCollection _tfs;

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
                //if (getProjectOk) getProjectOk = DownloadProjectFromTfs();
                //if (getProjectOk && changesets) getProjectOk = DownloadChangesets();

                getProjectOk = true;
            }
            catch (TeamFoundationServiceUnavailableException e)
            {
                Logging.WriteLog(e.Message);
            }
            catch (TeamFoundationServerUnauthorizedException e)
            {
                Logging.WriteLog(
                    "Falha de autenticação. Verifique se as informações de servidor, usuário e senhas estão corretas. " +
                    "Verifique na página online do TFS se a opção 'Alternate Authentication Credentials' está habilitada. Se utiliza servidor proxy, revise ou desative as configurações.: \n" +
                    e.Message);
            }
            catch (UnauthorizedAccessException e)
            {
                Logging.WriteLog(
                    @"Exceção de acesso negado. Verifique se você possui acesso as pastas de destino e \Microsoft Team Foundation Local Workspaces\.: \n" +
                    e.Message);
            }
            catch (IOException e)
            {
                Logging.WriteLog("Erro de Input/Output.: \n" + e);
            }
            catch (Exception e)
            {
                Logging.WriteLog("Exceção durante montagem de projeto TFS: \n" + e);
            }
            finally
            {
                if (!getProjectOk)
                {
                    Logging.WriteLog("ERRO FATAL. A execução será finalizada.");
                    Environment.Exit(0);
                }
            }
            return getProjectOk;
        }

        private bool ConnectTfs()
        {
            var breturn = false;

            #region Tentativa de conexão ao TFS

            Logging.WriteLog("Iniciando conexão com o TFS Server...");
            _netCred = new NetworkCredential(Xml.TfsUsername, Xml.TfsPassword);

            _basicCred = new BasicAuthCredential(_netCred);
            _tfsCred = new TfsClientCredentials(_basicCred) { AllowInteractive = false };
            _tfs = new TfsTeamProjectCollection(new Uri(Xml.TfsServerUrl), _tfsCred);
            _tfs.Authenticate();

            breturn = true;

            #endregion

            return breturn;
        }

        private bool DownloadProjectFromTfs()
        {
            var breturn = false;

            Logging.WriteLog("Id da Instância atual: " + _tfs.InstanceId);

            #region Download do Workspace Local

            Logging.WriteLog("Iniciando criação do Workspace Local...");
            var versioncontrols = (VersionControlServer)_tfs.GetService(typeof(VersionControlServer));
            var workspace = versioncontrols.QueryWorkspaces(Workspacename, Xml.TfsUsername, Environment.MachineName)
                .SingleOrDefault();

            if (workspace != null) // Recriar o workspace local, caso o mesmo já exista
                versioncontrols.DeleteWorkspace(Workspacename, Xml.TfsUsername);
            workspace = versioncontrols.CreateWorkspace(Workspacename, Xml.TfsUsername);
            var workfolder = new WorkingFolder(Xml.TfsProject, Xml.TfsFolder);
            workspace.CreateMapping(workfolder);

            Logging.WriteLog("Caminho Local: " + Xml.TfsFolder + ".");
            if (!Directory.Exists(Xml.TfsFolder)) Directory.CreateDirectory(Xml.TfsFolder);

            Logging.WriteLog("Iniciando download do Projeto " + Xml.TfsProject + "...");
            workspace.Get();
            Logging.WriteLog("Download concluído.");

            breturn = true;

            #endregion

            return breturn;
        }

        private bool DownloadChangesets()
        {
            var breturn = false;
            var d = new DirectoryInfo(Xml.TfsFolder);
            var vcs = (VersionControlServer)_tfs.GetService(typeof(VersionControlServer));
            var tp = vcs.GetTeamProject(@"TFS - Fareva LOU IT");
            var path = "";
            var serveritem = tp.ServerItem;
            var folderstring = "";
            var counter = 0;
            var current = 0;

            Logging.WriteLog("Iniciando criação de arquivos Changesets...");

            #region identificar ID do último changeset realizado
            var changes = vcs.QueryHistory(serveritem, VersionSpec.Latest, 0, RecursionType.Full, null,
                VersionSpec.Latest, VersionSpec.Latest, int.MaxValue, true, true, false, false);
            var latest = changes.Cast<Changeset>().First();
            var id = latest.ChangesetId;
            #endregion

            #region criar registro .#cs dos arquivos
            Logging.WriteLog("Criando .#cs dos arquivos...");
            Logging.WriteLog("");
            current = 0;
            counter = d.GetFiles("*.prw", SearchOption.AllDirectories).Length;
            foreach (var file in d.GetFiles("*.prw", SearchOption.AllDirectories))
            {
                current++;
                Logging.WriteLog(current + " de " + counter + " arquivos processados.", true);
                if (!file.FullName.Contains("$"))
                {
                    // Se não existe o arquivo texto do changeset, cria arquivo texto e inputa histórico do changeset
                    path = file.DirectoryName + @"\" + file.Name.Replace(file.Extension, "") + ".#cs";
                    if (!File.Exists(path)) // Se o arquivo não existir, baixa o changeset do mesmo.
                    {
                        File.WriteAllText(path, "@SHA1: " + Environment.NewLine); // Salva a primeira linha para que o Hash SHA1 seja gravado através da classe FileHash
                    }
                    else
                    {
                        var fileContent = File.ReadLines(path).ToList();
                        File.WriteAllText(path, fileContent[0] + Environment.NewLine); // Salva apenas o conteúdo da primeira linha, que contém o último hash. Será utilizado para comparar se houve modificação no conteúdo do arquivo
                    }
                    if (Xml.TfsChangesets == "show")
                    {
                        folderstring = file.FullName.Replace(Xml.TfsFolder, "").Replace("\\", "/");
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