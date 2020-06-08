using System;
using System.Collections.Generic;
using System.Text;

namespace ServerNode.Models.Servers
{
    public class Variable
    {
        public string Name { get; }

        public string Value { get; set; }

        public bool ForCommandline { get; }

        public Variable(string name)
        {
            Name = name;
        }

        public Variable(string name, object val, bool forCommandline)
        {
            Name = name;
            Value = val.ToString();
            ForCommandline = forCommandline;
        }
    }
}
