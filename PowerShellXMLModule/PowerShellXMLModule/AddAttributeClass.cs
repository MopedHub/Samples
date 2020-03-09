using System;
using System.IO;
using System.Diagnostics;
using System.Xml.Linq;
using System.Management.Automation;
using System.Collections.Generic;
using System.Xml;

namespace System.IO
{
    public class Attribute
    {
        public string Name;
        public string Value;
    }

    public class FileWithAttributes
    {
        public FileInfo file { get; }

        public FileWithAttributes(FileInfo file)
        {
            this.file = file;
        }

        public List<Attribute> Attributes { get; } = new List<Attribute>();

        public byte[] GetBytes()
        {
            return File.ReadAllBytes(file.FullName);
        }

        public void AddAttribute(string name, string value)
        {
            var index = Attributes.FindIndex(w => w.Name == name);
            if (index == -1)
            {
                Attributes.Add(new Attribute() { Name = name, Value = value });
            }
            else
            {
                Attributes[index].Value = value;
            }
        }

        public XmlDocument GetXml()
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(file.FullName);
            return xmlDoc;
        }

        public string Get(string attribute)
        {
            var attr = Attributes.Find(w => w.Name.ToLower() == attribute.ToLower());
            return attr == null ? "" : attr.Value;
        }

        public override string ToString()
        {
            var res = new List<string>();

            foreach (var str in Attributes)
            {
                res.Add($"Name:{str.Name}, Value: {str.Value}");
            }

            return $"{file.Name}:Attributes({String.Join(";", res)})";
        }
    }

}

namespace PowerShellXMLCmdlets
{
    [Cmdlet(VerbsCommon.Add, "Attribute")]
    public class AddAttribute : Cmdlet
    {
     
        [Parameter()]
        public FileInfo[] IOFiles{ get; set; }
        [Parameter()]
        public FileWithAttributes[] AFiles { get; set; }
        

        [ValidateSet("XPATH","CRC16", IgnoreCase = true)]
        [Parameter (HelpMessage = "XPATH or CRC16")]
        public string Command { get; set; }

        private string[] _attributeName;

        [Parameter(
         Mandatory = true, HelpMessage = "Attribute name")]
        public string[] AttributeName
        {
            get { return _attributeName; }
            set { _attributeName = value; }
        }

        public string GetAttributeName(int index)
        {
            if(_attributeName == null || _attributeName.Length <= index)
                return $"Attribute {index}";

            return _attributeName[index];
        }

        [Parameter()]
        public string[] XPATH { get; set; }

        [Parameter(
         HelpMessage = "NameSpace:URI")]
        public string NS { get; set; }

        [Parameter(
         HelpMessage = "InnerXML or InnerText")]
        public SwitchParameter xmlResult { get; set; }

        private FileWithAttributes Processing(object file)
        {
            var data = file as FileWithAttributes;

            if (data == null && file is FileInfo)            
                data = new FileWithAttributes(file as FileInfo);

            if (data != null)
            {
                switch (Command.ToUpper())
                {
                    case "XPATH":
                        var xmlDoc = data.GetXml();

                        var ns = (XmlNamespaceManager)null;

                        if (!String.IsNullOrEmpty(NS))
                        {
                            var arr = NS.Split(':');
                            ns = new XmlNamespaceManager(xmlDoc.NameTable);
                            ns.AddNamespace(arr[0], arr[arr.Length - 1]);
                        }

                        for (var i =0; i < XPATH.Length; i++)
                        {
                            var xpath = XPATH[i];
                            var name = GetAttributeName(i);

                            var nodes = ns == null ?
                                xmlDoc.DocumentElement.SelectNodes(xpath) :
                                xmlDoc.DocumentElement.SelectNodes(xpath, ns);

                            if (nodes == null || nodes.Count == 0)
                                data.AddAttribute(name, "");
                            else
                            {
                                var xml = new List<string>();
                                foreach (XmlNode item in nodes)
                                    xml.Add(xmlResult ? item.InnerXml : item.InnerText);

                                data.AddAttribute(name, string.Join(",", xml.ToArray()) ?? "");
                            }
                        }
                   
                        break;
                    case "CRC16":
                        byte high, low;
                        var fileBody = data.GetBytes();
                        CRC16(fileBody, fileBody.Length, out high, out low);
                        data.AddAttribute("CRC16", $"{high.ToString("x2")}{low.ToString("x2")}");
                        break;
                }
            }

            return data;
        }
        
        private static void CRC16(byte[] message, int length, out byte CRCHigh, out byte CRCLow)
        {
            ushort CRCFull = 0xFFFF;
            for (int i = 0; i < length; i++)
            {
                CRCFull = (ushort)(CRCFull ^ message[i]);
                for (int j = 0; j < 8; j++)
                {
                    if ((CRCFull & 0x0001) == 0)
                        CRCFull = (ushort)(CRCFull >> 1);//(ushort)(CRCFull ^ 0xA001);
                    else
                    {
                        CRCFull = (ushort)((CRCFull >> 1) ^ 0xA001);
                    }
                }
            }
            CRCHigh = (byte)((CRCFull >> 8) & 0xFF);
            CRCLow = (byte)(CRCFull & 0xFF);
        }

        protected override void ProcessRecord()        
        {
            try
            {
                var Files = (object[])IOFiles;
                if (Files == null) Files = (object[])AFiles;

                if (Files == null || Files.Length == 0) throw new Exception ("Empty data");

                var result = new List<FileWithAttributes>();
                
                foreach (var file in Files)
                {
                    var r = Processing(file);
                    if (r != null) result.Add(r);
                }

                WriteObject(result.ToArray(), true);
            }
            catch 
            {
                //WriteObject(Files, true);
                throw;
            }

     
        }
    }
}
