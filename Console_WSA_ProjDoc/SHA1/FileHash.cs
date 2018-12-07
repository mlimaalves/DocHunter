using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Console_WSA_ProjDoc.SHA1
{
    class FileHash
    {
        public bool CompareHash(string originalfilename, string NewSHA1, string extension)
        {
            bool Modified = true;
            string csfile = originalfilename.ToLower().Replace(extension, "") + ".#tfvc";
            if (File.Exists(csfile)) // Se o existir, grava chave SHA1 concatenada
            {
                var fileContent = File.ReadLines(csfile).ToList();
                if (fileContent[0].Contains("@SHA1: ") && fileContent[0].Replace("@SHA1: ", "") == NewSHA1) Modified = false; // Não houveram modificações
                else // Houveram modificações. Atualiza arquivo #tfvc com o novo hash
                {
                    fileContent[0] = "@SHA1: " + NewSHA1; // Atualiza a chave SHA1 do arquivo
                    File.WriteAllLines(csfile, fileContent);
                }
            }
            return Modified;
        }
    }
}

