using ServerNode.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerNode.Native.Linux
{
    internal static class Pstree
    {
        public static IEnumerable<int> ProcessTree(int processId)
        {
            string[] pstree = SH.Shell(Program.WorkingDirectory, @"-c ""pstree " + processId + @" -p""", null, null);
            string firstLine = pstree.FirstOrDefault();

            string tmpPID = "";

            foreach (char ch in firstLine)
            {
                if (Utility.StringExtension.IsRealDigitOnly(ch))
                {
                    tmpPID += ch;
                }
                else
                {
                    if (!string.IsNullOrEmpty(tmpPID) && tmpPID != "0" && tmpPID.IsDigitsOnly())
                    {
                        yield return Convert.ToInt32(tmpPID);
                        tmpPID = "";
                    }
                }
            }
        }
    }
}
