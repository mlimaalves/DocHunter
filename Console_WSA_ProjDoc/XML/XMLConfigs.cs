namespace Console_WSA_ProjDoc.XML
{
    public class XmlConfigs
    {
        private static readonly XmlOperations XmlOperations = new XmlOperations();

        public class Xml
        {
            // Controle do Dicionário dinâmico:
            public System.Collections.Generic.List<string[]> Dictionary { get; set; }

            // Variáveis gerais, compartilhadas entre todos os projetos:
            public int Projects { get; set; } = XmlOperations.GetProjectCount("projeto");
            public string XmlFileName { get; set; } = XmlOperations.XmlFileName;
            public string AssemblyFolder { get; set; } = XmlOperations.AssemblyFolder;

            public string ServiceName { get; set; } =
                XmlOperations.GetXmlElements("/configuracoes/service/servicename");

            public string DisplayName { get; set; } =
                XmlOperations.GetXmlElements("/configuracoes/service/displayname");

            public string ServiceDescription { get; set; } =
                XmlOperations.GetXmlElements("/configuracoes/service/servicedescription");

            // Variáveis específicas para projetos do tipo Local:
            public string LocalFolder { get; set; }

            // Variáveis específicas para projetos do tipo TFS:
            public string TfsServerUrl { get; set; }
            public string TfsProject { get; set; }
            public string TfsFolder { get; set; }
            public string TfsUsername { get; set; }
            public string TfsPassword { get; set; }

            public string TfsChangesets { get; set; }

            //
            public string ProjectName { get; set; }
            public string HtmlFolder { get; set; }
            public string DeleteFiles { get; set; }
            public string Language { get; set; }
            //

            public string GetProjectType(int nId)
            {
                return XmlOperations.GetXmlProjectType(nId);
            }

            public void GetTfsElements(int nId)
            {
                ProjectName =
                    XmlOperations.GetXmlElements("/configuracoes/projetos/projeto[@nId='" + nId + "']/name", true);
                TfsServerUrl =
                    XmlOperations.GetXmlElements("/configuracoes/projetos/projeto[@nId='" + nId + "']/serverurl", true);
                TfsProject =
                    XmlOperations.GetXmlElements("/configuracoes/projetos/projeto[@nId='" + nId + "']/project", true);
                TfsFolder =
                    XmlOperations.GetXmlElements("/configuracoes/projetos/projeto[@nId='" + nId + "']/tfsfolder", true).ToLower();
                TfsUsername =
                    XmlOperations.GetXmlElements(
                        "/configuracoes/projetos/projeto[@nId='" + nId + "']/networkcredential/username", true);
                TfsPassword =
                    XmlOperations.GetXmlElements(
                        "/configuracoes/projetos/projeto[@nId='" + nId + "']/networkcredential/password", true);
                TfsChangesets =
                    XmlOperations.GetXmlElements("/configuracoes/projetos/projeto[@nId='" + nId + "']/changesets");
                //
                HtmlFolder =
                    XmlOperations.GetXmlElements("/configuracoes/projetos/projeto[@nId='" + nId + "']/html/htmlfolder",
                        true).ToLower();
                DeleteFiles =
                    XmlOperations.GetXmlElements("/configuracoes/projetos/projeto[@nId='" + nId + "']/deletefiles");
                Language =
                    XmlOperations.GetXmlElements("/configuracoes/projetos/projeto[@nId='" + nId + "']/language",true).ToLower();
            }

            public void GetLocalElements(int nId)
            {
                ProjectName =
                    XmlOperations.GetXmlElements("/configuracoes/projetos/projeto[@nId='" + nId + "']/name", true);
                LocalFolder =
                    XmlOperations.GetXmlElements("/configuracoes/projetos/projeto[@nId='" + nId + "']/localfolder",true);
                HtmlFolder =
                    XmlOperations.GetXmlElements("/configuracoes/projetos/projeto[@nId='" + nId + "']/html/htmlfolder",true);
                DeleteFiles =
                    XmlOperations.GetXmlElements("/configuracoes/projetos/projeto[@nId='" + nId + "']/deletefiles");
                Language =
                    XmlOperations.GetXmlElements("/configuracoes/projetos/projeto[@nId='" + nId + "']/language",true).ToLower();
            }
            public void LoadDictionary() => Dictionary = XmlOperations.GetXmlDictionary(Language);
        }
    }
}