using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Console_WSA_ProjDoc.General;
using Console_WSA_ProjDoc.XML;

namespace Console_WSA_ProjDoc
{
    public class FunctionIndexer
    {
        private static readonly Logging Logging = new Logging();
        private DirectoryInfo _d;

        public List<ProjectCalling>
            _functionCalls = new List<ProjectCalling>(); // Lista contendo todas as chamadas de funções

        //
        public List<ProjectMapping>
            _functionMatches = new List<ProjectMapping>(); // Lista contendo todas as declarações de funções

        private XmlConfigs.Xml Xml { get; set; } = new XmlConfigs.Xml();

        public void LoadXml(XmlConfigs.Xml xml)
        {
            this.Xml = xml;
            _d = new DirectoryInfo(xml.LocalFolder);
        }

        public bool FunctionMapping()
        {
            var breturn = false;
            var regexUFunctions = @"\b(User Function)\s+(?<range>\w+?)\b";
            var regexUfCalls = @"U_.*?\)";
            try
            {
                #region Leitura de todos os arquivos do Projeto
                Logging.WriteLog("Starting the function call indexing...");
                Logging.WriteLog("");

                var counter = _d.GetFiles("*" + Xml.Extension, SearchOption.AllDirectories).Length;
                var current = 0;
                foreach (var file in _d.GetFiles("*" + Xml.Extension, SearchOption.AllDirectories).Where(d => !d.FullName.ToString().Contains("$")))
                {
                    current++;
                    Logging.WriteLog(current + " of " + counter + " files indexed.", true);
                    var fullcode = ReplaceComments(File.ReadAllText(file.FullName)); // obter código completo, sem comentário
                    var lines = fullcode.Split(new string[] { "\n" },
                        StringSplitOptions.None);
                    var linecounter = 0;
                    foreach (var line in lines)
                    {
                        linecounter++;
                        #region Expressão Regular para buscar User Function
                        foreach (Match m in Regex.Matches(line, regexUFunctions, RegexOptions.IgnoreCase))
                            _functionMatches.Add(new ProjectMapping
                            {
                                FilePath = file.DirectoryName + @"\",
                                FileName = file.Name,
                                FunctionType = "User Function",
                                FunctionName = m.Groups[2].Value,
                                LineNumber = linecounter
                            });
                        #endregion 

                        #region Expressão Regular para buscar chamadas de User Function
                        var currPattern = @"(?<=U_).*?(?=\()";
                        foreach (Match m in Regex.Matches(line, regexUfCalls, RegexOptions.IgnoreCase))
                        {
                            var currregex = new Regex(currPattern, RegexOptions.IgnoreCase);
                            var currmatch = currregex.Match(m.Value);
                            var funname = "";
                            if (currmatch.Success) funname = currmatch.Value;

                            _functionCalls.Add(new ProjectCalling()
                            {
                                FilePath = file.DirectoryName + @"\",
                                FileName = file.Name,
                                FunctionType = "Static Function",
                                FunctionName = funname,
                                FunctionCall = m.Value,
                                LineNumber = linecounter
                            });
                        }
                        #endregion
                    }
                    #endregion
                }
                breturn = true;
            }
            catch (Exception e)
            {
                Logging.WriteLog("An Exception occurred during the function call indexing: " + e);
            }

            return breturn;
        }

        private string ReplaceComments(string Text)
        {
            #region REMOVER DO TEXTO BLOCOS DE CÓDIGO COM Protheus.Doc
            var regexPattern = @"(/\*/.*?/\*/)";
            var regex = new Regex(regexPattern, RegexOptions.Singleline);
            var match = regex.Match(Text);
            foreach (Match m in regex.Matches(Text))
            {
                Text = Text.Replace(m.Value, new String('\n', m.ToString().Count(f => f == '\n')).ToString());
            }
            #endregion

            #region REMOVER DO TEXTO BLOCOS DE CÓDIGO COM //
            regexPattern = @"\/\/.*";
            regex = new Regex(regexPattern);
            match = regex.Match(Text);
            foreach (Match m in regex.Matches(Text))
            {
                Text = Text.Replace(m.Value, "");
            }
            #endregion

            #region REMOVER DO TEXTO BLOCOS DE CÓDIGO COM /*
            regexPattern = @"\/\*.*?\*\/";
            regex = new Regex(regexPattern, RegexOptions.Singleline);
            match = regex.Match(Text);
            foreach (Match m in regex.Matches(Text))
            {
                Text = Text.Replace(m.Value, "");
            }
            #endregion
            return Text;
        }

        //
        public class ProjectMapping
        {
            public string FilePath { get; set; }
            public string FileName { get; set; }
            public string FunctionType { get; set; }
            public string FunctionName { get; set; }
            public int LineNumber { get; set; }
        }

        public class ProjectCalling
        {
            public string FilePath { get; set; }
            public string FileName { get; set; }
            public string FunctionType { get; set; }
            public string FunctionName { get; set; }
            public string FunctionCall { get; set; }
            public int LineNumber { get; set; }
        }
    }
}