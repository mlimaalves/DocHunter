namespace Console_WSA_ProjDoc.XML
{
    public class XmlConfigs
    {
        private static readonly XmlOperations XmlOperations = new XmlOperations();

        public class Xml
        {
            // Controle do XML dinâmico:
            public System.Collections.Generic.List<string[]> DictionaryList { get; set; }
            public System.Collections.Generic.List<string[]> RegexList { get; set; }

            // Variáveis gerais, compartilhadas entre todos os projects:
            public int Projects { get; set; } = XmlOperations.GetProjectCount("project");
            public string XmlFileName { get; set; } = XmlOperations.XmlFileName;
            public string AssemblyFolder { get; set; } = XmlOperations.AssemblyFolder;

            public string ServiceName { get; set; } =
                XmlOperations.GetXmlElements("/configurations/service/servicename");

            public string DisplayName { get; set; } =
                XmlOperations.GetXmlElements("/configurations/service/displayname");

            public string ServiceDescription { get; set; } =
                XmlOperations.GetXmlElements("/configurations/service/servicedescription");

            // Variáveis específicas para projects do tipo TFS:
            public string TfsServerUrl { get; set; }
            public string TfsProjectName { get; set; }
            public string TfsUsername { get; set; }
            public string TfsPassword { get; set; }
            public string TfsHistory { get; set; }

            //
            public string ProjectTitle { get; set; }
            public string HtmlFolder { get; set; }
            public string LocalFolder { get; set; }
            public string DeleteFiles { get; set; }
            public string CallIndex { get; set; }
            public string Language { get; set; }
            public string Extension { get; set; }
            public string ProgrammingLanguage { get; set; }
            //

            public string GetProjectType(int nId)
            {
                return XmlOperations.GetXmlProjectType(nId);
            }

            public void GetTfsElements(int nId)
            {
                ProjectTitle =
                    XmlOperations.GetXmlElements("/configurations/projects/project[@nId='" + nId + "']/title", true);
                TfsServerUrl =
                    XmlOperations.GetXmlElements("/configurations/projects/project[@nId='" + nId + "']/tfvc/serverurl", true);
                TfsProjectName =
                    XmlOperations.GetXmlElements("/configurations/projects/project[@nId='" + nId + "']/tfvc/name", true);
                TfsUsername =
                    XmlOperations.GetXmlElements(
                        "/configurations/projects/project[@nId='" + nId + "']/tfvc/networkcredential/username", true);
                TfsPassword =
                    XmlOperations.GetXmlElements(
                        "/configurations/projects/project[@nId='" + nId + "']/tfvc/networkcredential/password", true);
                TfsHistory =
                    XmlOperations.GetXmlElements("/configurations/projects/project[@nId='" + nId + "']/tfvc/history").ToLower();
                //
                HtmlFolder =
                    XmlOperations.GetXmlElements("/configurations/projects/project[@nId='" + nId + "']/html/htmlfolder", true).ToLower();
                HtmlFolder = (HtmlFolder.Substring(HtmlFolder.Length - 1) == @"\") ? HtmlFolder : HtmlFolder + @"\";
                LocalFolder =
                    XmlOperations.GetXmlElements("/configurations/projects/project[@nId='" + nId + "']/localfolder", true).ToLower();
                LocalFolder = (LocalFolder.Substring(LocalFolder.Length - 1) == @"\") ? LocalFolder : LocalFolder + @"\";
                DeleteFiles =
                    XmlOperations.GetXmlElements("/configurations/projects/project[@nId='" + nId + "']/deletefiles").ToLower();
                DeleteFiles = (DeleteFiles == null) ? "false" : DeleteFiles;
                CallIndex =
                    XmlOperations.GetXmlElements("/configurations/projects/project[@nId='" + nId + "']/callindex").ToLower();
                CallIndex = (CallIndex == null) ? "false" : CallIndex;
                Language =
                    XmlOperations.GetXmlElements("/configurations/projects/project[@nId='" + nId + "']/language", true).ToLower();
                Extension =
                    XmlOperations.GetXmlElements("/configurations/projects/project[@nId='" + nId + "']/extension", true).ToLower();
                ProgrammingLanguage =
                    XmlOperations.GetXmlElements("/configurations/projects/project[@nId='" + nId + "']/programminglanguage", true).ToLower();
            }

            public void GetLocalElements(int nId)
            {
                ProjectTitle =
                    XmlOperations.GetXmlElements("/configurations/projects/project[@nId='" + nId + "']/title", true);
                HtmlFolder =
                    XmlOperations.GetXmlElements("/configurations/projects/project[@nId='" + nId + "']/html/htmlfolder",true).ToLower();
                HtmlFolder = (HtmlFolder.Substring(HtmlFolder.Length - 1) == @"\") ? HtmlFolder : HtmlFolder + @"\";
                LocalFolder =
                    XmlOperations.GetXmlElements("/configurations/projects/project[@nId='" + nId + "']/localfolder", true).ToLower();
                LocalFolder = (LocalFolder.Substring(LocalFolder.Length - 1) == @"\") ? LocalFolder : LocalFolder + @"\";
                DeleteFiles =
                    XmlOperations.GetXmlElements("/configurations/projects/project[@nId='" + nId + "']/deletefiles").ToLower();
                DeleteFiles = (DeleteFiles == null) ? "false" : DeleteFiles;
                CallIndex =
                    XmlOperations.GetXmlElements("/configurations/projects/project[@nId='" + nId + "']/callindex").ToLower();
                CallIndex = (CallIndex == null) ? "true" : CallIndex;
                Language =
                    XmlOperations.GetXmlElements("/configurations/projects/project[@nId='" + nId + "']/language",true).ToLower();
                Extension =
                    XmlOperations.GetXmlElements("/configurations/projects/project[@nId='" + nId + "']/extension", true).ToLower();
                ProgrammingLanguage =
                    XmlOperations.GetXmlElements("/configurations/projects/project[@nId='" + nId + "']/programminglanguage", true).ToLower();
            }
            public void LoadDictionaryList() => DictionaryList = XmlOperations.GetDictionaryList(Language);

            public void LoadRegexList() => RegexList = XmlOperations.GetRegexList(ProgrammingLanguage);

            public string Regex(string xmlitem)
            {
                var ret = RegexList.Find(x => x[0].Contains(xmlitem));
                return ret[1];
            }
        }
    }
}