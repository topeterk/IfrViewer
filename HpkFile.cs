//MIT License
//
//Copyright(c) 2016-2017 Peter Kirmeier
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using static IFR.IFRHelper;

namespace IFR
{
    /// <summary>
    /// Used as a core element that enables iterating through the HPK file tree
    /// and providing some generic information about the object.
    /// </summary>
    class HPKElement
    {
        #region UniqueID
        /// <summary>
        /// Holds last unique ID of HPKElement instances 
        /// </summary>
        private static int GlobalLastUniqueID = 0;
        /// <summary>
        /// Unique ID of instance
        /// </summary>
        public readonly int UniqueID;
        #endregion

        #region Raw Data Blocks
        /// <summary>
        /// Raw data representation of this object
        /// </summary> 
        protected IfrRawDataBlock data;
        /// <summary>
        /// Raw data representation of this object's payload
        /// </summary> 
        protected IfrRawDataBlock data_payload;
        /// <summary>
        /// Physical size of header and payload
        /// </summary>
        public uint PhysicalSize { get { return data.Length; } }
        #endregion

        #region Header specific
        /// <summary>
        /// Managed structure payload (raw)
        /// </summary>
        public byte[] HeaderRaw
        {
            get
            {
                byte[] result;
                // There is no payload? Then everything is header..
                if (data_payload == null)
                    result = data.CopyOfSelectedBytes;
                else
                {
                    // Otherwise, the header is from data start till payload begin..
                    IfrRawDataBlock hdr_data = new IfrRawDataBlock(data);
                    hdr_data.Length = data_payload.Offset - data.Offset;
                    result = hdr_data.CopyOfSelectedBytes;
                }
                return result.Length > 0 ? result : null;
            }
        }
 
        /// <summary>
        /// Managed structure header
        /// </summary>
        public virtual object Header { get { return null; } }

        /// <summary>
        /// Gets name/value pairs of all managed header's data
        /// </summary>
        /// <returns>list of name/value pairs</returns>
        public List<KeyValuePair<string, object>> GetPrintableHeader(uint BytesPerLine = 16)
        {
            List<KeyValuePair<string, object>> result = new List<KeyValuePair<string, object>>();

            if (Header != null) GetPrintableStructureElements(result, Header, BytesPerLine);

            return result;
        }
        #endregion

        #region Payload specfic
        /// <summary>
        /// Managed structure payload (raw)
        /// </summary>
        public byte[] PayloadRaw { get { return (data_payload == null ? null : (data_payload.Length == 0 ? null : data_payload.CopyOfSelectedBytes)); } }

        /// <summary>
        /// Managed structure payload
        /// </summary>
        public virtual object Payload { get { return null; } }

        /// <summary>
        /// Gets name/value pairs of all managed payloads's data
        /// </summary>
        /// <returns>list of name/value pairs</returns>
        public List<KeyValuePair<string, object>> GetPrintablePayload(uint BytesPerLine = 16)
        {
            List<KeyValuePair<string, object>> result = new List<KeyValuePair<string, object>>();

            if (Payload != null) GetPrintableStructureElements(result, Payload, BytesPerLine);

            return result;
        }
        #endregion

        #region Private helper methods
        private void GetPrintableStructureElements(List<KeyValuePair<string, object>> list, object obj, uint BytesPerLine)
        {
            Type type = obj.GetType();
            bool IsList = obj is IEnumerable;

            foreach (System.Reflection.PropertyInfo pi in type.GetProperties())
            {
                if (pi.CanRead)
                {
                    if (IsList)
                    {
                        if (type.FullName == "System.Byte[]")
                        {
                            list.Add(new KeyValuePair<string, object>("Raw Bytes", (obj as System.Byte[]).HexDump(BytesPerLine)));
                        }
                        else if (type.FullName == "System.String")
                        {
                            string value = obj as System.String;
                            list.Add(new KeyValuePair<string, object>("String", "\"" + value + "\""));
                        }
                        else
                        {
                            foreach (var obj_elem in obj as IEnumerable)
                                GetPrintableStructureElements(list, obj_elem, BytesPerLine);
                        }
                        break; // skip list internal data (means: all other properties of this type)
                    }
                    else if ((pi.PropertyType.IsEnum) || (pi.PropertyType.FullName.StartsWith("System.")))
                        list.Add(new KeyValuePair<string, object>(pi.Name, pi.GetValue(obj)));
                    else
                        GetPrintableStructureElements(list, pi.GetValue(obj), BytesPerLine);
                }
            }
            foreach (System.Reflection.MemberInfo mi in type.GetMembers())
            {
                if (mi is System.Reflection.FieldInfo)
                {
                    System.Reflection.FieldInfo fi = (System.Reflection.FieldInfo)mi;
                    if (fi.IsPublic)
                    {
                        if (fi.FieldType.FullName == "System.Char[]")
                        {
                            string value = new string(fi.GetValue(obj) as System.Char[]).Replace("\0", "<NUL>");
                            list.Add(new KeyValuePair<string, object>(fi.Name, "\"" + value + "\""));
                        }
                        else if (fi.FieldType.FullName == "System.String")
                        {
                            if (fi.Name != "Empty") // filter Empty member of basic string class
                            {
                                string value = fi.GetValue(obj) as System.String;
                                list.Add(new KeyValuePair<string, object>(fi.Name, "\"" + value + "\""));
                            }
                        }
                        else if ((fi.FieldType.IsEnum) || (fi.FieldType.FullName.StartsWith("System.")))
                            list.Add(new KeyValuePair<string, object>(fi.Name, fi.GetValue(obj)));
                        else
                            GetPrintableStructureElements(list, fi.GetValue(obj), BytesPerLine);
                    }
                }
            }
        }
        #endregion

        #region Name, Childs, Debug and Constructor
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
            UniqueID = GlobalLastUniqueID++;
            data = raw;
            Childs = new List<HPKElement>();
        }
        /// <summary>
        /// Generates a logged message
        /// </summary>
        /// <param name="severity">Severity of message</param>
        /// <param name="msg">Message string</param>
        protected void LogMessage(LogSeverity severity, string msg)
        {
            CreateLogEntry(severity, this.ToString() + " [" + this.UniqueID + "]", msg);
        }
        #endregion
    }

    /// <summary>
    /// Represents a whole file including multiple packages
    /// </summary>
    class HPKfile : HPKElement
    {
        /// <summary>
        /// Filename of the parsed file
        /// </summary>
        public readonly string Filename;
        /// <summary>
        /// Friendly name of this object
        /// </summary> 
        public override string Name { get { return Filename; } }

        public HPKfile(string filename) : base(null)
        {
            this.Filename = filename;

            // Load file into memory
            StreamReader stream = new StreamReader(Filename);
            BinaryReader file = new BinaryReader(stream.BaseStream);
            data = new IfrRawDataBlock(file.ReadBytes((int)file.BaseStream.Length));
            data_payload = new IfrRawDataBlock(data);
            stream.Close();

            // Parse all HII packages..
            uint offset = 0;
            while (offset < data_payload.Length)
            {
                EFI_HII_PACKAGE_HEADER hdr = data_payload.ToIfrType<EFI_HII_PACKAGE_HEADER>(offset);
                if (data_payload.Length < hdr.Length + offset)
                    throw new Exception("Payload length invalid");

                IfrRawDataBlock raw_data = new IfrRawDataBlock(data_payload.Bytes, data_payload.Offset + offset, hdr.Length);
                HPKElement hpk_element;

                switch (hdr.Type)
                {
                    case EFI_HII_PACKAGE_e.EFI_HII_PACKAGE_FORMS: hpk_element = new HiiPackageForm(raw_data); break;
                    case EFI_HII_PACKAGE_e.EFI_HII_PACKAGE_STRINGS: hpk_element = new HiiPackageString(raw_data); break;
                    default:
                        //raw_data.GenerateAndLogDump(hdr.Type.ToString());
                        LogMessage(LogSeverity.UNIMPLEMENTED, hdr.Type.ToString());
                        hpk_element = new HiiPackage<EFI_HII_PACKAGE_HEADER>(raw_data);
                        break;
                }
                Childs.Add(hpk_element);

                offset += hdr.Length;
            }
        }
    }
}
