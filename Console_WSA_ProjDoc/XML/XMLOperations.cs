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

        private readonly XmlDocument _xml = new XmlDocument();

        #endregion

        public XmlOperations()
        {
            _xml.Load(Assemblyfolder + Xmlfilename);
        }

        public string GetXmlElements(string strnode, bool obrigatorio = false)
        {
            var sret = "";
            if (_xml.DocumentElement.SelectSingleNode(strnode) == null)
            {
                // Tratativas caso a tag do parâmetro não seja encontrada no arquivo .XML:
                if (obrigatorio)
                {
                    Logging.WriteLog("Erro: O seguinte nó é obrigatório e não foi encontrado no arquivo XML:\n" +
                                     strnode);
                    Environment.Exit(1);
                }
            }
            else
            {
                sret = _xml.DocumentElement.SelectSingleNode(strnode).InnerText;
            }

            return sret;
        }

        public int GetProjectCount(string strnode)
        {
            return _xml.DocumentElement.GetElementsByTagName(strnode).Count;
        }

        public void GetXmlProjectElements()
        {
        }

        public string GetXmlProjectType(int nId = 0)
        {
            var creturn = "";
            var xmLtype = "";
            var typelist = new List<string>();
            /* tipos de projeto válidos para criar documentação */
            typelist.Add("TFS");
            typelist.Add("LOCAL");

            if (nId > 0)
                xmLtype = _xml.DocumentElement.SelectSingleNode("/configuracoes/projetos/projeto[@nId='" + nId + "']")
                    .Attributes["type"].InnerText.ToUpper();
            if (typelist.Find(c => c == xmLtype) != null) creturn = xmLtype;

            return creturn;
        }

        public List<string[]> GetXmlDictionary(string language)
        {
            var ret = new List<string[]>();
            XmlDocument _xmldic = new XmlDocument();
            _xml.Load(Assemblyfolder + @"languages\" + language + ".xml");
            XmlElement root = _xml.DocumentElement;
            XmlNodeList nodelist = root.SelectNodes("item");
            foreach (XmlNode node in nodelist)
            {
                var str = new string[2];
                str[0] = node.Attributes[0].InnerText; // id
                str[1] = node.FirstChild.InnerText; // texto do id
                ret.Add(str);
            }
            return ret;
        }

        #region Diretório do Assembly do executável

        private static readonly string Assemblyfolder =
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\";

        private static readonly Logging Logging = new Logging();

        public string AssemblyFolder => Assemblyfolder;

        #endregion

        #region Arquivo de Configurações:

        private static readonly string Xmlfilename = "appserver.xml";

        public string XmlFileName => Xmlfilename;

        #endregion
    }
}