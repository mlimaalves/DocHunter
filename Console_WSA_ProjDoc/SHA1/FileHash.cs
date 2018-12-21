using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Console_WSA_ProjDoc.SHA1
{
    class FileHash
    {
        public bool CompareHash(string htmlfile, string SHA1)
        {
            var regexPattern = "(?<=checksum=').*?(?=')"; // SHA1 content inside the checksum HTML meta tag
            var Text = File.ReadAllText(htmlfile);
            var regex = new Regex(regexPattern, RegexOptions.Singleline);
            var match = regex.Match(Text);
            
            return (match.Success && match.Value == SHA1) ? false : true;
        }
    }
}

