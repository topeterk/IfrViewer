﻿//MIT License
//
//Copyright(c) 2017-2017 Peter Kirmeier
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
using System.Text;
using System.Windows.Forms;
using System.Xml;
using static IFR.IFRHelper;
using static IfrViewer.HtmlBuilderHelper;

// TODO! https://www.quackit.com/html_5/tags/

// TODO! http://validator.w3.org/

// TODO! Removed shared code with ParseLogical.cs

namespace IfrViewer
{
    public static class HtmlBuilderHelper
    {
        /// <summary>
        /// Creates a log entry for the "Builder" module
        /// </summary>
        /// <param name="severity">Severity of message</param>
        /// <param name="msg">Message string</param>
        public static void CreateLogEntryBuilder(LogSeverity severity, string msg)
        {
            CreateLogEntry(severity, "Builder", msg);
        }
#if false
        /// <summary>
        /// Performs ToString() on the object with a fixed amount of characters
        /// </summary>
        /// <param name="Obj">Obj to be converted</param>
        /// <param name="TotalWith">Amount of characters of resulting string</param>
        /// <returns>String with fixed size</returns>
        public static string ToDecimalString(this object Obj, int TotalWith = 0)
        {
            return Obj.ToString().PadLeft(TotalWith);
        }
#endif
        /// <summary>
        /// Sets an attribute of an XML node to a specific value
        /// </summary>
        /// <param name="Node">Node whose attribute should be set</param>
        /// <param name="Document">XmlDocument used for attribute generation</param>
        /// <param name="Name">Name of the attribute</param>
        /// <param name="Value">Value of the attribute</param>
        /// <returns>Modified node</returns>
        public static XmlNode SetAttribute(this XmlNode Node, XmlDocument Document, string Name, object Value)
        {
            XmlAttribute attr = Document.CreateAttribute(Name);
            attr.Value = Value.ToString();
            Node.Attributes.Append(attr);
            return Node;
        }

        /// <summary>
        /// Appends a text node to an XML node
        /// </summary>
        /// <param name="Parent">Node is created as child of this element</param>
        /// <param name="Document">XmlDocument used for item generation</param>
        /// <param name="Text">Value of the attribute</param>
        /// <returns>Generated node</returns>
        public static XmlNode AddTextNode(this XmlNode Parent, XmlDocument Document, string Text)
        {
            // TODO! Open ticket
            // To see actual data (doubled, leading, trailing spaces), we need to convert the spaces
            // Unfortunately this makes the HTML document "non-searchable" by usual editors, so we dont do it by now
            // Maybe a kind of option is added to the application in order to select type of output (or produce both)
            // Text = Text.Replace(" ", "\u00A0"); // replace "normal space" with "non breaking space"
            XmlNode node = Document.CreateTextNode(Text);
            Parent.AppendChild(node);
            return node;
        }

        /// <summary>
        /// Appends an element node to an XML node
        /// </summary>
        /// <param name="Parent">Node is created as child of this element</param>
        /// <param name="Document">XmlDocument used for item generation</param>
        /// <param name="Name">Name of the element</param>
        /// <returns>Generated node</returns>
        public static XmlNode AddElementNode(this XmlNode Parent, XmlDocument Document, string Name)
        {
            XmlNode node = Document.CreateElement(Name);
            Parent.AppendChild(node);
            return node;
        }

        /// <summary>
        /// Appends an details node to an XML node
        /// </summary>
        /// <param name="Parent">Node is created as child of this element</param>
        /// <param name="Document">XmlDocument used for item generation</param>
        /// <param name="Summary">Summery of the details</param>
        /// <returns>Generated node</returns>
        public static XmlNode AddDetailsNode(this XmlNode Parent, XmlDocument Document, string Summary)
        {
            XmlNode node = Parent.AddElementNode(Document, "details");
            node.AddElementNode(Document, "summary").AddTextNode(Document, Summary);
            return node.AddElementNode(Document, "pre");
        }

        /// <summary>
        /// Appends an conditional node to an XML node
        /// </summary>
        /// <param name="Parent">Node is created as child of this element</param>
        /// <param name="Document">XmlDocument used for item generation</param>
        /// <param name="ConditionType">Main name of the top condition term</param>
        /// <param name="ConditionString">Detailed condition string</param>
        /// <param name="IsOpen">Node should be opened or closed</param>
        /// <returns>Generated node</returns>
        public static XmlNode AddConditionalNode(this XmlNode Parent, XmlDocument Document, string ConditionType, string ConditionString, Boolean IsOpen = true)
        {
            XmlNode node = Parent.AddElementNode(Document, "details");
            if (IsOpen) node.SetAttribute(Document, "open", "open");
            node.AddElementNode(Document, "summary").AddTextNode(Document, ConditionType);
            node.AddElementNode(Document, "span").SetAttribute(Document, "class", "note").AddTextNode(Document, ConditionString);
            node.AddElementNode(Document, "br");
            return node;
        }
    }

    /// <summary>
    /// Parses HPK packages and gives access to a parsed HTML tree and a string database
    /// </summary>
    public class HtmlBuilder
    {
        /// <summary>
        /// Language which is used for parsing
        /// </summary>
        private readonly string Language;

        /// <summary>
        /// Holds the StringID - String pairs for a language
        /// </summary>
        public struct StringDataBase
        {
            public string Language;
            public List<KeyValuePair<UInt16, string>> Strings;
        }
#if true // TODO!
        /// <summary>
        /// Parsed HPK tree
        /// </summary>
        public struct ParsedHpkNode
        {
            public readonly HPKElement Origin;
            public string Name;
            public List<ParsedHpkNode> Childs;

            public ParsedHpkNode(HPKElement Origin, string Name)
            {
                this.Origin = Origin;
                this.Name = Name;
                Childs = new List<ParsedHpkNode>();
            }
        };
#endif
        /// <summary>
        /// Parsed HPK tree
        /// </summary>
        public readonly List<ParsedHpkNode> HpkPackages;
        /// <summary>
        /// String database (no data outside of object construction!)
        /// </summary>
        public readonly List<StringDataBase> StringDB;

        /// <summary>
        /// Parses a set of packages
        /// </summary>
        /// <param name="Packages">List of packages that will be parsed</param>
        /// <param name="Language">Language which is used for parsing</param>
        public HtmlBuilder(List<HiiPackageBase> Packages, string Language)
        {
            this.Language = Language;
            StringDB = new List<StringDataBase>();
            HpkPackages = new List<ParsedHpkNode>();

            // Parsing strings first..
            foreach (HiiPackageBase pkg in Packages)
            {
                if (pkg.PackageType == EFI_HII_PACKAGE_e.EFI_HII_PACKAGE_STRINGS)
                {
                    try
                    {
                        ParsedHpkNode root = new ParsedHpkNode(pkg, pkg.Name);
                        UInt16 StringID = 0; // First ID is 1 (will be increased later)
                        StringDataBase db;
                        db.Language = ((HiiPackageString.Payload_t)pkg.Payload).Language;
                        db.Strings = new List<KeyValuePair<UInt16, string>>();
                        StringDB.Add(db);

                        root.Name = "Strings Package \"" + db.Language + "\"";
                        HpkPackages.Add(root);
                        foreach (HPKElement child in root.Origin.Childs)
                            ParsePackageSibt(child, root, ref StringID, db);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        CreateLogEntryBuilder(LogSeverity.ERROR, "Parsing failed!" + Environment.NewLine + ex.ToString());
                        MessageBox.Show("Parsing failed!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }

            foreach (HiiPackageBase pkg in Packages)
            {
                try
                {
                    ParsedHpkNode root = new ParsedHpkNode(pkg, pkg.Name);
                    switch (pkg.PackageType)
                    {
                        case EFI_HII_PACKAGE_e.EFI_HII_PACKAGE_FORMS:
                            foreach (HPKElement child in root.Origin.Childs)
                                ParsePackageIfr(child, null, null); // doc and node will be created by each formset within this package
                            break;
                        case EFI_HII_PACKAGE_e.EFI_HII_PACKAGE_STRINGS: break; // Already done
                        default:
                            CreateLogEntryBuilder(LogSeverity.UNIMPLEMENTED, root.Name);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    CreateLogEntryBuilder(LogSeverity.ERROR, "Parsing failed!" + Environment.NewLine + ex.ToString());
                    MessageBox.Show("Parsing failed!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        #region Parser - Internal Forms Representation
        /// <summary>
        /// Parses IFR elements and add them to the XML document
        /// </summary>
        /// <param name="hpkelem">HPK element holding the input data</param>
        /// <param name="Document">XmlDocument used for item generation</param>
        /// <param name="root">XML node which new nodes will be added to</param>
        /// <param name="CurrFormId">Current FormId (0 if not set)</param>
        /// <param name="CurrentQuestion">Current Question (null if not set)</param>
        private void ParsePackageIfr(HPKElement hpkelem, XmlDocument doc, XmlNode root, UInt16 CurrFormId = 0, XmlNode CurrentQuestion = null)
        {
            ParsedHpkNode branch = new ParsedHpkNode(hpkelem, hpkelem.Name); // TODO! Must be deleted!
            HiiIfrOpCode elem = (HiiIfrOpCode)hpkelem;
            bool bProcessChilds = true;

            if ((EFI_IFR_OPCODE_e.EFI_IFR_FORM_SET_OP != elem.OpCode) && (null == doc)) // all elements must be within a formset
            {
                CreateLogEntryBuilder(LogSeverity.WARNING, "Not within formset [" + hpkelem.UniqueID + "]!");
                return;
            }

            switch (elem.OpCode)
            {
#region Forms
                case EFI_IFR_OPCODE_e.EFI_IFR_FORM_SET_OP:
                    {
                        EFI_IFR_FORM_SET hdr = (EFI_IFR_FORM_SET)elem.Header;

                        // Prepare style..
                        string StyleString = Environment.NewLine
                            // elements
                            + "body {" + Environment.NewLine
                            + "  font-family: Courier New;" + Environment.NewLine
                            + "}" + Environment.NewLine
                            + "table.If, table.SupressIf, table.DisableIf {" + Environment.NewLine
                            + "  border: 1px solid black;" + Environment.NewLine
                            + "}" + Environment.NewLine
                            + "table.GrayOutIf {" + Environment.NewLine
                            + "  border: 1px solid gray;" + Environment.NewLine
                            + "}" + Environment.NewLine
                            + "table.WarningIf, table.InconsistendIf {" + Environment.NewLine
                            + "  border: 1px solid yellow;" + Environment.NewLine
                            + "}" + Environment.NewLine
                            + "table.NoSubmitIf {" + Environment.NewLine
                            + "  border: 1px solid red;" + Environment.NewLine
                            + "}" + Environment.NewLine
                            + "td {" + Environment.NewLine
                            + "  vertical-align:top;" + Environment.NewLine
                            + "}" + Environment.NewLine
                            + "summary {" + Environment.NewLine
                            + "  font-size: 0.8em;" + Environment.NewLine
                            + "  font-style: italic;" + Environment.NewLine
                            + "}" + Environment.NewLine
                            // classes
                            + ".full {" + Environment.NewLine
                            + "  width: 100%;" + Environment.NewLine
                            + "}" + Environment.NewLine
                            + ".third {" + Environment.NewLine
                            + "  width: 33%;" + Environment.NewLine
                            + "}" + Environment.NewLine
                            + ".note {" + Environment.NewLine
                            + "  color: green;" + Environment.NewLine
                            + "  font-size: 0.8em;" + Environment.NewLine
                            + "  font-style: italic;" + Environment.NewLine
                            + "}" + Environment.NewLine;

                        // Prepare new document..
                        doc = new XmlDocument();
                        doc.AppendChild(doc.CreateDocumentType("html", null, null, null)); // produce DOCTYPE for HTML5
                        XmlNode root_html = doc.AddElementNode(doc, "html");
                        root_html.AddElementNode(doc, "head").AddElementNode(doc, "style").AddTextNode(doc, StyleString);
                        root_html.AddElementNode(doc, "title").AddTextNode(doc, GetStringOfPackages(hdr.FormSetTitle, hpkelem.UniqueID));
                        XmlNode body = root_html.AddElementNode(doc, "body");

                        // Process formset data..
                        string prefix = "FormSet";
                        string DetailsString = prefix + "-Help = " + GetStringOfPackages(hdr.Help, hpkelem.UniqueID) + Environment.NewLine
                            + prefix + "-Guid = " + hdr.Guid.Guid.ToString() + Environment.NewLine;
                        foreach (EFI_GUID classguid in (List<EFI_GUID>)elem.Payload)
                            DetailsString += prefix + "-ClassGuid = " + classguid.Guid.ToString() + Environment.NewLine;
                        DetailsString += prefix + "-Varstores:" + Environment.NewLine;
                        foreach (HiiIfrOpCode child in elem.Childs)
                        {
                            switch (child.OpCode)
                            {
                                case EFI_IFR_OPCODE_e.EFI_IFR_DEFAULTSTORE_OP:
                                case EFI_IFR_OPCODE_e.EFI_IFR_VARSTORE_OP:
                                case EFI_IFR_OPCODE_e.EFI_IFR_VARSTORE_EFI_OP:
                                case EFI_IFR_OPCODE_e.EFI_IFR_VARSTORE_NAME_VALUE_OP:
                                case EFI_IFR_OPCODE_e.EFI_IFR_VARSTORE_DEVICE_OP:
                                    XmlNode varstores = doc.CreateElement("varstores");
                                    ParsePackageIfr(child, doc, varstores);
                                    if (0 < varstores.ChildNodes.Count)
                                        DetailsString += prefix + "-ClassGuid = " + varstores.ChildNodes[0].InnerText + Environment.NewLine;
                                    break;
                                default: break;
                            }
                        }
                        body.AddDetailsNode(doc, prefix).AddTextNode(doc, DetailsString);

                        // Process forms..
                        foreach (HiiIfrOpCode child in elem.Childs)
                        {
                            switch (child.OpCode)
                            {
                                case EFI_IFR_OPCODE_e.EFI_IFR_DEFAULTSTORE_OP:
                                case EFI_IFR_OPCODE_e.EFI_IFR_VARSTORE_OP:
                                case EFI_IFR_OPCODE_e.EFI_IFR_VARSTORE_EFI_OP:
                                case EFI_IFR_OPCODE_e.EFI_IFR_VARSTORE_NAME_VALUE_OP:
                                case EFI_IFR_OPCODE_e.EFI_IFR_VARSTORE_DEVICE_OP:
                                    break;
                                default:
                                    ParsePackageIfr(child, doc, body); break;
                            }
                        }

                        // Write file with correct encoding and BOM (HTML5 UTF-16)
                        XmlWriterSettings settings = new XmlWriterSettings
                        {
                            Encoding = Encoding.Unicode,
                            Indent = true,
                            IndentChars = "  ",
                            OmitXmlDeclaration = true // not used by HTML5
                        };
                        using (XmlWriter writer = XmlWriter.Create(hdr.Guid.Guid.ToString() + ".html", settings)) doc.Save(writer); // Save file!

                        bProcessChilds = false;
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_FORM_OP:
                    {
                        EFI_IFR_FORM ifr_hdr = (EFI_IFR_FORM)hpkelem.Header;
                        root.AddElementNode(doc, "hr");
                        XmlNode node = root.AddElementNode(doc, "h1");
                        node.SetAttribute(doc, "id", "form_" + ifr_hdr.FormId.ToString());
                        node.AddTextNode(doc, GetStringOfPackages(ifr_hdr.FormTitle, hpkelem.UniqueID));
                        CurrFormId = ifr_hdr.FormId; // We just entered a new form, so remember its ID
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_REF_OP:
                    {
                        string Uri = "#form_" + CurrFormId.ToString(); // Local by default
                        UInt16 NameStrId = 0;
                        switch (hpkelem.Header.GetType().Name)
                        {
                            case "EFI_IFR_REF":
                                {
                                    EFI_IFR_REF ifr_hdr = (EFI_IFR_REF)hpkelem.Header;
                                    NameStrId = ifr_hdr.Question.Header.Prompt;
                                    if (0 != ifr_hdr.FormId) // Not local?
                                        Uri = "#form_" + ifr_hdr.FormId.ToString();
                                }
                                break;
                            case "EFI_IFR_REF2":
                                {
                                    EFI_IFR_REF2 ifr_hdr = (EFI_IFR_REF2)hpkelem.Header;
                                    NameStrId = ifr_hdr.Question.Header.Prompt;
                                    if (0 != ifr_hdr.FormId) // Not local?
                                        Uri = "#form_" + ifr_hdr.FormId.ToString();
                                    if (0 != ifr_hdr.QuestionId) Uri += "_question_" + ifr_hdr.QuestionId.ToString();
                                }
                                break;
                            case "EFI_IFR_REF3":
                                {
                                    EFI_IFR_REF3 ifr_hdr = (EFI_IFR_REF3)hpkelem.Header;
                                    NameStrId = ifr_hdr.Question.Header.Prompt;
                                    if (0 != ifr_hdr.FormId) // Not local?
                                        Uri = "#form_" + ifr_hdr.FormId.ToString();
                                    if (0 != ifr_hdr.QuestionId) Uri += "_question_" + ifr_hdr.QuestionId.ToString();
                                    if (Guid.Empty != ifr_hdr.FormSetId.Guid) Uri = ifr_hdr.FormSetId.Guid.ToString() + ".html" + Uri; // External formset
                                }
                                break;
                            case "EFI_IFR_REF4":
                                {
                                    EFI_IFR_REF4 ifr_hdr = (EFI_IFR_REF4)hpkelem.Header;
                                    NameStrId = ifr_hdr.Question.Header.Prompt;
                                    if (0 != ifr_hdr.FormId) // Not local?
                                        Uri = "#form_" + ifr_hdr.FormId.ToString();
                                    if (0 != ifr_hdr.QuestionId) Uri += "_question_" + ifr_hdr.QuestionId.ToString();
                                    if (Guid.Empty != ifr_hdr.FormSetId.Guid) Uri = ifr_hdr.FormSetId.Guid.ToString() + ".html" + Uri; // External formset
                                    if (0 != ifr_hdr.DevicePath) Uri += "?DevicePath=" + GetStringOfPackages(ifr_hdr.DevicePath, hpkelem.UniqueID);
                                }
                                break;
                            case "EFI_IFR_REF5":
                                {
                                    CreateLogEntryBuilder(LogSeverity.WARNING, "Nested reference cannot be resolved [" + hpkelem.UniqueID + "]!");
                                    Uri = null;
                                }
                                break;
                            default:
                                CreateLogEntryBuilder(LogSeverity.WARNING, "Unknown reference type [" + hpkelem.UniqueID + "]!");
                                Uri = null;
                                break;
                        }

                        if (null != Uri)
                        {
                            XmlNode node = root.AddElementNode(doc, "a");
                            node.SetAttribute(doc, "href", Uri);
                            node.AddTextNode(doc, GetStringOfPackages(NameStrId, hpkelem.UniqueID));
                            root.AddElementNode(doc, "br");
                        }
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_GUID_OP: root.AddDetailsNode(doc, "GuidOp").AddTextNode(doc, ((EFI_IFR_GUID)hpkelem.Header).Guid.Guid.ToString()); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_FORM_MAP_OP:
                    {
                        EFI_IFR_FORM_MAP hdr = (EFI_IFR_FORM_MAP)elem.Header;
                        string prefix = "FormMap";
                        string DetailsString = prefix + " Id = " + hdr.FormId.ToDecimalString(5) + Environment.NewLine;
                        foreach (EFI_IFR_FORM_MAP_METHOD method in (List<EFI_IFR_FORM_MAP_METHOD>)elem.Payload)
                        {
                            DetailsString += prefix + "-Method = " + method.MethodIdentifier.Guid.ToString()
                                + " " + method.MethodTitle.ToDecimalString(5) + " [\"" + GetStringOfPackages(method.MethodTitle, hpkelem.UniqueID) + "\"]" + Environment.NewLine;
                        }
                        root.AddDetailsNode(doc, prefix).AddTextNode(doc, DetailsString);
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_MODAL_TAG_OP: root.AddDetailsNode(doc, "Modal"); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_REFRESH_ID_OP: root.AddDetailsNode(doc, "RefreshId").AddTextNode(doc, ((EFI_IFR_REFRESH_ID)hpkelem.Header).RefreshEventGroupId.Guid.ToString()); break;
#endregion

#region Varstores
                case EFI_IFR_OPCODE_e.EFI_IFR_DEFAULTSTORE_OP:
                    {
                        EFI_IFR_DEFAULTSTORE ifr_hdr = (EFI_IFR_DEFAULTSTORE)hpkelem.Header;
                        root.AddTextNode(doc, "DefaultStore = " + ifr_hdr.DefaultName + " [" + ifr_hdr.DefaultId.ToString() + "]");
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_VARSTORE_OP:
                    {
                        EFI_IFR_VARSTORE ifr_hdr = (EFI_IFR_VARSTORE)hpkelem.Header;
                        root.AddTextNode(doc, "VarStore"
                            + " [Id = " + ifr_hdr.VarStoreId.ToDecimalString(5) + ", Guid = " + ifr_hdr.Guid.Guid.ToString() + "]"
                            + " \"" + ((HiiIfrOpCodeWithAsciiNullTerminatedString<EFI_IFR_VARSTORE>.NamedPayload_t)hpkelem.Payload).Name + "\"");
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_VARSTORE_EFI_OP:
                    {
                        EFI_IFR_VARSTORE_EFI ifr_hdr = (EFI_IFR_VARSTORE_EFI)hpkelem.Header;
                        root.AddTextNode(doc, "VarStore"
                            + " [Id = " + ifr_hdr.VarStoreId.ToDecimalString(5) + ", Guid = " + ifr_hdr.Guid.Guid.ToString() + "]"
                            + " \"" + ((HiiIfrOpCodeWithAsciiNullTerminatedString<EFI_IFR_VARSTORE_EFI>.NamedPayload_t)hpkelem.Payload).Name + "\"");
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_VARSTORE_NAME_VALUE_OP:
                    {
                        EFI_IFR_VARSTORE_NAME_VALUE ifr_hdr = (EFI_IFR_VARSTORE_NAME_VALUE)hpkelem.Header;
                        root.AddTextNode(doc, "VarStore"
                             + " [Id = " + ifr_hdr.VarStoreId.ToDecimalString(5) + ", Guid = " + ifr_hdr.Guid.Guid.ToString() + "]");
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_VARSTORE_DEVICE_OP:
                    {
                        EFI_IFR_VARSTORE_DEVICE ifr_hdr = (EFI_IFR_VARSTORE_DEVICE)hpkelem.Header;
                        root.AddTextNode(doc, "VarStore \"" + GetStringOfPackages(ifr_hdr.DevicePath, hpkelem.UniqueID) + "\"");
                    }
                    break;
#endregion

#region Logic
                case EFI_IFR_OPCODE_e.EFI_IFR_SUPPRESS_IF_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_GRAY_OUT_IF_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_DISABLE_IF_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_WARNING_IF_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_NO_SUBMIT_IF_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_INCONSISTENT_IF_OP:
                    {
                        string LogicName = "If";
                        switch (elem.OpCode)
                        {
                            case EFI_IFR_OPCODE_e.EFI_IFR_SUPPRESS_IF_OP: LogicName = "SupressIf"; break;
                            case EFI_IFR_OPCODE_e.EFI_IFR_GRAY_OUT_IF_OP: LogicName = "GrayOutIf"; break;
                            case EFI_IFR_OPCODE_e.EFI_IFR_DISABLE_IF_OP: LogicName = "DisableIf"; break;
                            case EFI_IFR_OPCODE_e.EFI_IFR_WARNING_IF_OP: LogicName = "WarningIf"; break;
                            case EFI_IFR_OPCODE_e.EFI_IFR_NO_SUBMIT_IF_OP: LogicName = "NoSubmitIf"; break;
                            case EFI_IFR_OPCODE_e.EFI_IFR_INCONSISTENT_IF_OP: LogicName = "InconsistendIf"; break;
                            default: break;
                        }

                        if (elem.Childs.Count < 1)
                            CreateLogEntryBuilder(LogSeverity.WARNING, "Too few logic elements [" + hpkelem.UniqueID + "]!");
                        else
                        {
                            XmlNode td = root.AddElementNode(doc, "table").SetAttribute(doc, "class", "full " + LogicName).AddElementNode(doc, "tr").AddElementNode(doc, "td");
                            XmlNode cond = td.AddConditionalNode(doc, LogicName, GetIfrLogicString((HiiIfrOpCode)elem.Childs[0]));
                            for (int i = 1; i < elem.Childs.Count; i++) // skip first element, because it contains the (nested) logic
                                ParsePackageIfr(elem.Childs[i], doc, cond, CurrFormId, CurrentQuestion);
                        }

                        bProcessChilds = false;
                    }
                    break;
#endregion

#region Visuals
                case EFI_IFR_OPCODE_e.EFI_IFR_SUBTITLE_OP:
                    {
                        EFI_IFR_SUBTITLE ifr_hdr = (EFI_IFR_SUBTITLE)hpkelem.Header;
                        XmlNode node = root.AddElementNode(doc, "h2");
                        node.AddTextNode(doc, GetStringOfPackages(ifr_hdr.Statement.Prompt, hpkelem.UniqueID));
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_TEXT_OP:
                    {
                        EFI_IFR_TEXT ifr_hdr = (EFI_IFR_TEXT)hpkelem.Header;
                        XmlNode tr = root.AddElementNode(doc, "table").SetAttribute(doc, "class", "full").AddElementNode(doc, "tr");
                        tr.AddElementNode(doc, "td").SetAttribute(doc, "class", "third").AddTextNode(doc, GetStringOfPackages(ifr_hdr.Statement.Prompt, hpkelem.UniqueID));
                        tr.AddElementNode(doc, "td").SetAttribute(doc, "class", "third").AddTextNode(doc, GetStringOfPackages(ifr_hdr.TextTwo, hpkelem.UniqueID));
                        tr.AddElementNode(doc, "td").SetAttribute(doc, "class", "third").AddTextNode(doc, GetStringOfPackages(ifr_hdr.Statement.Help, hpkelem.UniqueID));
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_IMAGE_OP: root.AddDetailsNode(doc, "ImageOp").AddTextNode(doc, ((EFI_IFR_IMAGE)hpkelem.Header).Id.ToString()); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_ONE_OF_OP:
                    {
                        EFI_IFR_ONE_OF ifr_hdr = (EFI_IFR_ONE_OF)hpkelem.Header;
                        XmlNode select = ProduceSelectField(root, doc, CurrFormId, ifr_hdr.Question, hpkelem.UniqueID);
                        string InfoStr = "Flags = 0x" + ifr_hdr.Flags.ToString("X2") + Environment.NewLine;
                        switch (ifr_hdr.Flags_DataSize)
                        {
                            case EFI_IFR_NUMERIC_SIZE_e.EFI_IFR_NUMERIC_SIZE_1:
                                {
                                    EFI_IFR_NUMERIC_MINMAXSTEP_DATA_8 data = (EFI_IFR_NUMERIC_MINMAXSTEP_DATA_8)hpkelem.Payload;
                                    InfoStr += "Min = " + data.MinValue.ToString() + Environment.NewLine
                                        + "Max = " + data.MaxValue.ToString() + Environment.NewLine
                                        + "Step = " + data.Step.ToString() + Environment.NewLine;
                                }
                                break;
                            case EFI_IFR_NUMERIC_SIZE_e.EFI_IFR_NUMERIC_SIZE_2:
                                {
                                    EFI_IFR_NUMERIC_MINMAXSTEP_DATA_16 data = (EFI_IFR_NUMERIC_MINMAXSTEP_DATA_16)hpkelem.Payload;
                                    InfoStr += "Min = " + data.MinValue.ToString() + Environment.NewLine
                                        + "Max = " + data.MaxValue.ToString() + Environment.NewLine
                                        + "Step = " + data.Step.ToString() + Environment.NewLine;
                                }
                                break;
                            case EFI_IFR_NUMERIC_SIZE_e.EFI_IFR_NUMERIC_SIZE_4:
                                {
                                    EFI_IFR_NUMERIC_MINMAXSTEP_DATA_32 data = (EFI_IFR_NUMERIC_MINMAXSTEP_DATA_32)hpkelem.Payload;
                                    InfoStr += "Min = " + data.MinValue.ToString() + Environment.NewLine
                                        + "Max = " + data.MaxValue.ToString() + Environment.NewLine
                                        + "Step = " + data.Step.ToString() + Environment.NewLine;
                                }
                                break;
                            case EFI_IFR_NUMERIC_SIZE_e.EFI_IFR_NUMERIC_SIZE_8:
                                {
                                    EFI_IFR_NUMERIC_MINMAXSTEP_DATA_64 data = (EFI_IFR_NUMERIC_MINMAXSTEP_DATA_64)hpkelem.Payload;
                                    InfoStr += "Min = " + data.MinValue.ToString() + Environment.NewLine
                                        + "Max = " + data.MaxValue.ToString() + Environment.NewLine
                                        + "Step = " + data.Step.ToString() + Environment.NewLine;
                                }
                                break;
                            default:
                                CreateLogEntryBuilder(LogSeverity.WARNING, "Unknown numeric type [" + hpkelem.UniqueID + "]!");
                                break;
                        }
                        select.AddDetailsNode(doc, "OneOf-Question").AddTextNode(doc, GetIfrQuestionInfoString(ifr_hdr.Question, hpkelem.UniqueID) + InfoStr);
                        CurrentQuestion = select; // We just built a new question, so remember it
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_CHECKBOX_OP:
                    {
                        EFI_IFR_CHECKBOX ifr_hdr = (EFI_IFR_CHECKBOX)hpkelem.Header;
                        XmlNode input = ProduceInputField(root, doc, "checkbox", CurrFormId, ifr_hdr.Question, hpkelem.UniqueID);
                        input.SetAttribute(doc, "value", "1"); // dummy
                        if (ifr_hdr.Flags.HasFlag(EFI_IFR_CHECKBOX_e.EFI_IFR_CHECKBOX_DEFAULT)) input.SetAttribute(doc, "checked", "true");
                        input.AddDetailsNode(doc, "Checkbox-Question").AddTextNode(doc, GetIfrQuestionInfoString(ifr_hdr.Question, hpkelem.UniqueID)
                            + "Flags = " + ifr_hdr.Flags.ToString() + Environment.NewLine);
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_NUMERIC_OP:
                    {
                        EFI_IFR_NUMERIC ifr_hdr = (EFI_IFR_NUMERIC)hpkelem.Header;
                        XmlNode input = ProduceInputField(root, doc, "number", CurrFormId, ifr_hdr.Question, hpkelem.UniqueID);
                        string InfoStr = "Flags = 0x" + ifr_hdr.Flags.ToString("X2") + Environment.NewLine;
                        switch (ifr_hdr.Flags_DataSize)
                        {
                            case EFI_IFR_NUMERIC_SIZE_e.EFI_IFR_NUMERIC_SIZE_1:
                                {
                                    EFI_IFR_NUMERIC_MINMAXSTEP_DATA_8 data = (EFI_IFR_NUMERIC_MINMAXSTEP_DATA_8)hpkelem.Payload;
                                    InfoStr += "Min = " + data.MinValue.ToString() + Environment.NewLine
                                        + "Max = " + data.MaxValue.ToString() + Environment.NewLine
                                        + "Step = " + data.Step.ToString() + Environment.NewLine;
                                }
                                break;
                            case EFI_IFR_NUMERIC_SIZE_e.EFI_IFR_NUMERIC_SIZE_2:
                                {
                                    EFI_IFR_NUMERIC_MINMAXSTEP_DATA_16 data = (EFI_IFR_NUMERIC_MINMAXSTEP_DATA_16)hpkelem.Payload;
                                    InfoStr += "Min = " + data.MinValue.ToString() + Environment.NewLine
                                        + "Max = " + data.MaxValue.ToString() + Environment.NewLine
                                        + "Step = " + data.Step.ToString() + Environment.NewLine;
                                }
                                break;
                            case EFI_IFR_NUMERIC_SIZE_e.EFI_IFR_NUMERIC_SIZE_4:
                                {
                                    EFI_IFR_NUMERIC_MINMAXSTEP_DATA_32 data = (EFI_IFR_NUMERIC_MINMAXSTEP_DATA_32)hpkelem.Payload;
                                    InfoStr += "Min = " + data.MinValue.ToString() + Environment.NewLine
                                        + "Max = " + data.MaxValue.ToString() + Environment.NewLine
                                        + "Step = " + data.Step.ToString() + Environment.NewLine;
                                }
                                break;
                            case EFI_IFR_NUMERIC_SIZE_e.EFI_IFR_NUMERIC_SIZE_8:
                                {
                                    EFI_IFR_NUMERIC_MINMAXSTEP_DATA_64 data = (EFI_IFR_NUMERIC_MINMAXSTEP_DATA_64)hpkelem.Payload;
                                    InfoStr += "Min = " + data.MinValue.ToString() + Environment.NewLine
                                        + "Max = " + data.MaxValue.ToString() + Environment.NewLine
                                        + "Step = " + data.Step.ToString() + Environment.NewLine;
                                }
                                break;
                            default:
                                CreateLogEntryBuilder(LogSeverity.WARNING, "Unknown numeric type [" + hpkelem.UniqueID + "]!");
                                break;
                        }
                        input.AddDetailsNode(doc, "Numeric-Question").AddTextNode(doc, GetIfrQuestionInfoString(ifr_hdr.Question, hpkelem.UniqueID) + InfoStr);
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_PASSWORD_OP:
                    {
                        EFI_IFR_PASSWORD ifr_hdr = (EFI_IFR_PASSWORD)hpkelem.Header;
                        XmlNode input = ProduceInputField(root, doc, "password", CurrFormId, ifr_hdr.Question, hpkelem.UniqueID);
                        input.AddDetailsNode(doc, "Password-Question").AddTextNode(doc, GetIfrQuestionInfoString(ifr_hdr.Question, hpkelem.UniqueID)
                            + "Min = " + ifr_hdr.MinSize.ToString() + Environment.NewLine
                            + "Max = " + ifr_hdr.MaxSize.ToString() + Environment.NewLine);
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_ONE_OF_OPTION_OP:
                    {
                        EFI_IFR_ONE_OF_OPTION ifr_hdr = (EFI_IFR_ONE_OF_OPTION)hpkelem.Header;
                        if (null == CurrentQuestion)
                            CreateLogEntryBuilder(LogSeverity.WARNING, "Related question not found for option [" + hpkelem.UniqueID + "]!");
                        else
                        {
                            CurrentQuestion.AddElementNode(doc, "option").AddTextNode(doc, GetStringOfPackages(ifr_hdr.Option, hpkelem.UniqueID));

                            string DetailsString = "Option = \"" + GetStringOfPackages(ifr_hdr.Option, hpkelem.UniqueID) + "\"" + Environment.NewLine
                                + "Flags = " + ifr_hdr.Flags.ToString() + Environment.NewLine
                                + GetIfrTypeValueString(ifr_hdr.Type, hpkelem.Payload, hpkelem.UniqueID) + Environment.NewLine;

                            // Parse nested value logic..
                            if (ifr_hdr.Type == EFI_IFR_TYPE_e.EFI_IFR_TYPE_OTHER)
                            {
                                if (elem.Childs.Count < 2)
                                    CreateLogEntryBuilder(LogSeverity.WARNING, "Too few value opcodes [" + hpkelem.UniqueID + "]!");
                                if (2 < elem.Childs.Count)
                                    CreateLogEntryBuilder(LogSeverity.WARNING, "Too many value opcodes [" + hpkelem.UniqueID + "]!");
                                else
                                {
                                    // Child index: 0 = Value opcode, 1 = END opcode
                                    DetailsString += Environment.NewLine + "Nested value = " + GetIfrLogicString((HiiIfrOpCode)elem.Childs[0]);
                                    bProcessChilds = false;
                                }
                            }

                            CurrentQuestion.ParentNode.AddDetailsNode(doc, "OneOfOption").AddTextNode(doc, DetailsString);
                        }
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_LOCKED_OP: root.AddDetailsNode(doc, "Locked"); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_ACTION_OP:
                    {
                        switch (hpkelem.Header.GetType().Name)
                        {
                            case "EFI_IFR_ACTION":
                                {
                                    EFI_IFR_ACTION ifr_hdr = (EFI_IFR_ACTION)hpkelem.Header;
                                    string InfoStr = "";
                                    if (ifr_hdr.QuestionConfig != 0)
                                        InfoStr += "Config = \"" + GetStringOfPackages(ifr_hdr.QuestionConfig, hpkelem.UniqueID) + "\"" + Environment.NewLine;
                                    XmlNode input = ProduceInputField(root, doc, "button", CurrFormId, ifr_hdr.Question, hpkelem.UniqueID);
                                    input.SetAttribute(doc, "value", "Action");
                                    input.AddDetailsNode(doc, "ActionButton-Question").AddTextNode(doc, GetIfrQuestionInfoString(ifr_hdr.Question, hpkelem.UniqueID) + InfoStr);
                                }
                                break;
                            case "EFI_IFR_ACTION_1":
                                {
                                    EFI_IFR_ACTION_1 ifr_hdr = (EFI_IFR_ACTION_1)hpkelem.Header;
                                    XmlNode input = ProduceInputField(root, doc, "button", CurrFormId, ifr_hdr.Question, hpkelem.UniqueID);
                                    input.SetAttribute(doc, "value", "Action");
                                    input.AddDetailsNode(doc, "ActionButton-Question").AddTextNode(doc, GetIfrQuestionInfoString(ifr_hdr.Question, hpkelem.UniqueID));
                                }
                                break;
                            default:
                                CreateLogEntryBuilder(LogSeverity.WARNING, "Unknown action type [" + hpkelem.UniqueID + "]!");
                                break;
                        }
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_RESET_BUTTON_OP:
                    {
                        XmlNode input = root.AddElementNode(doc, "input");
                        input.SetAttribute(doc, "type", "button");
                        input.SetAttribute(doc, "value", "Reset");
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_DATE_OP:
                    {
                        EFI_IFR_DATE ifr_hdr = (EFI_IFR_DATE)hpkelem.Header;
                        XmlNode input = ProduceInputField(root, doc, "date", CurrFormId, ifr_hdr.Question, hpkelem.UniqueID);
                        input.AddDetailsNode(doc, "Date-Question").AddTextNode(doc, GetIfrQuestionInfoString(ifr_hdr.Question, hpkelem.UniqueID));
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_TIME_OP:
                    {
                        EFI_IFR_TIME ifr_hdr = (EFI_IFR_TIME)hpkelem.Header;
                        XmlNode input = ProduceInputField(root, doc, "time", CurrFormId, ifr_hdr.Question, hpkelem.UniqueID);
                        input.AddDetailsNode(doc, "Time-Question").AddTextNode(doc, GetIfrQuestionInfoString(ifr_hdr.Question, hpkelem.UniqueID));
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_STRING_OP:
                    {
                        EFI_IFR_STRING ifr_hdr = (EFI_IFR_STRING)hpkelem.Header;
                        XmlNode input = ProduceInputField(root, doc, "text", CurrFormId, ifr_hdr.Question, hpkelem.UniqueID);
                        input.AddDetailsNode(doc, "String-Question").AddTextNode(doc, GetIfrQuestionInfoString(ifr_hdr.Question, hpkelem.UniqueID)
                            + "Min = " + ifr_hdr.MinSize.ToString() + Environment.NewLine
                            + "Max = " + ifr_hdr.MaxSize.ToString() + Environment.NewLine
                            + "Flags = " + ifr_hdr.Flags.ToString() + Environment.NewLine);
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_REFRESH_OP: root.AddDetailsNode(doc, "RefreshOp").AddTextNode(doc, "Interval = " + ((EFI_IFR_REFRESH)hpkelem.Header).RefreshInterval.ToString()); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_ANIMATION_OP: root.AddDetailsNode(doc, "AnimationOp").AddTextNode(doc, "Id = " + ((EFI_IFR_ANIMATION)hpkelem.Header).Id.ToString()); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_ORDERED_LIST_OP:
                    {
                        EFI_IFR_ORDERED_LIST ifr_hdr = (EFI_IFR_ORDERED_LIST)hpkelem.Header;
                        root.AddDetailsNode(doc, "OrderedList-Question").AddTextNode(doc, GetIfrQuestionInfoString(ifr_hdr.Question, hpkelem.UniqueID)
                            + "MaxContainers = " + ifr_hdr.MaxContainers.ToString() + Environment.NewLine
                            + "Flags = " + ifr_hdr.Flags.ToString() + Environment.NewLine);
                    }
                    break;
                //EFI_IFR_READ_OP // Unclear what it does, therefore no implementation by now. If you know it, let me know ;)
                //EFI_IFR_WRITE_OP, // Unclear what it does, therefore no implementation by now. If you know it, let me know ;)
#endregion

#region Values
                case EFI_IFR_OPCODE_e.EFI_IFR_UINT8_OP: root.AddDetailsNode(doc, "UINT8").AddTextNode(doc, "0x" + ((EFI_IFR_UINT64)hpkelem.Header).Value.ToString("X2")); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_UINT16_OP: root.AddDetailsNode(doc, "UINT16").AddTextNode(doc, "0x" + ((EFI_IFR_UINT64)hpkelem.Header).Value.ToString("X4")); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_UINT32_OP: root.AddDetailsNode(doc, "UINT32").AddTextNode(doc, "0x" + ((EFI_IFR_UINT64)hpkelem.Header).Value.ToString("X8")); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_UINT64_OP: root.AddDetailsNode(doc, "UINT64").AddTextNode(doc, "0x" + ((EFI_IFR_UINT64)hpkelem.Header).Value.ToString("X16")); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_VALUE_OP: root.AddDetailsNode(doc, "ValueOp"); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_DEFAULT_OP:
                    {
                        EFI_IFR_DEFAULT ifr_hdr = (EFI_IFR_DEFAULT)hpkelem.Header;
                        string DetailsString = "Default Id = " + ifr_hdr.DefaultId.ToDecimalString(5)
                            + ", " + GetIfrTypeValueString(ifr_hdr.Type, hpkelem.Payload, hpkelem.UniqueID);

                        // Parse nested value logic..
                        if (ifr_hdr.Type == EFI_IFR_TYPE_e.EFI_IFR_TYPE_OTHER)
                        {
                            if (elem.Childs.Count < 2)
                                CreateLogEntryBuilder(LogSeverity.WARNING, "Too few value opcodes [" + hpkelem.UniqueID + "]!");
                            if (2 < elem.Childs.Count)
                                CreateLogEntryBuilder(LogSeverity.WARNING, "Too many value opcodes [" + hpkelem.UniqueID + "]!");
                            else
                            {
                                // Child index: 0 = Value opcode, 1 = END opcode
                                DetailsString += Environment.NewLine + "Nested value = " + GetIfrLogicString((HiiIfrOpCode)elem.Childs[0]);
                                bProcessChilds = false;
                            }
                        }

                        root.AddDetailsNode(doc, "DefaultOp").AddTextNode(doc, DetailsString);
                    }
                    break;
#endregion

                case EFI_IFR_OPCODE_e.EFI_IFR_END_OP: return; // Skip
                default: break; // simply add all others 1:1 when no specific handler exists
            }

            if (bProcessChilds)
                foreach (HiiIfrOpCode child in elem.Childs)
                    ParsePackageIfr(child, doc, root, CurrFormId, CurrentQuestion);
        }

        /// <summary>
        /// Generates empty input fields as XML nodes to a parent XML node
        /// </summary>
        /// <param name="Parent">Node is created as child of this element</param>
        /// <param name="Document">XmlDocument used for item generation</param>
        /// <param name="InputTypeName">Input field type name</param>
        /// <param name="FormId">Form ID which is assigned to this input field</param>
        /// <param name="Question">IFR question header</param>
        /// <param name="UniqueID">ID of the requesting HPK element (for reference on errors)</param>
        /// <returns>Generated input node</returns>
        private XmlNode ProduceInputField(XmlNode Parent, XmlDocument Document, string InputTypeName, UInt16 FormId, EFI_IFR_QUESTION_HEADER Question, int UniqueID)
        {
            XmlNode tr = Parent.AddElementNode(Document, "table").SetAttribute(Document, "class", "full").AddElementNode(Document, "tr");
            tr.AddElementNode(Document, "td").SetAttribute(Document, "class", "third").AddTextNode(Document, GetStringOfPackages(Question.Header.Prompt, UniqueID));
            XmlNode input = tr.AddElementNode(Document, "td").SetAttribute(Document, "class", "third").AddElementNode(Document, "input");
            tr.AddElementNode(Document, "td").SetAttribute(Document, "class", "third").AddTextNode(Document, GetStringOfPackages(Question.Header.Help, UniqueID));

            input.SetAttribute(Document, "id", "form_" + FormId.ToString() + "_question_" + Question.QuestionId.ToString());
            input.SetAttribute(Document, "type", InputTypeName);

            return input;
        }
   
        /// <summary>
        /// Generates empty input fields as XML nodes to a parent XML node
        /// </summary>
        /// <param name="Parent">Node is created as child of this element</param>
        /// <param name="Document">XmlDocument used for item generation</param>
        /// <param name="FormId">Form ID which is assigned to this input field</param>
        /// <param name="Question">IFR question header</param>
        /// <param name="UniqueID">ID of the requesting HPK element (for reference on errors)</param>
        /// <returns>Generated input node</returns>
        private XmlNode ProduceSelectField(XmlNode Parent, XmlDocument Document, UInt16 FormId, EFI_IFR_QUESTION_HEADER Question, int UniqueID)
        {
            XmlNode tr = Parent.AddElementNode(Document, "table").SetAttribute(Document, "class", "full").AddElementNode(Document, "tr");
            tr.AddElementNode(Document, "td").SetAttribute(Document, "class", "third").AddTextNode(Document, GetStringOfPackages(Question.Header.Prompt, UniqueID));
            XmlNode input = tr.AddElementNode(Document, "td").SetAttribute(Document, "class", "third").AddElementNode(Document, "select");
            tr.AddElementNode(Document, "td").SetAttribute(Document, "class", "third").AddTextNode(Document, GetStringOfPackages(Question.Header.Help, UniqueID));

            input.SetAttribute(Document, "id", "form_" + FormId.ToString() + "_question_" + Question.QuestionId.ToString());

            return input;
        }

        /// <summary>
        /// Builds humand readable string of an IFR question header
        /// </summary>
        /// <param name="Question">Input IFR question header</param>
        /// <param name="UniqueID">ID of the requesting HPK element (for reference on errors)</param>
        /// <returns>Humand readable string</returns>
        private string GetIfrQuestionInfoString(EFI_IFR_QUESTION_HEADER Question, int UniqueID) // TODO! Remove UniqueID
        {
            return "QuestionFlags = " + Question.Flags + Environment.NewLine
                + "QuestionVarStoreId = " + Question.VarStoreId.ToString() + Environment.NewLine
                + "QuestionOffset/Name = " + Question.VarStoreInfo.VarOffset.ToString() + Environment.NewLine;
        }

        /// <summary>
        /// Builds humand readable string of an IFR typed values including its type and value
        /// </summary>
        /// <param name="type">The IFR type of the value</param>
        /// <param name="Value">Input value</param>
        /// <param name="UniqueID">ID of the requesting HPK element (for reference on errors)</param>
        /// <returns>Humand readable string</returns>
        private string GetIfrTypeValueString(EFI_IFR_TYPE_e type, object Value, int UniqueID)
        {
            string TypeStr;
            string ValueStr;
            switch (type)
            {
                case EFI_IFR_TYPE_e.EFI_IFR_TYPE_NUM_SIZE_8: TypeStr = "UINT8"; ValueStr = ((IfrTypeUINT8)Value).u8.ToString(); break;
                case EFI_IFR_TYPE_e.EFI_IFR_TYPE_NUM_SIZE_16: TypeStr = "UINT16"; ValueStr = ((IfrTypeUINT16)Value).u16.ToString(); break;
                case EFI_IFR_TYPE_e.EFI_IFR_TYPE_NUM_SIZE_32: TypeStr = "UINT32"; ValueStr = ((IfrTypeUINT32)Value).u32.ToString(); break;
                case EFI_IFR_TYPE_e.EFI_IFR_TYPE_NUM_SIZE_64: TypeStr = "UINT64"; ValueStr = ((IfrTypeUINT64)Value).u64.ToString(); break;
                case EFI_IFR_TYPE_e.EFI_IFR_TYPE_BOOLEAN: TypeStr = "BOOLEAN"; ValueStr = (((IfrTypeBOOLEAN)Value).b == 0 ? "FALSE" : "TRUE"); break;
                case EFI_IFR_TYPE_e.EFI_IFR_TYPE_TIME: TypeStr = "TIME"; ValueStr = "(H=" + ((EFI_HII_TIME)Value).Hour.ToString("D2") + ", M=" + ((EFI_HII_TIME)Value).Minute.ToString("D2") + ", S=" + ((EFI_HII_TIME)Value).Second.ToString("D2") + ")"; break;
                case EFI_IFR_TYPE_e.EFI_IFR_TYPE_DATE: TypeStr = "DATE"; ValueStr = "(Y=" + ((EFI_HII_DATE)Value).Year.ToString("D4") + ", M=" + ((EFI_HII_DATE)Value).Month.ToString("D2") + ", D=" + ((EFI_HII_DATE)Value).Day.ToString("D2") + ")"; break;
                case EFI_IFR_TYPE_e.EFI_IFR_TYPE_STRING: TypeStr = "STRING"; ValueStr = ((IfrTypeEFI_STRING_ID)Value).stringid.ToDecimalString(5) + " [\"" + GetStringOfPackages(((IfrTypeEFI_STRING_ID)Value).stringid, UniqueID) + "\"]"; break;
                case EFI_IFR_TYPE_e.EFI_IFR_TYPE_OTHER: TypeStr = "OTHER"; ValueStr = "<SEE NESTED IFR>"; break; // There is no value. It is nested and part of next IFR OpCode object
                case EFI_IFR_TYPE_e.EFI_IFR_TYPE_UNDEFINED: TypeStr = "UNDEFINED"; ValueStr = "<NOT AVAILABLE>"; CreateLogEntryBuilder(LogSeverity.WARNING, "Data type not speficied [" + UniqueID + "]!"); break;
                case EFI_IFR_TYPE_e.EFI_IFR_TYPE_ACTION: TypeStr = "ACTION"; ValueStr = ((IfrTypeEFI_STRING_ID)Value).stringid.ToDecimalString(5) + " [\"" + GetStringOfPackages(((IfrTypeEFI_STRING_ID)Value).stringid, UniqueID) + "\"]"; break;
                case EFI_IFR_TYPE_e.EFI_IFR_TYPE_BUFFER: TypeStr = "BUFFER"; ValueStr = "<SEE DETAILS>"; break;
                case EFI_IFR_TYPE_e.EFI_IFR_TYPE_REF: TypeStr = "REF"; ValueStr = "<SEE DETAILS>"; break;
                default: TypeStr = "?"; ValueStr = "?"; CreateLogEntryBuilder(LogSeverity.WARNING, "Unknown data type of [" + UniqueID + "]!"); break;
            }
            return "Type = " + TypeStr + ", Value = " + ValueStr;
        }
#endregion

#region Parser - IFR Logic
        /// <summary>
        /// Translates IFR logic Opcodes into human readable string
        /// </summary>
        /// <param name="ifrelem">IFR element holding the input data</param>
        /// <returns>Human readable string</returns>
        private string GetIfrLogicString(HiiIfrOpCode ifrelem)
        {
            IfrLogicStack Stack = new IfrLogicStack();
            string LogicString;

            ParseIfrLogicStack(ifrelem, Stack);

            if (Stack.Elements.Count != 1)
            {
                string StackElements = "";
                foreach (string element in Stack.Elements) StackElements += Environment.NewLine + "   " + element;
                CreateLogEntryBuilder(LogSeverity.WARNING, "Logic stack has " + Stack.Elements.Count + " elements but must have 1 only [" + ifrelem.UniqueID + "]!" + StackElements);

                StackElements = "INVALIDSTACK(";
                foreach (string element in Stack.Elements) StackElements += element + ",";
                LogicString = StackElements + ")";
            }
            else LogicString = Stack.Elements[0];

            return LogicString;
        }

        /// <summary>
        /// Parses IFR logic Opcodes and returns the remaining IFR logic stack
        /// </summary>
        /// <param name="ifrelem">IFR element holding the input data</param>
        /// <param name="Stack">Input: Current stack, Output: Remaining stack</param>
        private void ParseIfrLogicStack(HiiIfrOpCode ifrelem, IfrLogicStack Stack)
        {
            switch (ifrelem.OpCode)
            {
#region Logic OpCodes
                // Built-in functions:
                // EFI_IFR_DUP // Unclear what it does, therefore no implementation by now. If you know it, let me know ;)
                case EFI_IFR_OPCODE_e.EFI_IFR_EQ_ID_VAL_OP:
                    {
                        EFI_IFR_EQ_ID_VAL ifr_hdr = (EFI_IFR_EQ_ID_VAL)ifrelem.Header;
                        Stack.Push("(ValueOfId(" + ifr_hdr.QuestionId.ToDecimalString(5) + ") == " + ifr_hdr.Value.ToString() + ")");
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_EQ_ID_ID_OP:
                    {
                        EFI_IFR_EQ_ID_ID ifr_hdr = (EFI_IFR_EQ_ID_ID)ifrelem.Header;
                        Stack.Push("(ValueOfId(" + ifr_hdr.QuestionId1.ToDecimalString(5) + ") == ValueOfId(" + ifr_hdr.QuestionId2.ToString("#####") + "))");
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_EQ_ID_VAL_LIST_OP:
                    {
                        string ValueListStr = "";
                        foreach (IfrTypeUINT16 value in (List<IfrTypeUINT16>)ifrelem.Payload) ValueListStr += value.u16.ToString() + ",";
                        Stack.Push("(ValueOfId(" + ((EFI_IFR_EQ_ID_VAL_LIST)ifrelem.Header).QuestionId.ToDecimalString(5) + ") is in [" + ValueListStr + "])");
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_GET_OP:
                    {
                        EFI_IFR_GET ifr_hdr = (EFI_IFR_GET)ifrelem.Header;
                        Stack.Push("GetVarValue(Id = " + ifr_hdr.VarStoreId.ToDecimalString(5)
                            + ", Offset/Name = " + ifr_hdr.VarStoreInfo.VarOffset.ToDecimalString(5)
                            + ", Type = " + ifr_hdr.VarStoreType.ToString() + ")");
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_QUESTION_REF1_OP: Stack.Push("ValueOfId(" + ((EFI_IFR_QUESTION_REF1)ifrelem.Header).QuestionId.ToDecimalString(5) + ")"); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_QUESTION_REF3_OP:
                    {
                        switch (ifrelem.Header.GetType().Name)
                        {
                            case "EFI_IFR_QUESTION_REF3": Stack.Push("ValueOfId(" + Stack.Pop() + ")"); break;
                            case "EFI_IFR_QUESTION_REF3_2":
                                {
                                    EFI_IFR_QUESTION_REF3_2 ifr_hdr = (EFI_IFR_QUESTION_REF3_2)ifrelem.Header;
                                    Stack.Push("ValueOfId(" + Stack.Pop() + (ifr_hdr.DevicePath == 0 ? "" : ", DevicePath = \"" + GetStringOfPackages(ifr_hdr.DevicePath, ifrelem.UniqueID) + "\"") + ")");
                                }
                                break;
                            case "EFI_IFR_QUESTION_REF3_3":
                                {
                                    EFI_IFR_QUESTION_REF3_3 ifr_hdr = (EFI_IFR_QUESTION_REF3_3)ifrelem.Header;
                                    Stack.Push("ValueOfId(" + Stack.Pop() + ", Guid = " + ifr_hdr.Guid.Guid.ToString() + (ifr_hdr.DevicePath == 0 ? "" : ", DevicePath = \"" + GetStringOfPackages(ifr_hdr.DevicePath, ifrelem.UniqueID) + "\"") + ")");
                                }
                                break;
                            default:
                                Stack.Push("UNKNWONOPCODE(" + ifrelem.OpCode.ToString() + ")");
                                CreateLogEntryBuilder(LogSeverity.UNIMPLEMENTED, "Logic reference type " + ifrelem.OpCode.ToString() + " [" + ifrelem.UniqueID + "]!");
                                break;
                        }
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_RULE_REF_OP: Stack.Push(((EFI_IFR_RULE_REF)ifrelem.Header).RuleId.ToString()); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_STRING_REF1_OP:
                    {
                        UInt16 StringID = ((EFI_IFR_STRING_REF1)ifrelem.Header).StringId;
                        Stack.Push("GetString(Id = " + StringID.ToDecimalString(5) + " [\"" + GetStringOfPackages(StringID, ifrelem.UniqueID) + "\"])");
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_THIS_OP: Stack.Push("THIS"); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_SECURITY_OP: Stack.Push(((EFI_IFR_SECURITY)ifrelem.Header).Permissions.Guid.ToString()); break;
                // Constants
                case EFI_IFR_OPCODE_e.EFI_IFR_FALSE_OP: Stack.Push("FALSE"); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_ONE_OP: Stack.Push("1"); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_ONES_OP: Stack.Push("0xFFFFFFFFFFFFFFFF"); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_TRUE_OP: Stack.Push("TRUE"); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_UINT8_OP: Stack.Push("0x" + ((EFI_IFR_UINT8)ifrelem.Header).Value.ToString("X2")); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_UINT16_OP: Stack.Push("0x" + ((EFI_IFR_UINT16)ifrelem.Header).Value.ToString("X4")); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_UINT32_OP: Stack.Push("0x" + ((EFI_IFR_UINT32)ifrelem.Header).Value.ToString("X8")); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_UINT64_OP: Stack.Push("0x" + ((EFI_IFR_UINT64)ifrelem.Header).Value.ToString("X16")); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_UNDEFINED_OP: Stack.Push("Undefined"); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_VERSION_OP: Stack.Push("GetUefiVersion()"); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_ZERO_OP: Stack.Push("0"); break;
                // Binary operations
                case EFI_IFR_OPCODE_e.EFI_IFR_ADD_OP: Stack.Op21("+"); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_AND_OP: Stack.Op12("&&"); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_BITWISE_AND_OP: Stack.Op12("&"); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_BITWISE_OR_OP: Stack.Op12("|"); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_CATENATE_OP:
                    {
                        string str1 = Stack.Pop();
                        string str2 = Stack.Pop();
                        Stack.Push("Concat(" + str1 + " , " + str2 + ")");
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_DIVIDE_OP: Stack.Op21("/"); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_EQUAL_OP: Stack.Op12("=="); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_GREATER_EQUAL_OP: Stack.Op12(">="); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_GREATER_THAN_OP: Stack.Op12(">"); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_LESS_EQUAL_OP: Stack.Op12("<="); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_LESS_THAN_OP: Stack.Op12("<"); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_MATCH_OP:
                    {
                        string str1 = Stack.Pop();
                        string str2 = Stack.Pop();
                        Stack.Push("Match(" + str1 + " /w pattern " + str2 + ")");
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_MATCH2_OP:
                    {
                        string str1 = Stack.Pop();
                        string str2 = Stack.Pop();
                        Stack.Push("Match(" + str1 + " /w regex " + str2 + " syntax " + ((EFI_IFR_MATCH2)ifrelem.Header).SyntaxType.Guid.ToString() + ")");
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_MODULO_OP: Stack.Op21("%"); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_MULTIPLY_OP: Stack.Op21("*"); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_NOT_EQUAL_OP: Stack.Op12("!="); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_OR_OP: Stack.Op12("||"); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_SHIFT_LEFT_OP: Stack.Op21("<<"); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_SHIFT_RIGHT_OP: Stack.Op21("<<"); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_SUBTRACT_OP: Stack.Op21("-"); break;
                // Unary operations
                case EFI_IFR_OPCODE_e.EFI_IFR_LENGTH_OP: Stack.Push("Length(" + Stack.Pop() + ")"); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_NOT_OP: Stack.Push("!" + Stack.Pop()); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_BITWISE_NOT_OP: Stack.Push("~" + Stack.Pop()); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_QUESTION_REF2_OP: Stack.Push("ValueOfId(" + Stack.Pop() + ")"); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_SET_OP:
                    {
                        EFI_IFR_SET ifr_hdr = (EFI_IFR_SET)ifrelem.Header;
                        Stack.Push("SetVarValue(Id = " + ifr_hdr.VarStoreId.ToDecimalString(5)
                            + ", Offset/Name = " + ifr_hdr.VarStoreInfo.VarOffset.ToDecimalString(5)
                            + ", Type = " + ifr_hdr.VarStoreType.ToString()
                            + ", Value = (" + Stack.Pop() + "))");
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_STRING_REF2_OP:
                    {
                        UInt16 StringID;
                        string expr = Stack.Pop();
                        if (UInt16.TryParse(expr, out StringID))
                            Stack.Push("GetString(Id = " + StringID.ToDecimalString(5) + " [\"" + GetStringOfPackages(StringID, ifrelem.UniqueID) + "\"])");
                        else
                            Stack.Push("GetString(Id = (" + expr + "))");
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_TO_BOOLEAN_OP: Stack.Push("ToBoolen(" + Stack.Pop() + ")"); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_TO_STRING_OP: Stack.Push("ToString((" + Stack.Pop() + ")), Format = 0x" + ((EFI_IFR_TO_STRING)ifrelem.Header).Format.ToString("X2") + ")"); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_TO_UINT_OP: Stack.Push("ToUint(" + Stack.Pop() + ")"); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_TO_UPPER_OP: Stack.Push("ToUpper(" + Stack.Pop() + ")"); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_TO_LOWER_OP: Stack.Push("ToLower(" + Stack.Pop() + ")"); break;
                // -- ternary-op
                case EFI_IFR_OPCODE_e.EFI_IFR_CONDITIONAL_OP:
                    {
                        string expr1 = Stack.Pop();
                        string expr2 = Stack.Pop();
                        string expr3 = Stack.Pop();
                        Stack.Push("(" + expr3 + " ? " + expr1 + " : " + expr2 + ")");
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_FIND_OP:
                    {
                        string expr1 = Stack.Pop();
                        string expr2 = Stack.Pop();
                        string expr3 = Stack.Pop();
                        Stack.Push("Find(" + expr3 + " in " + expr2 + ", Start = " + expr1 + ", Format = " + ((EFI_IFR_FIND)ifrelem.Header).Format.ToString() + ")");
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_MID_OP:
                    {
                        string expr1 = Stack.Pop();
                        string expr2 = Stack.Pop();
                        string expr3 = Stack.Pop();
                        Stack.Push("Mid(" + expr3 + ", Start = " + expr2 + ", Length = " + expr1 + ")");
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_TOKEN_OP:
                    {
                        string expr1 = Stack.Pop();
                        string expr2 = Stack.Pop();
                        string expr3 = Stack.Pop();
                        Stack.Push("Token(" + expr3 + ", Delimiters = " + expr2 + ", return #" + expr1 + ")");
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_SPAN_OP:
                    {
                        string expr1 = Stack.Pop();
                        string expr2 = Stack.Pop();
                        string expr3 = Stack.Pop();
                        Stack.Push("Span(" + expr3 + ", FromToCharPairs = " + expr2 + ", Start = " + expr1 + ", Flags = " + ((EFI_IFR_SPAN)ifrelem.Header).Flags.ToString() + ")");
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_MAP_OP:
                    {
                        string expr = Stack.Pop();
                        if ((ifrelem.Childs.Count % 2) != 0)
                        {
                            Stack.Push("INVALIDOPCODEPARAMETERS(" + ifrelem.OpCode.ToString() + ", Value = " + expr + ")");
                            CreateLogEntryBuilder(LogSeverity.WARNING, "Map has invalid expression pairs [" + ifrelem.UniqueID + "]!");
                        }
                        else
                        {
                            string result = "Switch(" + expr;
                            for (int i = 0; i < ifrelem.Childs.Count; i += 2) // for each expression pair
                            {
                                result += ", {" + GetIfrLogicString((HiiIfrOpCode)ifrelem.Childs[i]) // Match expression
                                    + ", " + GetIfrLogicString((HiiIfrOpCode)ifrelem.Childs[i+1]) + "}"; // Result expression
                            }
                            result += ")";
                            Stack.Push(result);
                        }
                    }
                    break;
#endregion
                case EFI_IFR_OPCODE_e.EFI_IFR_VALUE_OP: break; // Nothing to show, the value is nested..
                case EFI_IFR_OPCODE_e.EFI_IFR_END_OP: return;
                default:
                    Stack.Push("UNKNWONOPCODE(" + ifrelem.OpCode.ToString() + ")");
                    CreateLogEntryBuilder(LogSeverity.UNIMPLEMENTED, "Logic OpCode " + ifrelem.OpCode.ToString() + " [" + ifrelem.UniqueID + "]!");
                    break;
            }

            if (ifrelem.HasOwnScope)
            {
                foreach (HiiIfrOpCode child in ifrelem.Childs)
                    ParseIfrLogicStack(child, Stack);
            }
        }
#endregion

#region Parser - String Information Blocks

        /// <summary>
        /// Parses String Information Blocks and add them to a parsed HPK tree
        /// </summary>
        /// <param name="hpkelem">HPK element holding the input data</param>
        /// <param name="root">Tree node which new nodes will be added to</param>
        /// <param name="StringID">Last used string ID (increased when string found)</param>
        /// <param name="db">String database where new strings will be added to</param>
        private void ParsePackageSibt(HPKElement hpkelem, ParsedHpkNode root, ref UInt16 StringID, StringDataBase db)
        {
            ParsedHpkNode branch = new ParsedHpkNode(hpkelem, hpkelem.Name);
            HiiSibtBlockBase elem = (HiiSibtBlockBase)hpkelem;

            switch (elem.BlockType)
            {
                case EFI_HII_SIBT_e.EFI_HII_SIBT_STRING_UCS2:
                    string StringText = ((HiiSibtBlockStringUcs2.Payload_t)elem.Payload).StringText;
                    StringID++;
                    branch.Name = StringID.ToDecimalString(5) + " " + StringText;
                    db.Strings.Add(new KeyValuePair<ushort, string>(StringID, StringText));
                    break;
                case EFI_HII_SIBT_e.EFI_HII_SIBT_SKIP1: StringID += ((EFI_HII_SIBT_SKIP1_BLOCK)elem.Header).SkipCount; return; // Skip
                case EFI_HII_SIBT_e.EFI_HII_SIBT_SKIP2: StringID += ((EFI_HII_SIBT_SKIP2_BLOCK)elem.Header).SkipCount; return; // Skip
                case EFI_HII_SIBT_e.EFI_HII_SIBT_END: return; // Skip
                default:
                    foreach (HiiSibtBlockBase child in elem.Childs)
                        ParsePackageSibt(child, branch, ref StringID, db);
                    break; // simply add all others 1:1 when no specific handler exists
            }

            root.Childs.Add(branch);
        }

        /// <summary>
        /// Returns a string from the database
        /// </summary>
        /// <param name="StringID">ID of the requested string</param>
        /// <param name="UniqueID">ID of the requesting HPK element (for reference on errors)</param>
        /// <param name="bZeroIsEmpty">When true: In case StringID equals 0 then return "" instead of rising an error</param>
        /// <returns>String from database</returns>
        private string GetStringOfPackages(UInt16 StringID, int UniqueID, bool bZeroIsEmpty = true)
        {
            if (StringID == 0 && bZeroIsEmpty)
                return "";

            foreach (string lang in new string[]{Language, "en-US"}) // try current first and english as fallback
            {
                foreach (StringDataBase db in StringDB)
                {
                    if (db.Language != lang) continue;

                    foreach (KeyValuePair<UInt16, string> entry in db.Strings)
                    {
                        if (entry.Key == StringID)
                        {
                            return entry.Value;
                        }
                    }
                }
            }

            //CreateLogEntryBuilder(LogSeverity.WARNING, "StringID " + StringID + " could not be translated [" + UniqueID + "]!"); // this may occur pretty often, so keep line inactive by default
            return "UNKNOWN_STRING_ID(" + StringID + ")";
        }
#endregion
    }
}