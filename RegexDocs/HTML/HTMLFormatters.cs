using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RegexDocs.HTML
{
    static class HtmlFormatters
    {
        public static string URLReplace(string s)
        {
            string sret = s;
            sret = sret.ToLower().Replace(" ", "_").Replace(",", "_").Replace(".", "_").Replace("/", "").Replace("@", "_").Replace("'", "_");
            return sret;
        }
        public static string StringReplace(string s)
        {
            string sret = s;
            string[][] table =
            {
                new string[] { "&", "&amp;"},
                new string[] { "<", "&lt;"},
                new string[] { ">", "&gt;"},
                new string[] { "\"", "&quot;"}
                
            };

            foreach(string[] line in table)
            {
                sret = sret.Replace(line[0], line[1]);
            }
            return sret;
        }
    }
}
