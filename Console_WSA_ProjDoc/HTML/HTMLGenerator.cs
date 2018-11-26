using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Console_WSA_ProjDoc.CSS;
using Console_WSA_ProjDoc.General;
using Console_WSA_ProjDoc.XML;
using Console_WSA_ProjDoc.SHA1;

namespace Console_WSA_ProjDoc.HTML
{
    public class HtmlGenerator
    {
        private static readonly Logging Logging = new Logging();
        private static FileHash FileHash = new FileHash();
        private XmlConfigs.Xml Xml { get; set; } = new XmlConfigs.Xml();
        private static readonly string Assemblyfolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\";
        private string Tree = "";
        private string CodeErrorList = "";
        private FunctionIndexer FunctionIndexer;
        public void LoadXml(XmlConfigs.Xml xml) => this.Xml = xml;

        public bool Start()
        {
            var lret = false;
            try
            {
                Logging.WriteLog("Iniciando a criação da documentação...");
                CreateDocumentation();
                Logging.WriteLog("Documentação finalizada...");
                lret = true;
            }
            catch (IOException e)
            {
                Logging.WriteLog("Erro de Input/Output.: \n" + e);
            }
            catch (Exception e)
            {
                Logging.WriteLog("Exceção durante montagem de projeto HTML: \n" + e);
            }

            if (!lret) Environment.Exit(0);
            return lret;
        }

        private bool CreateDocumentation()
        {
            const bool lRet = false;
            var htmlFolder = HtmlFormatters.URLReplace(Xml.HtmlFolder + Xml.ProjectName);
            var file = "";
            var dir = new DirectoryInfo(htmlFolder);
            try
            {
                if (Xml.DeleteFiles == "true" && Directory.Exists(htmlFolder)) dir.Delete(true);
            }
            catch (IOException e)
            {
                Logging.WriteLog("Não foi possível excluir o diretório " + dir.FullName + ". Continuando execução");
            }
            Directory.CreateDirectory(htmlFolder);

            #region Copiando arquivos css originais do bináiro para o diretório HTML:
            var css = new CssGenerator();
            css.LoadXml(Xml);
            css.CopyCss();
            #endregion

            #region CRIANDO ÁRVORE DE PASTAS E ROTINAS PARA A NAVEGAÇÃO PRINCIPAL
            Create_Tree(Xml.TfsFolder + @"\");
            // Ajuste final na árvore:
            Tree = Tree.Replace(@"},]", @"}]").Replace(@"],}", @"]}");
            Tree = Tree.Substring(0, Tree.Length - 2);
            #endregion 

            #region CRIANDO ARQUIVO NAVIGATION PRINCIPAL (MAIN.HTML)
            //Iniciando o projeto com o arquivo Main.Html
            file = htmlFolder + @"\main.html";
            CreateHTMLFile(file, "Navigation.html");
            Replace_TreeArea(file);
            Replace_NameArea(file, Xml.ProjectName);
            Replace_ButtonArea(Xml.TfsFolder, file);
            Replace_FileProtocolArea(file);
            Replace_SearchArea(file);
            Replace_BreadcrumbArea(file, file);
            Replace_Dictionary(file);

            Logging.WriteLog("Arquivo Main.html criado com sucesso. Iniciando a criação da estrutura de código-fonte...");
            #endregion

            #region CRIANDO ARQUIVOS CODE
            var tfsFolder = new DirectoryInfo(Xml.TfsFolder);
            var counter = tfsFolder.GetFiles("*.prw", SearchOption.AllDirectories).Length;
            var current = 0;
            var Indexed = false;
            foreach (var codefile in tfsFolder.GetFiles("*.prw", SearchOption.AllDirectories)
                .Where(d => !d.FullName.ToString().Contains("$") && !d.FullName.ToString().Contains(".vscode")))
            {
                current++;
                Logging.WriteLog(current + " de " + counter + " arquivos processados.", true);
                var originalfilename = codefile.FullName;
                var newdirectory = WebUtility.HtmlEncode(codefile.Directory.FullName);
                newdirectory = htmlFolder + HtmlFormatters.URLReplace(newdirectory.Replace(Xml.TfsFolder, ""));
                Directory.CreateDirectory(newdirectory);

                file = HtmlFormatters.URLReplace(codefile.Name) + ".html";
                file = newdirectory + @"\" + file;
                byte[] result;

                System.Security.Cryptography.SHA1 sha = new System.Security.Cryptography.SHA1CryptoServiceProvider();
                // This is one implementation of the abstract class SHA1.
                FileStream fs = File.OpenRead(originalfilename);
                result = sha.ComputeHash(fs);
                var hash = "";
                foreach(var line in result) hash += line.ToString();
                fs.Close();
                if (!File.Exists(file) || FileHash.CompareHash(originalfilename, hash))
                {
                    if (!Indexed)
                    {
                        FunctionIndexer = new FunctionIndexer();
                        FunctionIndexer.LoadXml(Xml);
                        FunctionIndexer.FunctionMapping();
                        Indexed = true;
                    }

                    CreateHTMLFile(file, "Code.html");
                    Replace_NameArea(file, Xml.ProjectName);
                    Replace_FileNameArea(file, codefile.Name);
                    Replace_FileProtocolArea(file);
                    Replace_SearchArea(file);
                    Replace_BreadcrumbArea(file, originalfilename);
                    //Replace_ChangesetsArea(file, codefile.FullName);
                    RegExDocumentation(file, codefile.FullName);
                    RegExCode(file, codefile.FullName);
                    Replace_ChangesetsArea(file, codefile.FullName);
                    Replace_Dictionary(file);
                }
            }
            #endregion

            return lRet;
        }

        private void CreateHTMLFile(string file, string filetype)
        {
            var f = new FileInfo(file);
            var navFolder = Assemblyfolder + @"HTML\";
            var htmlfile = navFolder + filetype;
            if (File.Exists(file)) File.Delete(file);
            File.Copy(htmlfile, file, true);
        }
        private void Replace_NameArea(string file, string newText)
        {
            File.WriteAllText(file, File.ReadAllText(file).Replace("&nameArea&", newText));
            return;
        }
        private void Replace_FileNameArea(string file, string newText)
        {
            File.WriteAllText(file, File.ReadAllText(file).Replace("&fileNameArea&", newText));
            return;
        }
        private void Replace_ButtonArea(string HTMLFolder, string mainfile)
        {
            var currentFolder = new DirectoryInfo(HTMLFolder);
            var buttonList = "";
            var buttonCollapseList = "";
            var f = new FileInfo(mainfile);

            foreach (var directory in currentFolder.GetDirectories("*.*", SearchOption.TopDirectoryOnly)
                .Where(d => !d.FullName.ToString().Contains("$") && !d.FullName.ToString().Contains(".vscode")))
            {
                var identifier = directory.Name;
                identifier = HtmlFormatters.URLReplace(identifier);
                var buttonText = WebUtility.HtmlEncode(directory.Name);
                buttonList += "							<button " +
                              "type='button' " +
                              "class='btn btn-secondary btn-lg btn-custom-main' " +
                              "data-toggle='collapse' " +
                              "data-target='#" + identifier + "' " +
                              "aria-expanded='false' " +
                              "aria-controls='" + identifier + "'>" +
                              buttonText + "</button>" + Environment.NewLine;

                //
                #region Criando collapses para subpastas dentro do Button
                buttonCollapseList += "					<div class='collapse'  id='" + identifier + "'>" + Environment.NewLine;
                buttonCollapseList += "						<div class='card card-body'>" + Environment.NewLine;
                buttonCollapseList += "						<h3>Diretório " + buttonText + "</h3>" + Environment.NewLine;
                buttonCollapseList += "							<ul class='list-group'>" + Environment.NewLine;

                var innerFolder = new DirectoryInfo(directory.FullName);
                foreach (var subdirectories in innerFolder.GetDirectories("*.*", SearchOption.TopDirectoryOnly))
                {
                    var subText = WebUtility.HtmlEncode(subdirectories.Name);

                    var href = directory.Name + @"\" + subdirectories.Name + @"\" + subdirectories.Name;
                    href = HtmlFormatters.URLReplace(href) + ".html";
                    buttonCollapseList += "                               <li class='list-group-item'>" +
                                          "<a class='a-custom-main' " +
                                          "href='" + href + "'>" +
                                          "<i class='far fa-folder fa-custom'></i>" +
                                          subText + "</a></li>" + Environment.NewLine;
                }
                foreach (var subfiles in innerFolder.GetFiles("*.prw*", SearchOption.TopDirectoryOnly))
                {
                    var subText = WebUtility.HtmlEncode(subfiles.Name);

                    var href = directory.Name + @"\" + subfiles.Name;
                    href = HtmlFormatters.URLReplace(href) + ".html";
                    buttonCollapseList += "                               <li class='list-group-item'>" +
                                          "<a class='a-custom-main' " +
                                          "href='" + href + "'>" +
                                          "<i class='far fa-file-code fa-custom'></i>" +
                                          subText + "</a></li>" + Environment.NewLine;
                }
                #endregion
                buttonCollapseList += "							</ul>" + Environment.NewLine;
                buttonCollapseList += "						</div>" + Environment.NewLine;
                buttonCollapseList += "					</div>" + Environment.NewLine;


            }

            #region loop em ARQUIVOS- Gera uma tag <a> para cada arquivo encontrado

            foreach (var codefile in currentFolder.GetFiles("*.prw", SearchOption.TopDirectoryOnly)
                .Where(d => !d.FullName.ToString().Contains("$") && !d.FullName.ToString().Contains(".vscode")))
            {
                var href = codefile.Name;
                href = HtmlFormatters.URLReplace(href) + ".html";
                var buttonText = WebUtility.HtmlEncode(codefile.Name);
                buttonList += "							<a " +
                              "role='button' " +
                              "class='btn btn-secondary btn-lg btn-custom-main' " +
                              "href='" + href + "'>" +
                              buttonText + "</a>" + Environment.NewLine;
            }

            #endregion

            File.WriteAllText(mainfile, File.ReadAllText(mainfile).Replace("&btnArea&", buttonList).Replace("&btnCollapseArea&", buttonCollapseList));
            return;
        }

        private void Replace_BreadcrumbArea(string file, string originalfilename)
        {
            var f = new FileInfo(originalfilename.ToLower());
            //var replace = HtmlFormatters.URLReplace(Xml.HtmlFolder + Xml.ProjectName);
            var replace = (Xml.HtmlFolder + Xml.ProjectName).ToLower();
            var breadcrumbfolders = f.FullName.Replace(replace, "").Replace(@"\main.html", "");
            var folderSplit = f.Name == "main.html" ? new String[1] : breadcrumbfolders.Split(Path.DirectorySeparatorChar);
            var breadcrumbText = "";
            int x;
            for (x = 0; x < folderSplit.Length; x++)
            {
                if (folderSplit[x] == null) folderSplit[x] = "";
                var currentpath = HtmlFormatters.URLReplace(Xml.HtmlFolder + Xml.ProjectName);
                int y;
                for (y = 0; y < x + 1; y++)
                {
                    currentpath += folderSplit[y] + @"\";
                }

                if (f.Name != "main.html" && x == 0)
                {
                    var goback = string.Concat(Enumerable.Repeat("../", folderSplit.Length - 2));
                    var href = currentpath.Replace(HtmlFormatters.URLReplace(Xml.HtmlFolder + Xml.ProjectName + @"\"), "") +
                               ((folderSplit[x] == "") ? "main" : folderSplit[x]);
                    href = goback + HtmlFormatters.URLReplace(href) + ".html";
                    breadcrumbText += "							<li " +
                                      "class='breadcrumb-item'>" +
                                      "<a href='" + href + "'>" +
                                      ((folderSplit[x] == "") ? Xml.ProjectName : folderSplit[x].First().ToString().ToUpper() + folderSplit[x].Substring(1).ToLower()) +
                                      "</a></li>" +
                                      Environment.NewLine;
                }
                else
                {
                    breadcrumbText += "							<li " +
                                      "class='breadcrumb-item active' " +
                                      "aria-current='page'>" +
                                      ((folderSplit[x] == "") ? Xml.ProjectName : folderSplit[x].First().ToString().ToUpper() + folderSplit[x].Substring(1).ToLower()) +
                                      "</li>" +
                                      Environment.NewLine;
                }
            }
            File.WriteAllText(file, File.ReadAllText(file).Replace("&breadcrumbArea&", breadcrumbText));
        }

        private void Replace_ChangesetsArea(string file, string originalfilename)
        {
            var f = new FileInfo(originalfilename);
            var csfile = f.Directory.FullName + @"\" + f.Name.Replace(".prw", "") + ".#cs";
            var currcs = "";
            var csnav = "";
            DateTime currdatetime = DateTime.Now;
            var cscount = 0;
            var search = csfile;


            if (Xml.TfsChangesets == "show" && File.Exists(csfile))
            {
                csnav += "						<li class='nav-item'>" + Environment.NewLine;
                csnav += "							<a class='nav-link' id='changesets-tab' data-toggle='tab' href='#changesets' role='tab' aria-controls='changesets' aria-selected='false'>&Dic:tab_changesets&</a>" + Environment.NewLine;
                csnav += "						</li>";
                var lines = File.ReadAllText(csfile).Split(new string[] { "\n" },
                StringSplitOptions.None);
                var linecounter = 0;
                var csDatetime = DateTime.Today;
                var temptitle = "";
                var tempcomment = "";
                foreach (var line in lines)
                {
                    if (line.Contains("@SHA1: "))
                    {
                    }
                    else if (line.Contains("@CreationDate:"))
                    {
                        csDatetime = DateTime.Parse(line.Replace("@CreationDate: ", ""));
                        if (currcs == "" || csDatetime.ToString("dd/MM/yyyy hh:mm:ss") != currdatetime.ToString("dd/MM/yyyy hh:mm:ss"))
                        {
                            if (currcs != "") currcs += "								</li>" + Environment.NewLine; // Fechando já existente
                            currcs += "								<li class='list-group-item'>" + Environment.NewLine; // Criando novo grupo de changeset
                            currdatetime = csDatetime;
                            cscount = 0;
                            currcs += "									<div class='text-muted font-weight-bold'>" +
                               currdatetime.ToString("dddd, dd/MMM/yyyy", new System.Globalization.CultureInfo(Xml.Language)) + ", " + currdatetime.ToString("hh:mm:ss") + ": </div>" + Environment.NewLine;
                            currcs += "<p></p>" + Environment.NewLine;

                        }
                    }
                    else if (line.Contains("@Comment:"))
                    {
                        currcs += "									<div class='custom-changeset'>" + Environment.NewLine;
                        currcs += "										<h6 " +
                            "class='custom-changeset-title' " +
                            "data-toggle='tooltip' " +
                            "data-placement='bottom' title='&newTitle&'>" + Environment.NewLine+
                            "&newComment&" + Environment.NewLine;
                        tempcomment = line.Replace("@Comment: ", "");
                        temptitle = tempcomment + Environment.NewLine;
                    }
                    else if (line.Contains("@Changeset:"))
                    {
                        currcs =  currcs.Replace("&newTitle&", temptitle).Replace("&newComment&", tempcomment);
                        currcs += "										<div class='text-muted font-weight-normal'>" +
                            "Changeset " + line.Replace("@Changeset: ", "") + "</div>" + Environment.NewLine;
                        currcs += "										</h6>" + Environment.NewLine;
                        currcs += "									</div>" + Environment.NewLine ; // fim changeset

                    }
                    else // continuação comentário
                    {
                        tempcomment += line;
                        temptitle += line + Environment.NewLine;
                    }
                    cscount++;
                }
            }
            File.WriteAllText(file, File.ReadAllText(file).Replace("&ChangesetsNavArea&", csnav));
            File.WriteAllText(file, File.ReadAllText(file).Replace("&ChangesetsArea&", currcs));
        }

        private void Replace_SearchArea(string file)
        {
            var tfsFolder = new DirectoryInfo(Xml.TfsFolder);
            var searchList = "";
            var f = new FileInfo(file);
            foreach (var codefile in tfsFolder.GetFiles("*.prw", SearchOption.AllDirectories).Where(d => !d.FullName.ToString().Contains("$") && !d.FullName.ToString().Contains(".vscode")))
            {
                var searchText = WebUtility.HtmlEncode(Xml.ProjectName + codefile.FullName.Replace(Xml.TfsFolder, ""));
                var replace = HtmlFormatters.URLReplace(Xml.HtmlFolder + Xml.ProjectName);
                string[] split = f.Directory.FullName.Replace(replace, "").Split(Path.DirectorySeparatorChar);
                var goback = string.Concat(Enumerable.Repeat("../", split.Length - 1));
                var href = codefile.FullName.Replace(Xml.TfsFolder + @"\", "");
                href = goback + HtmlFormatters.URLReplace(href) + ".html";
                searchList += "			    	<li " +
                              "class='nav-item text-right'><a " +
                              "class='search-item nav-link a-custom-main' " +
                              "href='" + href + "'>" +
                              searchText +
                              "</a></li>" + Environment.NewLine;
            }
            File.WriteAllText(file, File.ReadAllText(file).Replace("&searchArea&", searchList));
        }

        private void Replace_FileProtocolArea(string file)
        {
            var f = new FileInfo(file);
            var replace = HtmlFormatters.URLReplace(Xml.HtmlFolder + Xml.ProjectName);
            string[] split = f.Directory.FullName.Replace(replace, "").Split(Path.DirectorySeparatorChar);
            var goback = string.Concat(Enumerable.Repeat("../", split.Length - 1));
            File.WriteAllText(file, File.ReadAllText(file).Replace("&fileProtocolArea&", goback));
        }

        private void Replace_Dictionary(string file)
        {
            foreach(string[] line in Xml.Dictionary)
            {
                File.WriteAllText(file, File.ReadAllText(file).Replace("&Dic:" + line[0] + "&", line[1]));
            }
        }

        private void Replace_TreeArea(string file) => File.WriteAllText(file, File.ReadAllText(file).Replace("&treeArea&", Tree));

        private void Replace_ErrorArea(string file) => File.WriteAllText(file, File.ReadAllText(file).Replace("&ErrorArea&", CodeErrorList));

        private void Replace_SyntaxArea(string file, string syntax) => File.WriteAllText(file, File.ReadAllText(file).Replace("&SyntaxArea&", syntax));

        private void RegExCode(string htmlfile, string codefile)
        {
            var fullcode = HtmlFormatters.StringReplace(File.ReadAllText(codefile, Encoding.GetEncoding("Windows-1252")));
            var newCode = "";

            var lines = fullcode.Split(new string[] { "\n" },
                StringSplitOptions.None);
            var linecounter = 0;
            foreach (var line in lines)
            {
                newCode += "<tr><td class='codebox linenumber' id='"+(linecounter + 1) + "'>" + (linecounter + 1) +
                          "</td><td><pre>" + lines[linecounter] + "</pre></td></tr>";
                linecounter++;
            }
            File.WriteAllText(htmlfile, File.ReadAllText(htmlfile).Replace("&CodeBoxArea&", newCode));
        }

        private void RegExDocumentation(string htmlfile, string codefile)
        {
            //
            string protheusDoc = "";
            string description = "";
            string wherevar = "";
            List<string> todolist = new List<string>();
            List<string[]> param = new List<string[]>();
            List<string[]> returnvar = new List<string[]>();
            //
            var search = "";
            var fullcode = HtmlFormatters.StringReplace(File.ReadAllText(codefile, Encoding.GetEncoding("Windows-1252")));
            var regexPattern = "";

            CodeErrorList = "";

            #region OBTER O PRIMEIRO PROTHEUS.DOC
            regexPattern = @"(/\*/.*?/\*/)";
            Regex regex = new Regex(regexPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
            search = fullcode;
            Match match = regex.Match(search);

            fullcode = ReplaceComments(fullcode);
            if (match.Success)
            {
                protheusDoc = match.Value;
                #region PROTHEUS.DOC - OBTER DESCRIÇÃO (LINHAS ANTES DO PRIMEIRO PARÂMETRO)
                regexPattern = @"\n.*?(?=@)";
                regex = new Regex(regexPattern, RegexOptions.Singleline);
                search = protheusDoc;
                match = regex.Match(search);
                if (match.Success) description = match.Value.Replace("\n", "<br>");
                #endregion

                #region PROTHEUS.DOC - OBTER OBSERVAÇÕES)
                regexPattern = @"@where.*";
                regex = new Regex(regexPattern, RegexOptions.IgnoreCase);
                search = protheusDoc;
                match = regex.Match(search);
                if (match.Success) wherevar = ReplacepDocPar("@where", match.Value); // removendo texto @where e espaços
                #endregion

                #region PROTHEUS.DOC - OBTER ARRAY @param
                regexPattern = @"@param.*";
                regex = new Regex(regexPattern, RegexOptions.IgnoreCase);
                search = protheusDoc;

                if (regex.Matches(search).Count > 0)
                {
                    foreach (Match m in regex.Matches(search))
                    {
                        var currparam = ReplacepDocPar("@param", m.Value); // removendo texto @param e espaços
                        var currregex = new Regex(@".*?,", RegexOptions.IgnoreCase);
                        var matchcollection = currregex.Matches(currparam);
                        if (matchcollection.Count > 0)
                        {
                            if (matchcollection.Count > 1)
                            {
                                var str = new string[3];
                                currparam = currparam.Replace(matchcollection[0].Value, "");
                                currparam = currparam.Replace(matchcollection[1].Value, "");
                                str[0] = matchcollection[0].Value.Replace(",", "").Replace(" ", "");
                                str[1] = matchcollection[1].Value.Replace(",", "").Replace(" ", "").ToLower();
                                str[2] = currparam; // Descrição = resto do @param que não foi validado pelo RegEx
                                param.Add(str);
                            }
                        }
                        else
                        {
                            var errormsg = "";
                            errormsg = "&Dic:err_param&";

                            FeedError("danger", errormsg);
                        }
                    }
                }
                else // Nenhum @param foi identificado no Protheus.Doc
                {
                    var errormsg = "";
                    errormsg = "&Dic:err_paramnotfound&";

                    FeedError("danger", errormsg);
                }

                #endregion

                #region PROTHEUS.DOC - OBTER ARRAY @ToDo
                regexPattern = @"@todo.*";
                regex = new Regex(regexPattern, RegexOptions.IgnoreCase);
                search = protheusDoc;
                foreach (Match m in regex.Matches(search))
                {
                    var currtodo = ReplacepDocPar("@todo", m.Value); // removendo texto @Todo e espaços
                    todolist.Add((todolist.Count + 1) + ") " + currtodo);
                }
                #endregion

                #region PROTHEUS.DOC - OBTER ARRAY @return (RETORNO DA FUNÇÃO)
                regexPattern = @"@return.*";
                regex = new Regex(regexPattern, RegexOptions.IgnoreCase);
                search = protheusDoc;
                if (regex.Matches(search).Count > 0)
                {
                    foreach (Match m in regex.Matches(search))
                    {
                        var currreturn = ReplacepDocPar("@return", m.Value); // removendo texto @return e espaços
                        var currregex = new Regex(@".*?,", RegexOptions.IgnoreCase);
                        var matchcollection = currregex.Matches(currreturn);
                        if (matchcollection.Count > 0)
                        {
                            if (matchcollection.Count > 1)
                            {
                                var str = new string[3];
                                currreturn = currreturn.Replace(matchcollection[0].Value, "");
                                currreturn = currreturn.Replace(matchcollection[1].Value, "");
                                str[0] = matchcollection[0].Value.Replace(",", "").Replace(" ", "");
                                str[1] = matchcollection[1].Value.Replace(",", "").Replace(" ", "").ToLower();
                                str[2] = currreturn; // Descrição = resto do @return que não foi validado pelo RegEx
                                returnvar.Add(str);
                            }
                        }
                        else
                        {
                            var errormsg = "";
                            errormsg = "&Dic:err_return&";

                            FeedError("danger", errormsg);
                        }
                    }
                }
                else // Nenhum @return foi identificado no Protheus.Doc
                {
                    var errormsg = "";
                    errormsg = "&Dic:err_returnnotfound&";

                    FeedError("danger", errormsg);
                }
                #endregion
            }
            else
            {
                var errormsg = "";
                errormsg = "&Dic:err_pdoc&";
                FeedError("danger", errormsg);
            }

            // Após analisar todas as variáveis, substitui o arquivo HTML:
            pDocDescription(htmlfile, description);
            pDocWhere(htmlfile, wherevar);
            pDocParam(htmlfile, param);
            pDocReturn(htmlfile, returnvar);
            pDocToDo(htmlfile, todolist);
            #endregion

            #region OBTER NOME E SINTAXE DA FUNÇÃO (PRIMEIRO ITEM DO Match)
            var functions = fullcode;
            ReplaceComments(functions);
            regexPattern = @"(.*function)\s+(\w.*)";
            regex = new Regex(regexPattern, RegexOptions.IgnoreCase);
            match = regex.Match(functions);
            if (match.Success)
            {
                var funtype = match.Groups[1].Value.ToLower().Contains("user") ? "U_" : ""; // Grupo 1 = Tipo da Função
                var funcall = match.Groups[2].Value; // Nome da Função + Parâmetros
                Replace_SyntaxArea(htmlfile, funtype + funcall);
            }
            #endregion
            
            #region OBTER REFERÊNCIAS/CHAMADAS DAS USERFUNCTIONS EM OUTROS ARQUIVOS
            var cfunref = "";

            foreach (FunctionIndexer.ProjectMapping f in FunctionIndexer._functionMatches.Where(x => x.FilePath.ToUpper() + x.FileName.ToUpper() == codefile.ToUpper()))
            {
                if (f.FunctionType == "User Function")
                {
                    foreach (FunctionIndexer.ProjectCalling c in FunctionIndexer._functionCalls.Where(x => x.FunctionName.ToUpper() == f.FunctionName.ToUpper()))
                    {
                        var currfiledir =
                            new FileInfo(htmlfile).Directory.FullName.Replace(
                                Xml.HtmlFolder + HtmlFormatters.URLReplace(Xml.ProjectName) + @"\", "");
                        var folderSplit = currfiledir.Split(Path.DirectorySeparatorChar);
                        var goback = string.Concat(Enumerable.Repeat("../", folderSplit.Length));
                        var calldir = HtmlFormatters.URLReplace(c.FilePath.Replace(Xml.TfsFolder + @"\", "") + c.FileName)+".html";
                            //htmlfile.Replace(Xml.HtmlFolder + HtmlFormatters.URLReplace(Xml.ProjectName) + @"\", "");

                        cfunref += "									<a " +
                                  "class='card-text custom-card-text4' " +
                                  "href='" + goback + calldir + "#" + c.LineNumber + "'>" +
                                  c.FilePath.Replace(Xml.TfsFolder + @"\", "") + c.FileName + " => U_" + c.FunctionName + ", Linha " + c.LineNumber + "</a><br>" + Environment.NewLine;

                    }
                }
            }
            cfunref += "<p></p>";
            Replace_FunctionCallArea(htmlfile, cfunref);
            #endregion
            
            Replace_ErrorArea(htmlfile);
        }

        private string ReplacepDocPar(string pattern, string Text)
        {
            var regexPattern = pattern + @".*?(?=[A-Za-z|1-9])";
            var regex = new Regex(regexPattern, RegexOptions.Singleline);
            var match = regex.Match(Text);
            if (match.Success) return Text.Replace(match.Value, "");
            else return Text;
        }

        private string ReplaceComments(string Text)
        {
            #region REMOVER DO TEXTO BLOCOS DE CÓDIGO COM Protheus.Doc
            var regexPattern = @"(/\*/.*?/\*/)";
            var regex = new Regex(regexPattern, RegexOptions.Singleline);
            var match = regex.Match(Text);
            foreach (Match m in regex.Matches(Text))
            {
                Text = Text.Replace(m.Value, "");
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

        private void pDocParam(string file, List<string[]> param)
        {
            var newText = "";
            if (param.Count > 0)
            {
                foreach (var p in param)
                {
                    //gerar: [Nome_Parametro, Tipo_Parametro]
                    newText += "									<h6 " +
                               "class='card-subtitle mb-2 text-muted'>" +
                               p[0] + ", " + p[1] + "</h6>" + Environment.NewLine;
                    //gerar: [Nome_Parametro, Tipo_Parametro, Descricao_Parametro]
                    newText += "									<p " +
                               "class='card-text custom-card-text'><i>" +
                               p[2] + "</i></p>" + Environment.NewLine;
                }
            }
            else newText += "<p></p>" + Environment.NewLine;

            File.WriteAllText(file, File.ReadAllText(file).Replace("&pDoc:Param&", newText));
        }

        private void pDocReturn(string file, List<string[]> returnvar)
        {
            var newText = "";
            if (returnvar.Count > 0)
            {
                for (int i = 0; i < returnvar.Count; i++)
                {
                    //gerar: [Nome_Retorno, Tipo_Retorno]
                    newText += "									<h6 " +
                               "class='card-subtitle mb-2 text-muted'>" +
                               returnvar[i][0] + ", " + returnvar[i][1] + "</h6>" + Environment.NewLine;
                    //gerar: [Nome_Retorno, Tipo_Retorno, Descricao_Retorno]
                    newText += "									<p " +
                               "class='card-text custom-card-text'><i>" +
                               returnvar[i][2] + "</i></p>" + Environment.NewLine;
                }
            }
            else newText += "<p></p>" + Environment.NewLine;

            File.WriteAllText(file, File.ReadAllText(file).Replace("&pDoc:Return&", newText));
        }

        private void pDocToDo(string file, List<string> todolist)
        {
            var newText = "";
            if (todolist.Count > 0)
            {
                for (int i = 0; i < todolist.Count; i++)
                {
                    newText += "								" +
                               todolist[i] + "<br>" + Environment.NewLine;
                }
            }
            else newText += "<br>" + Environment.NewLine;

            File.WriteAllText(file, File.ReadAllText(file).Replace("&pDoc:ToDoList&", newText));
        }

        private void pDocDescription(string file, string description) => File.WriteAllText(file, File.ReadAllText(file).Replace("&pDoc:Description&", description));

        private void Replace_FunctionCallArea(string file, string cfunref) => File.WriteAllText(file, File.ReadAllText(file).Replace("&FunctionCallArea&", cfunref));

        private void pDocWhere(string file, string wherevar)
        {
            var newText = "";
            if (wherevar != "")
            {
                newText = "<span style='font-weight:bold'>EM QUE PONTO:</span> " +
                              wherevar +
                              "</p>" + Environment.NewLine;
            }
            File.WriteAllText(file, File.ReadAllText(file).Replace("&pDoc:Where&", newText));
        }

        private void Create_Tree(string file, int recursivelevel = 0)
        {
            var folder = new DirectoryInfo(file);
            recursivelevel++; // Somente para checar o nível da recursividade da função

            if (folder.GetDirectories("*", SearchOption.TopDirectoryOnly)
                .Any(d => !d.FullName.ToString().Contains("$") && !d.FullName.ToString().Contains(".vscode")))
            {
                foreach (var directory in folder.GetDirectories("*", SearchOption.TopDirectoryOnly)
                    .Where(d => !d.FullName.ToString().Contains("$") && !d.FullName.ToString().Contains(".vscode")))
                {
                    Tree += "{";
                    Tree += "text:'" + directory.Name + "'";
                    var dInfo = new DirectoryInfo(directory.FullName);
                    if (dInfo.GetDirectories("*", SearchOption.TopDirectoryOnly).Count(d => !d.FullName.ToString().Contains("$") && !d.FullName.ToString().Contains(".vscode")) != 0)
                    {
                        Tree += ",nodes:[";
                        Create_Tree(directory.FullName, recursivelevel);

                    }
                    else
                    {
                        Tree += ",nodes:[";
                        foreach (var codefile in dInfo.GetFiles("*.prw", SearchOption.TopDirectoryOnly)
                            .Where(d => !d.FullName.ToString().Contains("$") && !d.FullName.ToString().Contains(".vscode")))
                        {
                            var href = codefile.FullName.Replace(Xml.TfsFolder, "");
                            href = "." + HtmlFormatters.URLReplace(href) + ".html";
                            Tree += "{";
                            Tree += "text:'" + codefile.Name + "',";
                            Tree += "icon:'far fa-file',";
                            Tree += "href:'" + href.Replace(@"\", @"/") + "'},";
                        }
                        Tree += "]},";
                    }
                }
                foreach (var codefile in folder.GetFiles("*.prw", SearchOption.TopDirectoryOnly)
                    .Where(d => !d.FullName.ToString().Contains("$") && !d.FullName.ToString().Contains(".vscode")))
                {
                    var href = codefile.FullName.Replace(Xml.TfsFolder, "");
                    href = "." + HtmlFormatters.URLReplace(href) + ".html";
                    Tree += "{";
                    Tree += "text:'" + codefile.Name + "',";
                    Tree += "icon:'far fa-file',";
                    Tree += "href:'" + href.Replace(@"\", @"/") + "'},";
                }
                Tree += "],";
            }
            Tree += "},";
        }

        private void FeedError(string CSSclass, string message)
        {
            CodeErrorList += "						<div " +
                      "class='alert alert-" + CSSclass + "' role='alert'>" + Environment.NewLine +
                      message + Environment.NewLine +
                      "						</div>" + Environment.NewLine;
        }
    }
}