﻿//MIT License
//
//Copyright(c) 2016 Peter Kirmeier
//
//Permission Is hereby granted, free Of charge, to any person obtaining a copy
//of this software And associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, And/Or sell
//copies of the Software, And to permit persons to whom the Software Is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice And this permission notice shall be included In all
//copies Or substantial portions of the Software.
//
//THE SOFTWARE Is PROVIDED "AS IS", WITHOUT WARRANTY Of ANY KIND, EXPRESS Or
//IMPLIED, INCLUDING BUT Not LIMITED To THE WARRANTIES Of MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE And NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS Or COPYRIGHT HOLDERS BE LIABLE For ANY CLAIM, DAMAGES Or OTHER
//LIABILITY, WHETHER In AN ACTION Of CONTRACT, TORT Or OTHERWISE, ARISING FROM,
//OUT OF Or IN CONNECTION WITH THE SOFTWARE Or THE USE Or OTHER DEALINGS IN THE
//SOFTWARE.

using IFR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using static IFR.IFRHelper;

namespace IFR
{
    /// <summary>
    /// Used as a core element that enables iterating through the HPK file tree
    /// and providing some generic information about the object.
    /// </summary>
    class HPKElement
    {
        /// <summary>
        /// Raw data representation of this object
        /// </summary> 
        protected IfrRawDataBlock data;
        /// <summary>
        /// Managed structure header
        /// </summary>
        public virtual object Header { get { return null; } }
        /// <summary>
        /// Gets name/value pairs of all managed header's data
        /// </summary>
        /// <returns>list of name/value pairs</returns>
        public List<KeyValuePair<string, object>> HeaderToStringList()
        {
            List<KeyValuePair<string, object>> result = new List<KeyValuePair<string, object>>();

            if (Header != null) AddStructToStringList(result, Header);

            return result;
        }
        
        private void AddStructToStringList(List<KeyValuePair<string, object>> list, object obj)
        {
            foreach (System.Reflection.PropertyInfo pi in obj.GetType().GetProperties())
            {
                if (pi.CanRead)
                {
                    if ((pi.PropertyType.IsEnum) || (pi.PropertyType.FullName.StartsWith("System.")))
                        list.Add(new KeyValuePair<string, object>(pi.Name, pi.GetValue(obj)));
                    else
                        AddStructToStringList(list, pi.GetValue(obj));
                }
            }
            foreach (System.Reflection.MemberInfo mi in obj.GetType().GetMembers())
            {
                if (mi is System.Reflection.FieldInfo)
                {
                    System.Reflection.FieldInfo fi = (System.Reflection.FieldInfo)mi;
                    if (fi.IsPublic)
                    {
                        if ((fi.FieldType.IsEnum) || (fi.FieldType.FullName.StartsWith("System.")))
                            list.Add(new KeyValuePair<string, object>(fi.Name, fi.GetValue(obj)));
                        else
                            AddStructToStringList(list, fi.GetValue(obj));
                    }
                }
            }
        }

        private void LoadHpkElementIntoTreeView(HPKElement elem, TreeNode parent)
        {
            // add all header fields to the tree..
            if (elem.Header != null)
            {
                TreeNode leaf = parent.Nodes.Add("Header");
                foreach (System.Reflection.PropertyInfo pi in elem.Header.GetType().GetProperties())
                {
                    if (pi.CanRead)
                    {
                        leaf.Nodes.Add(pi.Name + " = " + pi.GetValue(elem.Header).ToString());
                    }
                }
                foreach (System.Reflection.MemberInfo mi in elem.Header.GetType().GetMembers())
                {
                    if (mi is System.Reflection.FieldInfo)
                    {
                        System.Reflection.FieldInfo fi = (System.Reflection.FieldInfo)mi;
                        if (!fi.FieldType.FullName.StartsWith("System."))
                            leaf.Nodes.Add(mi.Name + " ! " + fi.GetValue(elem.Header).ToString());
                        else
                        if (fi.IsPublic)
                            leaf.Nodes.Add(mi.Name + " = " + fi.GetValue(elem.Header).ToString());
                    }
                }
            }
        }

        /// <summary>
        /// Friendly name of this object
        /// </summary> 
        public virtual string Name { get { return this.ToString(); } }
        /// <summary>
        /// List of all childs
        /// </summary> 
        public readonly List<HPKElement> Childs;

        public HPKElement(IfrRawDataBlock raw)
        {
            data = raw;
            Childs = new List<HPKElement>();
        }
        /// <summary>
        /// An error has occured
        /// </summary>
        /// <param name="severity">Severity of message</param>
        /// <param name="msg">Message string</param>
        protected void PrintConsoleMsg(IfrErrorSeverity severity, string msg)
        {
            PrintLineToLocalConsole(severity, this.ToString(), msg);
        }
    }

    /// <summary>
    /// Represents a whole file including multiple packages
    /// </summary>
    class HPKfile : HPKElement
    {
        /// <summary>
        /// Filename of the parsed file
        /// </summary>
        private readonly string Filename;
        /// <summary>
        /// Friendly name of this object
        /// </summary> 
        public override string Name { get { return Filename; } }

        public HPKfile(string filename) : base(null)
        {
            this.Filename = filename;

            // Load file into memory
            StreamReader stream = new StreamReader(filename);
            BinaryReader file = new BinaryReader(stream.BaseStream);
            data = new IfrRawDataBlock(file.ReadBytes((int)file.BaseStream.Length));
            stream.Close();

            try
            {
                PrintConsoleMsg(IfrErrorSeverity.WARNING, "EFI_GUID not checked to be correctly read from binary!");
                // Parse all HII packages..
                uint offset = 0;
                while (offset < data.Length)
                {
                    EFI_HII_PACKAGE_HEADER hdr = data.ToIfrType<EFI_HII_PACKAGE_HEADER>(offset);
                    if (data.Length < hdr.Length + offset)
                        throw new Exception("Payload length invalid");

                    IfrRawDataBlock raw_data = new IfrRawDataBlock(data.Bytes, data.Offset + offset, hdr.Length);

                    switch (hdr.Type)
                    {
                        case EFI_HII_PACKAGE_e.EFI_HII_PACKAGE_FORMS:
                            EFI_HII_FORM_PACKAGE_HDR pkg_hdr = new EFI_HII_FORM_PACKAGE_HDR();
                            pkg_hdr.Header = hdr;
                            Childs.Add(new HiiPackageForm(pkg_hdr, raw_data));
                            break;
                        default: PrintConsoleMsg(IfrErrorSeverity.UNIMPLEMENTED, hdr.Type.ToString()); break;
                    }

                    offset += hdr.Length;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                MessageBox.Show("Parsing HPK failed!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
