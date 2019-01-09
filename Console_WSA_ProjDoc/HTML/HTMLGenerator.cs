using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using RegexDocs.CSS;
using RegexDocs.General;
using RegexDocs.XML;
using RegexDocs.SHA1;
using RegexDocs.SQLite;
using System.Data;

namespace RegexDocs.HTML
{
    public class HtmlGenerator
    {
        private static readonly Logging Logging = new Logging();
        private static FileHash FileHash = new FileHash();
        private XmlConfigs.Xml Xml { get; set; } = new XmlConfigs.Xml();
        private static readonly string Assemblyfolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\";
        private string Tree = "";
        private string CodeErrorList = "";
        public void LoadXml(XmlConfigs.Xml xml) => this.Xml = xml;

        public bool Start()
        {
            var lret = false;
            try
            {
                Logging.WriteLog("Starting the HTML files creation...");
                CreateDocumentation();
                Logging.WriteLog("HTML Files Creation Status: CONCLUDED.");
                lret = true;
            }
            catch (IOException e)
            {
                Logging.WriteLog("An I/O Exception occurred during the HTML files creation: \n" + e);
            }
            catch (Exception e)
            {
                Logging.WriteLog("An Exception occurred during the HTML files creation: \n" + e);
            }

            if (!lret) Environment.Exit(0);
            return lret;
        }

        private bool CreateDocumentation()
        {
            const bool lRet = false;
            var htmlFolder = HtmlFormatters.URLReplace(Xml.HtmlFolder + Xml.ProjectTitle);
            var htmlfile = "";
            var dir = new DirectoryInfo(htmlFolder);
            try
            {
                if (Xml.DeleteFiles == "true" && Directory.Exists(htmlFolder)) dir.Delete(true);
            }
            catch (IOException e)
            {
                Logging.WriteLog("ATTENTION: IT WAS NOT POSSIBLE TO EXCLUDE THE DIRECTORY: " + dir.FullName + ". THE PROCESS WILL CONTINUE.\n" + e);
            }
            Directory.CreateDirectory(htmlFolder);

            #region Copiando arquivos css originais do bináiro para o diretório HTML:
            var css = new CssGenerator();
            css.LoadXml(Xml);
            css.CopyCss(htmlFolder);
            #endregion

            #region CRIANDO ÁRVORE DE PASTAS E ROTINAS PARA A NAVEGAÇÃO PRINCIPAL
            Create_Tree(Xml.LocalFolder);
            // Ajuste final na árvore:
            Tree = Tree.Replace(@"},]", @"}]").Replace(@"],}", @"]}");
            Tree = Tree.Substring(0, Tree.Length - 2);
            #endregion 

            #region CRIANDO ARQUIVO NAVIGATION PRINCIPAL (MAIN.HTML)
            //Iniciando o project com o arquivo Main.Html
            htmlfile = htmlFolder + @"\main.html";
            CreateHTMLFile(htmlfile, "Navigation.html");
            Replace_TreeArea(htmlfile);
            Replace_NameArea(htmlfile, Xml.ProjectTitle);
            Replace_ButtonArea(Xml.LocalFolder, htmlfile);
            Replace_FileProtocolArea(htmlfile);
            Replace_SearchArea(htmlfile);
            Replace_BreadcrumbArea(htmlfile, htmlfile);
            Replace_Dictionary(htmlfile);

            Logging.WriteLog("main.html file created successfully. Generating the " + Xml.Extension + " documentation.");
            #endregion

            #region CRIANDO ARQUIVOS CODE
            var tfsFolder = new DirectoryInfo(Xml.LocalFolder);
            var counter = tfsFolder.GetFiles("*" + Xml.Extension, SearchOption.AllDirectories).Length;
            var current = 0;

            foreach (var codefile in tfsFolder.GetFiles("*" + Xml.Extension, SearchOption.AllDirectories)
                .Where(d => !d.FullName.ToString().Contains(".vscode")))
            {
                current++;
                Logging.WriteLog(current + " of " + counter + " files processed.", true);
                var originalfilename = codefile.FullName;
                var newdirectory = WebUtility.HtmlEncode(codefile.Directory.FullName);
                newdirectory = htmlFolder + @"\" + HtmlFormatters.URLReplace(newdirectory.Replace(Xml.LocalFolder, ""));
                Directory.CreateDirectory(newdirectory);

                htmlfile = HtmlFormatters.URLReplace(codefile.Name) + ".html";
                htmlfile = newdirectory + @"\" + htmlfile;
                byte[] result;

                System.Security.Cryptography.SHA1 sha = new System.Security.Cryptography.SHA1CryptoServiceProvider();
                // This is one implementation of the abstract class SHA1.
                FileStream fs = File.OpenRead(originalfilename);
                result = sha.ComputeHash(fs);
                var SHA1 = "";
                foreach (var line in result) SHA1 += line.ToString();
                fs.Close();
                if (!File.Exists(htmlfile) || FileHash.CompareHash(htmlfile, SHA1))
                {
                    CreateHTMLFile(htmlfile, "Code.html", SHA1);
                    Replace_NameArea(htmlfile, Xml.ProjectTitle);
                    Replace_FileNameArea(htmlfile, codefile.Name);
                    Replace_FileProtocolArea(htmlfile);
                    Replace_SearchArea(htmlfile);
                    Replace_BreadcrumbArea(htmlfile, originalfilename);
                    RegExDocumentation(htmlfile, codefile.FullName);
                    RegExCode(htmlfile, codefile.FullName);
                    Replace_History(htmlfile, codefile.FullName);
                    Replace_Dictionary(htmlfile);
                }
            }
            #endregion

            return lRet;
        }

        private void CreateHTMLFile(string htmldocfile, string filetype, string checksum = "")
        {
            var navFolder = Assemblyfolder + @"HTML\";
            var rawhtmlfile = navFolder + filetype;
            if (File.Exists(htmldocfile)) File.Delete(htmldocfile);
            File.Copy(rawhtmlfile, htmldocfile, true);
            File.WriteAllText(htmldocfile, File.ReadAllText(htmldocfile).Replace("checksum=''", "checksum='" + checksum + "'"));
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
                .Where(d => !d.FullName.ToString().Contains(".vscode")))
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
                foreach (var subfiles in innerFolder.GetFiles("*" + Xml.Extension, SearchOption.TopDirectoryOnly))
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

            foreach (var codefile in currentFolder.GetFiles("*" + Xml.Extension, SearchOption.TopDirectoryOnly)
                .Where(d => !d.FullName.ToString().Contains(".vscode")))
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
            var replace = (Xml.LocalFolder).ToLower();
            var breadcrumbfolders = @"\" + f.FullName.Replace(replace, "").Replace(@"\main.html", "");
            var folderSplit = f.Name == "main.html" ? new String[1] : breadcrumbfolders.Split(Path.DirectorySeparatorChar);
            var breadcrumbText = "";
            int x;
            for (x = 0; x < folderSplit.Length; x++)
            {
                if (folderSplit[x] == null) folderSplit[x] = "";
                var currentpath = HtmlFormatters.URLReplace(Xml.LocalFolder);
                int y;
                for (y = 0; y < x + 1; y++)
                {
                    currentpath += folderSplit[y] + @"\";
                }

                if (f.Name != "main.html" && x == 0)
                {
                    var goback = string.Concat(Enumerable.Repeat("../", folderSplit.Length - 2));
                    var href = currentpath.Replace(HtmlFormatters.URLReplace(Xml.LocalFolder), "") +
                               ((folderSplit[x] == "") ? "main" : folderSplit[x]);
                    href = goback + HtmlFormatters.URLReplace(href) + ".html";
                    breadcrumbText += "							<li " +
                                      "class='breadcrumb-item'>" +
                                      "<a href='" + href + "'>" +
                                      ((folderSplit[x] == "") ? Xml.ProjectTitle : folderSplit[x].First().ToString().ToUpper() + folderSplit[x].Substring(1).ToLower()) +
                                      "</a></li>" +
                                      Environment.NewLine;
                }
                else
                {
                    breadcrumbText += "							<li " +
                                      "class='breadcrumb-item active' " +
                                      "aria-current='page'>" +
                                      ((folderSplit[x] == "") ? Xml.ProjectTitle : folderSplit[x].First().ToString().ToUpper() + folderSplit[x].Substring(1).ToLower()) +
                                      "</li>" +
                                      Environment.NewLine;
                }
            }
            File.WriteAllText(file, File.ReadAllText(file).Replace("&breadcrumbArea&", breadcrumbText));
        }

        private void Replace_History(string file, string originalfilename)
        {
            var f = new FileInfo(originalfilename);
            var currcs = "";
            var csnav = "";
            DateTime currdatetime = DateTime.Now;

            if (Xml.TfsHistory == "true")
            {
                csnav += "						<li class='nav-item'>" + Environment.NewLine;
                csnav += "							<a class='nav-link' id='history-tab' data-toggle='tab' href='#history' role='tab' aria-controls='history' aria-selected='false'>&Dic:tab_history&</a>" + Environment.NewLine;
                csnav += "						</li>";

                var dbsource = Xml.LocalFolder + "db.sqlite";
                var SQLiteDb = new Datastore(dbsource);
                var dt = SQLiteDb.QueryRecord(originalfilename.ToLower().Replace("/", "\\"));

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        var Datetime = row["creationdate"];

                        currcs += "								<li class='list-group-item'>" + Environment.NewLine; // Creates a new history group
                        currcs += "									<div class='text-muted font-weight-bold'>" +
                           currdatetime.ToString("dddd, dd/MMM/yyyy", new System.Globalization.CultureInfo(Xml.Language)) + ", " + currdatetime.ToString("hh:mm:ss") + "</div>" + Environment.NewLine;
                        currcs += "<p></p>" + Environment.NewLine;
                        currcs += "									<div class='custom-history'>" + Environment.NewLine;
                        currcs += "										<h6 " +
                            "class='custom-history-title' " +
                            "data-toggle='tooltip' " +
                            "data-placement='bottom' title='&newComment&'>" + Environment.NewLine +
                            "&newComment&" + Environment.NewLine;

                        currcs = currcs.Replace("&newComment&", row["comment"].ToString());
                        currcs += "										<div class='text-muted font-weight-normal'>" +
                           "Changeset " + row["id"].ToString() + ", " + row["creator"].ToString() + "</div>" + Environment.NewLine;
                        currcs += "										</h6>" + Environment.NewLine;
                        currcs += "									</div>" + Environment.NewLine; // History group end
                        currcs += "								</li>";
                    }
                }
            }

            File.WriteAllText(file, File.ReadAllText(file).Replace("&HistoryTab&", csnav));
            File.WriteAllText(file, File.ReadAllText(file).Replace("&HistoryArea&", currcs));
        }

        private void Replace_SearchArea(string file)
        {
            var tfsFolder = new DirectoryInfo(Xml.LocalFolder);
            var searchList = "";
            var f = new FileInfo(file);
            foreach (var codefile in tfsFolder.GetFiles("*" + Xml.Extension, SearchOption.AllDirectories).Where(d => !d.FullName.ToString().Contains(".vscode")))
            {
                var searchText = WebUtility.HtmlEncode(Xml.ProjectTitle + @"\" + codefile.FullName.Replace(Xml.LocalFolder, ""));
                var replace = HtmlFormatters.URLReplace(Xml.HtmlFolder + Xml.ProjectTitle);
                string[] split = f.Directory.FullName.Replace(replace, "").Split(Path.DirectorySeparatorChar);
                var goback = string.Concat(Enumerable.Repeat("../", split.Length - 1));
                var href = codefile.FullName.Replace(Xml.LocalFolder, "");
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
            var replace = HtmlFormatters.URLReplace(Xml.HtmlFolder + Xml.ProjectTitle);
            string[] split = f.Directory.FullName.Replace(replace, "").Split(Path.DirectorySeparatorChar);
            var goback = string.Concat(Enumerable.Repeat("../", split.Length - 1));
            File.WriteAllText(file, File.ReadAllText(file).Replace("&fileProtocolArea&", goback));
        }

        private void Replace_Dictionary(string file)
        {
            foreach (string[] line in Xml.DictionaryList)
            {
                File.WriteAllText(file, File.ReadAllText(file).Replace("&Dic:" + line[0] + "&", line[1]));
            }
        }

        private void Replace_TreeArea(string file) => File.WriteAllText(file, File.ReadAllText(file).Replace("&treeArea&", Tree));

        private void Replace_ErrorArea(string file) => File.WriteAllText(file, File.ReadAllText(file).Replace("&ErrorArea&", CodeErrorList));

        private void RegExCode(string htmlfile, string codefile)
        {
            var fullcode = HtmlFormatters.StringReplace(File.ReadAllText(codefile, Encoding.GetEncoding("Windows-1252")));
            var newCode = "";

            var lines = fullcode.Split(new string[] { "\n" },
                StringSplitOptions.None);
            var linecounter = 0;
            foreach (var line in lines)
            {
                newCode += "<tr><td class='codebox linenumber' id='" + (linecounter + 1) + "'>" + (linecounter + 1) +
                          "</td><td><pre>" + lines[linecounter] + "</pre></td></tr>";
                linecounter++;
            }
            File.WriteAllText(htmlfile, File.ReadAllText(htmlfile).Replace("&CodeBoxArea&", newCode));
        }

        private void RegExDocumentation(string htmlfile, string codefile)
        {
            //
            string CommentBlock = "";

            List<string> exampleslist = new List<string>();
            List<string> obslist = new List<string>();
            List<string> todolist = new List<string>();
            List<string[]> param = new List<string[]>();
            List<string[]> returnvar = new List<string[]>();
            //
            var search = "";
            var fullcode = HtmlFormatters.StringReplace(File.ReadAllText(codefile, Encoding.GetEncoding("Windows-1252")));
            var regexPattern = "";

            CodeErrorList = "";

            #region COMMENT BLOCK [commentblock] SECTION
            regexPattern = Xml.Regex("commentblock");
            Regex regex = new Regex(regexPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
            search = fullcode;
            Match match = regex.Match(search);

            if (match.Success)
            {
                CommentBlock = match.Value;

                #region COMMENT BLOCK - GETS THE [examples] ARRAY
                regexPattern = Xml.Regex("examples");
                regex = new Regex(regexPattern, RegexOptions.IgnoreCase);
                search = CommentBlock;
                foreach (Match m in regex.Matches(search))
                {
                    var currexample = RemoveRegexItemText(Xml.Regex("examples"), m.Value);
                    exampleslist.Add(currexample);
                }
                #endregion

                #region COMMENT BLOCK - GETS THE [obs] ARRAY
                regexPattern = Xml.Regex("observations");
                regex = new Regex(regexPattern, RegexOptions.IgnoreCase);
                search = CommentBlock;
                foreach (Match m in regex.Matches(search))
                {
                    var currobs = RemoveRegexItemText(Xml.Regex("observations"), m.Value);
                    obslist.Add(currobs);
                }
                #endregion

                #region COMMENT BLOCK - GETS THE [param] ARRAY
                regexPattern = Xml.Regex("param");
                regex = new Regex(regexPattern, RegexOptions.IgnoreCase);

                if (regex.Matches(CommentBlock).Count > 0)
                {
                    foreach (Match m in regex.Matches(search))
                    {
                        var currparam = RemoveRegexItemText(Xml.Regex("param"), m.Value);
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
                else // No [param] was identified in the CommentBlock
                {
                    var errormsg = "";
                    errormsg = "&Dic:err_paramnotfound&";

                    FeedError("danger", errormsg);
                }

                #endregion

                #region COMMENT BLOCK - GETS THE [todo] ARRAY
                regexPattern = Xml.Regex("todo");
                regex = new Regex(regexPattern, RegexOptions.IgnoreCase);
                search = CommentBlock;
                foreach (Match m in regex.Matches(search))
                {
                    var currtodo = RemoveRegexItemText(Xml.Regex("todo"), m.Value); // removendo texto @Todo e espaços
                    todolist.Add((todolist.Count + 1) + ") " + currtodo);
                }
                #endregion

                #region COMMENT BLOCK - GETS THE [return]
                regexPattern = Xml.Regex("return");
                regex = new Regex(regexPattern, RegexOptions.IgnoreCase);
                search = CommentBlock;
                if (regex.Matches(search).Count > 0)
                {
                    foreach (Match m in regex.Matches(search))
                    {
                        var currreturn = RemoveRegexItemText(Xml.Regex("return"), m.Value); // removendo texto @return e espaços
                        var currregex = new Regex(@"[^,]+.*?(?=,)", RegexOptions.IgnoreCase);
                        var matchcollection = currregex.Matches(currreturn);
                        if (matchcollection.Count > 0)
                        {
                            var str = new string[3];
                            currreturn = currreturn.Replace(matchcollection[0].Value, "");
                            currreturn = (matchcollection.Count > 2) ? currreturn.Replace(matchcollection[1].Value, "") : currreturn.Replace(",", "");
                            str[0] = matchcollection[0].Value;
                            str[1] = (matchcollection.Count > 2) ? matchcollection[1].Value : "";
                            str[2] = currreturn; // Descrição = resto do @return que não foi validado pelo RegEx
                            returnvar.Add(str);
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
                errormsg = "&Dic:err_commentblock&";
                FeedError("danger", errormsg);
            }

            // Após analisar todas as variáveis, substitui o arquivo HTML:
            SetExamples(htmlfile, exampleslist);
            SetObservations(htmlfile, obslist);
            SetParam(htmlfile, param);
            SetReturn(htmlfile, returnvar);
            SetToDo(htmlfile, todolist);
            #endregion

            Replace_ErrorArea(htmlfile);
        }

        private string RemoveRegexItemText(string pattern, string Text)
        {
            var regexPattern = pattern + @"?(?=[\[|@|!|#|\]|A-Za-z|1-9])";
            var regex = new Regex(regexPattern, RegexOptions.Singleline);
            var match = regex.Match(Text);
            if (match.Success) return Text.Replace(match.Value, "");
            else return Text;
        }

        private void SetParam(string file, List<string[]> param)
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

            File.WriteAllText(file, File.ReadAllText(file).Replace("&Doc:Param&", newText));
        }

        private void SetReturn(string file, List<string[]> returnvar)
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

            File.WriteAllText(file, File.ReadAllText(file).Replace("&Doc:Return&", newText));
        }

        private void SetToDo(string file, List<string> todolist)
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

            File.WriteAllText(file, File.ReadAllText(file).Replace("&Doc:ToDoList&", newText));
        }

        private void SetExamples(string file, List<string> exampleslist)
        {

            var newText = "";
            if (exampleslist.Count > 0)
            {
                for (int i = 0; i < exampleslist.Count; i++)
                {
                    newText += "								" +
                               exampleslist[i] + "<br>" + Environment.NewLine;
                }
            }
            else newText += "<br>" + Environment.NewLine;

            File.WriteAllText(file, File.ReadAllText(file).Replace("&Doc:Examples&", newText));
        }

        private void SetObservations(string file, List<string> obslist)
        {

            var newText = "";
            if (obslist.Count > 0)
            {
                for (int i = 0; i < obslist.Count; i++)
                {
                    newText += "								" +
                               obslist[i] + "<br>" + Environment.NewLine;
                }
            }
            else newText += "<br>" + Environment.NewLine;

            File.WriteAllText(file, File.ReadAllText(file).Replace("&Doc:Observations&", newText));
        }

        private void Create_Tree(string file, int recursivelevel = 0)
        {
            var folder = new DirectoryInfo(file);
            recursivelevel++; // Somente para checar o nível da recursividade da função

            if (folder.GetDirectories("*", SearchOption.TopDirectoryOnly)
                .Any(d => !d.FullName.ToString().Contains(".vscode")))
            {
                foreach (var directory in folder.GetDirectories("*", SearchOption.TopDirectoryOnly)
                    .Where(d => !d.FullName.ToString().Contains(".vscode")))
                {
                    Tree += "{";
                    Tree += "text:'" + directory.Name + "'";
                    var dInfo = new DirectoryInfo(directory.FullName);
                    if (dInfo.GetDirectories("*", SearchOption.TopDirectoryOnly).Count(d => !d.FullName.ToString().Contains(".vscode")) != 0)
                    {
                        Tree += ",nodes:[";
                        Create_Tree(directory.FullName, recursivelevel);

                    }
                    else
                    {
                        Tree += ",nodes:[";
                        foreach (var codefile in dInfo.GetFiles("*" + Xml.Extension, SearchOption.TopDirectoryOnly)
                            .Where(d => !d.FullName.ToString().Contains(".vscode")))
                        {
                            var href = codefile.FullName.Replace(Xml.LocalFolder, "");
                            href = @".\" + HtmlFormatters.URLReplace(href) + ".html";
                            Tree += "{";
                            Tree += "text:'" + codefile.Name + "',";
                            Tree += "icon:'far fa-file',";
                            Tree += "href:'" + href.Replace(@"\", @"/") + "'},";
                        }
                        Tree += "]},";
                    }
                }
                foreach (var codefile in folder.GetFiles("*" + Xml.Extension, SearchOption.TopDirectoryOnly)
                    .Where(d => !d.FullName.ToString().Contains(".vscode")))
                {
                    var href = codefile.FullName.Replace(Xml.LocalFolder, "");
                    href = @".\" + HtmlFormatters.URLReplace(href) + ".html";
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