using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using Console_WSA_ProjDoc.General;

namespace Console_WSA_ProjDoc.XML
{
    public class XmlOperations
    {
        #region Variáveis para leitura do arquivo XML (config)

        private readonly XmlDocument _xmlSettings = new XmlDocument();

        #endregion

        public XmlOperations()
        {
            _xmlSettings.Load(Assemblyfolder + Xmlfilename);
        }

        public string GetXmlElements(string strnode, bool obrigatorio = false)
        {
            var sret = "";
            if (_xmlSettings.DocumentElement.SelectSingleNode(strnode) == null)
            {
                // Tratativas caso a tag do parâmetro não seja encontrada no arquivo .XML:
                if (obrigatorio)
                {
                    Logging.WriteLog("ERROR: THE FOLLOWING NODE IS MANDATORY AND IT WAS NOF FOUND IN THE configurations.xml FILE:\n" +
                                     strnode);
                    Environment.Exit(1);
                }
            }
            else
            {
                sret = _xmlSettings.DocumentElement.SelectSingleNode(strnode).InnerText;
            }

            return sret;
        }

        public int GetProjectCount(string strnode)
        {
            return _xmlSettings.DocumentElement.GetElementsByTagName(strnode).Count;
        }

        public void GetXmlProjectElements()
        {
        }

        public string GetXmlProjectType(int nId = 0)
        {
            var creturn = "";
            var xmLtype = "";
            var typelist = new List<string>
            {
                // project types that are valid to create a Documentation:
                "tfvc",
                "local"
            };

            if (nId > 0)
                xmLtype = _xmlSettings.DocumentElement.SelectSingleNode("/configurations/projects/project[@nId='" + nId + "']")
                    .Attributes["type"].InnerText.ToLower();
            if (typelist.Find(c => c == xmLtype) != null) creturn = xmLtype;

            return creturn;
        }

        public List<string[]> GetDictionaryList(string language)
        {
            var ret = new List<string[]>();
            XmlDocument _xmlDic = new XmlDocument();
            if (File.Exists(Assemblyfolder + @"languages\" + language + ".xml"))
            {
                _xmlDic.Load(Assemblyfolder + @"languages\" + language + ".xml");
                XmlElement root = _xmlDic.DocumentElement;
                XmlNodeList nodelist = root.SelectNodes("item");
                foreach (XmlNode node in nodelist)
                {
                    var str = new string[2];
                    str[0] = node.Attributes[0].InnerText; // id
                    str[1] = node.FirstChild.InnerText; // texto do id
                    ret.Add(str);
                }
            }
            else Logging.WriteLog(@"ATTENTION: THE LANGUAGE FILE \LANGUAGES\" + language + ".xml DOES NOT EXISTS. THE HTML TEXTS WILL NOT BE SHOWN CORRECTLY.");
            return ret;
        }

        public List<string[]> GetRegexList(string programminglanguage)
        {
            var ret = new List<string[]>();

            XmlDocument _xmlDic = new XmlDocument();
            if (File.Exists(Assemblyfolder + @"regex\" + programminglanguage + ".xml"))
            {
                _xmlDic.Load(Assemblyfolder + @"regex\" + programminglanguage + ".xml");
                XmlElement root = _xmlDic.DocumentElement;
                XmlNodeList nodelist = root.SelectNodes("item");
                foreach (XmlNode node in nodelist)
                {
                    var str = new string[2];
                    str[0] = node.Attributes[0].InnerText; // id
                    str[1] = node.FirstChild.InnerText; // texto do id
                    ret.Add(str);
                }
            }
            else Logging.WriteLog(@"ERROR: THE PROGRAMMING LANGUAGE FILE \REGEX\" + programminglanguage+ ".xml DOES NOT EXISTS. THE PROJECT DOCUMENTATION WILL NOT BE CREATED.");
            return ret;
        }

        #region Diretório do Assembly do executável

        private static readonly string Assemblyfolder =
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\";

        private static readonly Logging Logging = new Logging();

        public string AssemblyFolder => Assemblyfolder;

        #endregion

        #region Arquivo de Configurações:

        private static readonly string Xmlfilename = "configurations.xml";

        public string XmlFileName => Xmlfilename;

        #endregion
    }
}