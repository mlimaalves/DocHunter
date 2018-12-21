using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using Console_WSA_ProjDoc.General;
using Console_WSA_ProjDoc.XML;
using Console_WSA_ProjDoc.SQLite;
using Microsoft.TeamFoundation;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.VersionControl.Client;
using static Console_WSA_ProjDoc.SQLite.Datastore;
using System.Collections.Generic;

namespace Console_WSA_ProjDoc.TFS
{
    public class TFVC
    {
        private static readonly Logging Logging = new Logging();
        private static readonly string Workspacename = Environment.UserName + "_" + Environment.MachineName;
        private TfsConfigurationServer _TFVC;
        private XmlConfigs.Xml Xml;
        internal History History { get; private set; }

        public TFVC(XmlConfigs.Xml xml) => this.Xml = xml;

        public bool GetProject(int nId)
        {
            var TFSOk = false;
            try
            {
                TFSOk = Connect();
                if (TFSOk) TFSOk = UpdateProject();

                TFSOk = true;
            }
            catch (TeamFoundationServiceUnavailableException e)
            {
                Logging.WriteLog(e.Message);
            }
            catch (TeamFoundationServerUnauthorizedException e)
            {
                Logging.WriteLog(
                    "An TFS Authentication has occurred. 1) Make sure that the Server URL and Username are correct; " +
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
                Logging.WriteLog("An I/O Exception has occurred during the TFS Authentication: \n" + e);
            }
            catch (Exception e)
            {
                if(e.Message.Contains("TF14045"))
                {
                    Logging.WriteLog("ERROR: sure that the user inside the <username> tag is a " + Xml.TfsProjectName + " Member and contains the CreateWorkspace Global Permission.");
                }
                else if (e.Message.Contains("TF14044") || e.Message.Contains("TF204017"))
                {
                    Logging.WriteLog("ERROR: User has no sufficient permissions to manage workspace. Please, try to access the following URL and check if the user is a member of a group that has 'Create a new workspace' permission allowed:" +
                        Xml.TfsServerUrl + ((Xml.TfsServerUrl.ToString().Last().ToString() == "m") ? "/" : "") + "DefaultCollection/_settings/security");
                }
                else Logging.WriteLog("An Exception has occurred during the TFS Operations.");
                throw e;
            }
            finally
            {
                if (!TFSOk)
                {
                    Logging.WriteLog("FATAL TFS ERROR. THE EXECUTION WILL BE STOPPED");
                    Environment.Exit(0);
                }
            }
            return TFSOk;
        }

        private bool Connect()
        {
            var breturn = false;

            #region Tentativa de conexão ao TFS

            Logging.WriteLog("Starting the TFS Server Connection...");

            _TFVC = TfsConfigurationServerFactory.GetConfigurationServer(new Uri(Xml.TfsServerUrl));

            _TFVC.EnsureAuthenticated();

            breturn = true;

            #endregion

            return breturn;
        }

        private bool UpdateProject()
        {
            var downloaded = false;
            var dbsource = Xml.LocalFolder + "db.sqlite";
            Datastore SQLiteDb = new Datastore(dbsource);
            SQLiteDb.DbCreation();

            Logging.WriteLog("Instance ID: " + _TFVC.InstanceId);

            // Get the catalog of team project collections
            ReadOnlyCollection<CatalogNode> collectionNodes = _TFVC.CatalogNode.QueryChildren(
                new[] { CatalogResourceTypes.ProjectCollection },
                false, CatalogQueryOptions.None);

            // List the team project collections
            foreach (CatalogNode collectionNode in collectionNodes)
            {
                // Use the InstanceId property to get the team project collection
                Guid collectionId = new Guid(collectionNode.Resource.Properties["InstanceId"]);
                TfsTeamProjectCollection teamProjectCollection = _TFVC.GetTeamProjectCollection(collectionId);
                Logging.WriteLog("Collection: " + teamProjectCollection.Name);

                ReadOnlyCollection<CatalogNode> projectNodes = collectionNode.QueryChildren(
                    new[] { CatalogResourceTypes.TeamProject },
                    false, CatalogQueryOptions.None);

                // List the team projects in the collection
                foreach (CatalogNode projectNode in projectNodes)
                {
                    if (projectNode.Resource.DisplayName == Xml.TfsProjectName)
                    {
                        downloaded = true;
                        Logging.WriteLog("Team Project: " + projectNode.Resource.DisplayName);
                        VersionControlServer versionControl = (VersionControlServer)teamProjectCollection.GetService(typeof(VersionControlServer));
                        Workspace ws = versionControl.QueryWorkspaces(Workspacename, null, Environment.MachineName).SingleOrDefault();

                        if (ws == null)
                        {
                            Logging.WriteLog("There is no workspace for " + Environment.MachineName);
                            ws = versionControl.CreateWorkspace(Workspacename, Xml.TfsUsername);
                            Logging.WriteLog("Workspace " + Workspacename + " Creation Status: CONCLUDED.");
                        }
                        if (ws.MappingsAvailable == false)
                        {
                            Logging.WriteLog("There is no workspace for " + Workspacename);
                            ws.CreateMapping(new WorkingFolder(Xml.TfsProjectName, Xml.LocalFolder));
                            Logging.WriteLog("Workspace " + Workspacename + " Mapping Status: CONCLUDED.");
                        }

                        if (!Directory.Exists(Xml.LocalFolder)) Directory.CreateDirectory(Xml.LocalFolder);

                        Logging.WriteLog("Workspace " + Workspacename);
                        Logging.WriteLog("Downloading Updated files at: " + Xml.LocalFolder);
                        Logging.WriteLog("All folders and files inside the $/" + Xml.TfsProjectName + " path will be downloaded.");

                        if (Xml.TfsHistory == "true")
                        {
                            Logging.WriteLog("The <history> tag IS set as 'true'. The History SQLite database will be created at " + dbsource);
                        }
                        else Logging.WriteLog("The <history> tag IS NOT set as 'true'. The History will not be Download and Documented.");

                        var previousdirectory = "";
                        Logging.WriteLog("");
                        foreach (Item item in
    versionControl.GetItems("$/" + Xml.TfsProjectName, VersionSpec.Latest, RecursionType.Full, DeletedState.NonDeleted, ItemType.Any, true).Items)
                        {
                            string targetFile = Path.Combine(Xml.LocalFolder, item.ServerItem.Substring(2));
                            if (item.ItemType == ItemType.Folder && targetFile != previousdirectory)
                            {
                                Logging.WriteLog(("Downloading: " + item.ServerItem), true);
                                previousdirectory = targetFile;
                            }
                            if (item.ItemType == ItemType.Folder && !Directory.Exists(targetFile)) Directory.CreateDirectory(targetFile);

                            else if (item.ItemType == ItemType.File)
                            {
                                item.DownloadFile(targetFile);
                                if (Xml.TfsHistory == "true")
                                {
                                    var changesetList = versionControl.QueryHistory(item.ServerItem, VersionSpec.Latest, 0,
                                        RecursionType.Full, null, null, null, Int32.MaxValue, false, false);

                                    foreach (Changeset changeset in changesetList)
                                    {
                                        // Adding every history record in the temporarly sqlite file:
                                        History = new History
                                        {
                                            File = targetFile.ToLower().Replace("/", "\\"),
                                            Id = changeset.ChangesetId,
                                            CreationDate = changeset.CreationDate,
                                            Creator = changeset.CommitterDisplayName,
                                            Comment = changeset.Comment
                                        };

                                        SQLiteDb.AddRecord(History);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (downloaded) Logging.WriteLog("Project Download Status: CONCLUDED", true);
            else Logging.WriteLog("It was not possible to download the project. Please, confirm that the serverurl, projectname and username are correct.");

            return downloaded = true;
        }
    }
}