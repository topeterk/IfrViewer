//MIT License
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
        /// <param name="bShowMsgBox">Shows message box when true</param>
        public static void CreateLogEntryBuilder(LogSeverity severity, string msg, bool bShowMsgBox = false)
        {
            CreateLogEntry(severity, "Builder", msg, bShowMsgBox);
        }

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
            XmlNode node = Document.CreateTextNode(Text == "" ? "\u00A0" : Text);
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
        /// Appends a table node to an XML node
        /// </summary>
        /// <param name="Parent">Node is created as child of this element</param>
        /// <param name="Document">XmlDocument used for item generation</param>
        /// <param name="TableClass">Class of table style</param>
        /// <param name="ColClasses">List of span-class pairs</param>
        /// <returns>Generated node</returns>
        public static XmlNode AddTableNode(this XmlNode Parent, XmlDocument Document, string TableClass, string[,] ColClasses)
        {
            XmlNode table = Parent.AddElementNode(Document, "table").SetAttribute(Document, "class", TableClass);
            XmlNode colgroup = table.AddElementNode(Document, "colgroup");
            for (int i = 0; i < ColClasses.Length / 2; i++)
            {
                XmlNode col = colgroup.AddElementNode(Document, "col").SetAttribute(Document, "class", ColClasses[i, 1]);
                if (null != ColClasses[i, 0]) col.SetAttribute(Document, "span", ColClasses[i, 0]);
            }
            return table.AddElementNode(Document, "tr"); // first row
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
        /// Controls printing details
        /// </summary>
        private readonly bool bShowDetails;

        /// <summary>
        /// Controls HTML generation:
        /// true: Create files for each formset only
        /// false: Create files for each form and formset
        /// </summary>
        private readonly bool bCompactHtmlOutput;

        /// <summary>
        /// Parsed HPK strings
        /// </summary>
        private readonly ParsedHpkStringContainer HpkStrings;

        /// <summary>
        /// Parses a set of packages
        /// </summary>
        /// <param name="Packages">List of packages that will be parsed</param>
        /// <param name="HpkStrings">Parsed HPK strings used for translations</param>
        /// <param name="bShowDetails">Printing details into HTML</param>
        /// <param name="bCompactHtmlOutput">Controls HTML generation:<para />true = Create files for each formset only<para />false = Create files for each form and formset</param>
        public HtmlBuilder(List<HiiPackageBase> Packages, ParsedHpkStringContainer HpkStrings, bool bShowDetails, bool bCompactHtmlOutput)
        {
            this.HpkStrings = HpkStrings;
            this.bShowDetails = bShowDetails;
            this.bCompactHtmlOutput = bCompactHtmlOutput;

            foreach (HiiPackageBase pkg in Packages)
            {
                try
                {
                    ParsedHpkNode root = new ParsedHpkNode(pkg, pkg.Name);
                    switch (pkg.PackageType)
                    {
                        case EFI_HII_PACKAGE_e.EFI_HII_PACKAGE_FORMS:
                            foreach (HPKElement child in root.Origin.Childs)
                                ParsePackageIfr(child, null, null, Guid.Empty); // doc and node will be created by each formset within this package
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
                    CreateLogEntryBuilder(LogSeverity.ERROR, "Parsing failed!" + Environment.NewLine + ex.ToString(), true);
                }
            }
        }

        /// <summary>
        /// Parses IFR elements and add them to the XML document
        /// </summary>
        /// <param name="hpkelem">HPK element holding the input data</param>
        /// <param name="Document">XmlDocument used for item generation</param>
        /// <param name="root">XML node which new nodes will be added to</param>
        /// <param name="CurrFormSetGuid">Current FormSetGuid (Guid.Empty if unknown)</param>
        /// <param name="CurrFormId">Current FormId (0 if not set)</param>
        /// <param name="CurrentQuestion">Current Question (null if not set)</param>
        private void ParsePackageIfr(HPKElement hpkelem, XmlDocument doc, XmlNode root, Guid CurrFormSetGuid, UInt16 CurrFormId = 0, XmlNode CurrentQuestion = null)
        {
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

                        // Create new document..
                        doc = new XmlDocument();
                        XmlNode body = ProduceHtmlBody(doc, HpkStrings.GetString(hdr.FormSetTitle, hpkelem.UniqueID));

                        // Process formset data..
                        string prefix = "FormSet";
                        string DetailsString = prefix + "-Help = " + HpkStrings.GetString(hdr.Help, hpkelem.UniqueID) + Environment.NewLine
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
                                    ParsePackageIfr(child, doc, varstores, hdr.Guid.Guid);
                                    if (0 < varstores.ChildNodes.Count)
                                        DetailsString += prefix + "-ClassGuid = " + varstores.ChildNodes[0].InnerText + Environment.NewLine;
                                    break;
                                default: break;
                            }
                        }
                        if (bShowDetails) body.AddDetailsNode(doc, prefix).AddTextNode(doc, DetailsString);

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
                                    ParsePackageIfr(child, doc, body, hdr.Guid.Guid); break;
                            }
                        }

                        WriteHtmlFile(doc, hdr.Guid.Guid.ToString());

                        bProcessChilds = false;
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_FORM_OP:
                    {
                        EFI_IFR_FORM ifr_hdr = (EFI_IFR_FORM)hpkelem.Header;
                        string FormTitle = HpkStrings.GetString(ifr_hdr.FormTitle, hpkelem.UniqueID);
                        CurrFormId = ifr_hdr.FormId; // We just entered a new form, so remember its ID

                        if (bCompactHtmlOutput)
                        {
                            root.AddElementNode(doc, "hr");
                            XmlNode node = root.AddElementNode(doc, "h1");
                            node.SetAttribute(doc, "id", "form_" + CurrFormId.ToString());
                            node.AddTextNode(doc, FormTitle);
                        }
                        else
                        {
                            ProduceLink(root, doc, "<FORM: " + FormTitle + ">", CurrFormSetGuid, CurrFormId); // add link so, formset HTML can be used as crossreference

                            // Create new document..
                            XmlDocument form_doc = new XmlDocument();
                            XmlNode body = ProduceHtmlBody(form_doc, FormTitle);

                            // Process childs
                            foreach (HiiIfrOpCode child in elem.Childs)
                                ParsePackageIfr(child, form_doc, body, CurrFormSetGuid, CurrFormId, CurrentQuestion);

                            WriteHtmlFile(form_doc, CurrFormSetGuid.ToString() + "_form_" + CurrFormId.ToString());

                            bProcessChilds = false;
                        }

                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_REF_OP:
                    {
                        string QueryString = null;
                        Guid FormSetGuid = CurrFormSetGuid;
                        UInt16 FormId = CurrFormId;
                        UInt16 NameStrId = 0;
                        UInt16 QuestionId = 0;
                        switch (hpkelem.Header.GetType().Name)
                        {
                            case "EFI_IFR_REF":
                                {
                                    EFI_IFR_REF ifr_hdr = (EFI_IFR_REF)hpkelem.Header;
                                    NameStrId = ifr_hdr.Question.Header.Prompt;
                                    if (0 != ifr_hdr.FormId) FormId = ifr_hdr.FormId;
                                }
                                break;
                            case "EFI_IFR_REF2":
                                {
                                    EFI_IFR_REF2 ifr_hdr = (EFI_IFR_REF2)hpkelem.Header;
                                    NameStrId = ifr_hdr.Question.Header.Prompt;
                                    if (0 != ifr_hdr.FormId) FormId = ifr_hdr.FormId;
                                    if (0 != ifr_hdr.QuestionId) QuestionId = ifr_hdr.QuestionId;
                                }
                                break;
                            case "EFI_IFR_REF3":
                                {
                                    EFI_IFR_REF3 ifr_hdr = (EFI_IFR_REF3)hpkelem.Header;
                                    NameStrId = ifr_hdr.Question.Header.Prompt;
                                    if (0 != ifr_hdr.FormId) FormId = ifr_hdr.FormId;
                                    if (0 != ifr_hdr.QuestionId) QuestionId = ifr_hdr.QuestionId;
                                    if (Guid.Empty != ifr_hdr.FormSetId.Guid) FormSetGuid = ifr_hdr.FormSetId.Guid;
                                }
                                break;
                            case "EFI_IFR_REF4":
                                {
                                    EFI_IFR_REF4 ifr_hdr = (EFI_IFR_REF4)hpkelem.Header;
                                    NameStrId = ifr_hdr.Question.Header.Prompt;
                                    if (0 != ifr_hdr.FormId) FormId = ifr_hdr.FormId;
                                    if (0 != ifr_hdr.QuestionId) QuestionId = ifr_hdr.QuestionId;
                                    if (Guid.Empty != ifr_hdr.FormSetId.Guid) FormSetGuid = ifr_hdr.FormSetId.Guid;
                                    if (0 != ifr_hdr.DevicePath) QueryString = "DevicePath=" + HpkStrings.GetString(ifr_hdr.DevicePath, hpkelem.UniqueID);
                                }
                                break;
                            case "EFI_IFR_REF5": CreateLogEntryBuilder(LogSeverity.WARNING, "Nested reference cannot be resolved [" + hpkelem.UniqueID + "]!"); break;
                            default: CreateLogEntryBuilder(LogSeverity.WARNING, "Unknown reference type [" + hpkelem.UniqueID + "]!"); break;
                        }

                        if (0 != NameStrId)
                            ProduceLink(root, doc, HpkStrings.GetString(NameStrId, hpkelem.UniqueID), FormSetGuid, FormId, QuestionId, QueryString);
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_GUID_OP: if (bShowDetails) root.AddDetailsNode(doc, "GuidOp").AddTextNode(doc, ((EFI_IFR_GUID)hpkelem.Header).Guid.Guid.ToString()); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_FORM_MAP_OP:
                    if (bShowDetails)
                    {
                        EFI_IFR_FORM_MAP hdr = (EFI_IFR_FORM_MAP)elem.Header;
                        string prefix = "FormMap";
                        string DetailsString = prefix + " Id = " + hdr.FormId.ToDecimalString(5) + Environment.NewLine;
                        foreach (EFI_IFR_FORM_MAP_METHOD method in (List<EFI_IFR_FORM_MAP_METHOD>)elem.Payload)
                        {
                            DetailsString += prefix + "-Method = " + method.MethodIdentifier.Guid.ToString()
                                + " " + method.MethodTitle.ToDecimalString(5) + " [\"" + HpkStrings.GetString(method.MethodTitle, hpkelem.UniqueID) + "\"]" + Environment.NewLine;
                        }
                        root.AddDetailsNode(doc, prefix).AddTextNode(doc, DetailsString);
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_MODAL_TAG_OP: if (bShowDetails) root.AddDetailsNode(doc, "Modal"); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_REFRESH_ID_OP: if (bShowDetails) root.AddDetailsNode(doc, "RefreshId").AddTextNode(doc, ((EFI_IFR_REFRESH_ID)hpkelem.Header).RefreshEventGroupId.Guid.ToString()); break;
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
                        root.AddTextNode(doc, "VarStore \"" + HpkStrings.GetString(ifr_hdr.DevicePath, hpkelem.UniqueID) + "\"");
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
                        if (bShowDetails)
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
                                XmlNode cond = td.AddConditionalNode(doc, LogicName, HpkStrings.GetIfrLogicString((HiiIfrOpCode)elem.Childs[0]));
                                for (int i = 1; i < elem.Childs.Count; i++) // skip first element, because it contains the (nested) logic
                                    ParsePackageIfr(elem.Childs[i], doc, cond, CurrFormSetGuid, CurrFormId, CurrentQuestion);
                            }
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
                        node.AddTextNode(doc, HpkStrings.GetString(ifr_hdr.Statement.Prompt, hpkelem.UniqueID));
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_TEXT_OP:
                    {
                        EFI_IFR_TEXT ifr_hdr = (EFI_IFR_TEXT)hpkelem.Header;
                        XmlNode tr = root.AddTableNode(doc, "full", new string[,] { { null, "left" }, { null, "mid" }, { null, "right" } });
                        tr.AddElementNode(doc, "td").AddTextNode(doc, HpkStrings.GetString(ifr_hdr.Statement.Prompt, hpkelem.UniqueID));
                        tr.AddElementNode(doc, "td").AddTextNode(doc, HpkStrings.GetString(ifr_hdr.TextTwo, hpkelem.UniqueID));
                        tr.AddElementNode(doc, "td").AddTextNode(doc, HpkStrings.GetString(ifr_hdr.Statement.Help, hpkelem.UniqueID));
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_IMAGE_OP: if (bShowDetails) root.AddDetailsNode(doc, "ImageOp").AddTextNode(doc, ((EFI_IFR_IMAGE)hpkelem.Header).Id.ToString()); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_ONE_OF_OP:
                    {
                        EFI_IFR_ONE_OF ifr_hdr = (EFI_IFR_ONE_OF)hpkelem.Header;
                        XmlNode select = ProduceSelectField(root, doc, CurrFormId, ifr_hdr.Question, hpkelem.UniqueID);
                        if (bShowDetails)
                        {
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
                        }
                        CurrentQuestion = select; // We just built a new question, so remember it
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_CHECKBOX_OP:
                    {
                        EFI_IFR_CHECKBOX ifr_hdr = (EFI_IFR_CHECKBOX)hpkelem.Header;
                        XmlNode input = ProduceInputField(root, doc, "checkbox", CurrFormId, ifr_hdr.Question, hpkelem.UniqueID);
                        input.SetAttribute(doc, "value", "1"); // dummy
                        if (ifr_hdr.Flags.HasFlag(EFI_IFR_CHECKBOX_e.EFI_IFR_CHECKBOX_DEFAULT)) input.SetAttribute(doc, "checked", "true");
                        if (bShowDetails) input.AddDetailsNode(doc, "Checkbox-Question").AddTextNode(doc, GetIfrQuestionInfoString(ifr_hdr.Question, hpkelem.UniqueID)
                            + "Flags = " + ifr_hdr.Flags.ToString() + Environment.NewLine);
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_NUMERIC_OP:
                    {
                        EFI_IFR_NUMERIC ifr_hdr = (EFI_IFR_NUMERIC)hpkelem.Header;
                        XmlNode input = ProduceInputField(root, doc, "number", CurrFormId, ifr_hdr.Question, hpkelem.UniqueID);
                        if (bShowDetails)
                        {
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
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_PASSWORD_OP:
                    {
                        EFI_IFR_PASSWORD ifr_hdr = (EFI_IFR_PASSWORD)hpkelem.Header;
                        XmlNode input = ProduceInputField(root, doc, "password", CurrFormId, ifr_hdr.Question, hpkelem.UniqueID);
                        if (bShowDetails) input.AddDetailsNode(doc, "Password-Question").AddTextNode(doc, GetIfrQuestionInfoString(ifr_hdr.Question, hpkelem.UniqueID)
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

                            object OptionValue = null;
                            string OptionText = HpkStrings.GetString(ifr_hdr.Option, hpkelem.UniqueID);
                            string DetailsString = "Option = \"" + OptionText + "\"" + Environment.NewLine
                                + "Flags = " + ifr_hdr.Flags.ToString() + Environment.NewLine
                                + HpkStrings.GetValueString(ifr_hdr.Type, hpkelem.Payload, hpkelem.UniqueID, ref OptionValue) + Environment.NewLine;

                            CurrentQuestion.AddElementNode(doc, "option").SetAttribute(doc, "value", OptionValue.ToString()).AddTextNode(doc, OptionText);

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
                                    DetailsString += Environment.NewLine + "Nested value = " + HpkStrings.GetIfrLogicString((HiiIfrOpCode)elem.Childs[0]);
                                    bProcessChilds = false;
                                }
                            }

                            if (bShowDetails) CurrentQuestion.ParentNode.AddDetailsNode(doc, "OneOfOption").AddTextNode(doc, DetailsString);
                        }
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_LOCKED_OP: if (bShowDetails) root.AddDetailsNode(doc, "Locked"); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_ACTION_OP:
                    {
                        switch (hpkelem.Header.GetType().Name)
                        {
                            case "EFI_IFR_ACTION":
                                {
                                    EFI_IFR_ACTION ifr_hdr = (EFI_IFR_ACTION)hpkelem.Header;
                                    string InfoStr = "";
                                    if (ifr_hdr.QuestionConfig != 0)
                                        InfoStr += "Config = \"" + HpkStrings.GetString(ifr_hdr.QuestionConfig, hpkelem.UniqueID) + "\"" + Environment.NewLine;
                                    XmlNode input = ProduceInputField(root, doc, "button", CurrFormId, ifr_hdr.Question, hpkelem.UniqueID);
                                    input.SetAttribute(doc, "value", "Action");
                                    if (bShowDetails) input.AddDetailsNode(doc, "ActionButton-Question").AddTextNode(doc, GetIfrQuestionInfoString(ifr_hdr.Question, hpkelem.UniqueID) + InfoStr);
                                }
                                break;
                            case "EFI_IFR_ACTION_1":
                                {
                                    EFI_IFR_ACTION_1 ifr_hdr = (EFI_IFR_ACTION_1)hpkelem.Header;
                                    XmlNode input = ProduceInputField(root, doc, "button", CurrFormId, ifr_hdr.Question, hpkelem.UniqueID);
                                    input.SetAttribute(doc, "value", "Action");
                                    if (bShowDetails) input.AddDetailsNode(doc, "ActionButton-Question").AddTextNode(doc, GetIfrQuestionInfoString(ifr_hdr.Question, hpkelem.UniqueID));
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
                        if (bShowDetails) input.AddDetailsNode(doc, "Date-Question").AddTextNode(doc, GetIfrQuestionInfoString(ifr_hdr.Question, hpkelem.UniqueID));
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_TIME_OP:
                    {
                        EFI_IFR_TIME ifr_hdr = (EFI_IFR_TIME)hpkelem.Header;
                        XmlNode input = ProduceInputField(root, doc, "time", CurrFormId, ifr_hdr.Question, hpkelem.UniqueID);
                        if (bShowDetails) input.AddDetailsNode(doc, "Time-Question").AddTextNode(doc, GetIfrQuestionInfoString(ifr_hdr.Question, hpkelem.UniqueID));
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_STRING_OP:
                    {
                        EFI_IFR_STRING ifr_hdr = (EFI_IFR_STRING)hpkelem.Header;
                        XmlNode input = ProduceInputField(root, doc, "text", CurrFormId, ifr_hdr.Question, hpkelem.UniqueID);
                        if (bShowDetails) input.AddDetailsNode(doc, "String-Question").AddTextNode(doc, GetIfrQuestionInfoString(ifr_hdr.Question, hpkelem.UniqueID)
                            + "Min = " + ifr_hdr.MinSize.ToString() + Environment.NewLine
                            + "Max = " + ifr_hdr.MaxSize.ToString() + Environment.NewLine
                            + "Flags = " + ifr_hdr.Flags.ToString() + Environment.NewLine);
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_REFRESH_OP: if (bShowDetails) root.AddDetailsNode(doc, "RefreshOp").AddTextNode(doc, "Interval = " + ((EFI_IFR_REFRESH)hpkelem.Header).RefreshInterval.ToString()); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_ANIMATION_OP: if (bShowDetails) root.AddDetailsNode(doc, "AnimationOp").AddTextNode(doc, "Id = " + ((EFI_IFR_ANIMATION)hpkelem.Header).Id.ToString()); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_ORDERED_LIST_OP:
                    {
                        EFI_IFR_ORDERED_LIST ifr_hdr = (EFI_IFR_ORDERED_LIST)hpkelem.Header;
                        if (bShowDetails) root.AddDetailsNode(doc, "OrderedList-Question").AddTextNode(doc, GetIfrQuestionInfoString(ifr_hdr.Question, hpkelem.UniqueID)
                            + "MaxContainers = " + ifr_hdr.MaxContainers.ToString() + Environment.NewLine
                            + "Flags = " + ifr_hdr.Flags.ToString() + Environment.NewLine);
                    }
                    break;
                //EFI_IFR_READ_OP // Unclear what it does, therefore no implementation by now. If you know it, let me know ;)
                //EFI_IFR_WRITE_OP, // Unclear what it does, therefore no implementation by now. If you know it, let me know ;)
                #endregion

                #region Values
                case EFI_IFR_OPCODE_e.EFI_IFR_UINT8_OP: if (bShowDetails) root.AddDetailsNode(doc, "UINT8").AddTextNode(doc, "0x" + ((EFI_IFR_UINT64)hpkelem.Header).Value.ToString("X2")); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_UINT16_OP: if (bShowDetails) root.AddDetailsNode(doc, "UINT16").AddTextNode(doc, "0x" + ((EFI_IFR_UINT64)hpkelem.Header).Value.ToString("X4")); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_UINT32_OP: if (bShowDetails) root.AddDetailsNode(doc, "UINT32").AddTextNode(doc, "0x" + ((EFI_IFR_UINT64)hpkelem.Header).Value.ToString("X8")); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_UINT64_OP: if (bShowDetails) root.AddDetailsNode(doc, "UINT64").AddTextNode(doc, "0x" + ((EFI_IFR_UINT64)hpkelem.Header).Value.ToString("X16")); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_VALUE_OP: if (bShowDetails) root.AddDetailsNode(doc, "ValueOp"); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_DEFAULT_OP:
                    {
                        EFI_IFR_DEFAULT ifr_hdr = (EFI_IFR_DEFAULT)hpkelem.Header;
                        object dummy = null;
                        string DetailsString = "Default Id = " + ifr_hdr.DefaultId.ToDecimalString(5)
                            + ", " + HpkStrings.GetValueString(ifr_hdr.Type, hpkelem.Payload, hpkelem.UniqueID, ref dummy);

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
                                DetailsString += Environment.NewLine + "Nested value = " + HpkStrings.GetIfrLogicString((HiiIfrOpCode)elem.Childs[0]);
                                bProcessChilds = false;
                            }
                        }

                        if (bShowDetails) root.AddDetailsNode(doc, "DefaultOp").AddTextNode(doc, DetailsString);
                    }
                    break;
                #endregion

                case EFI_IFR_OPCODE_e.EFI_IFR_END_OP: return; // Skip
                default: break; // simply add all others 1:1 when no specific handler exists
            }

            if (bProcessChilds)
                foreach (HiiIfrOpCode child in elem.Childs)
                    ParsePackageIfr(child, doc, root, CurrFormSetGuid, CurrFormId, CurrentQuestion);
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
            XmlNode tr = Parent.AddTableNode(Document, "full", new string[,] { { null, "left" }, { null, "mid" }, { null, "right" } });
            tr.AddElementNode(Document, "td").AddTextNode(Document, HpkStrings.GetString(Question.Header.Prompt, UniqueID));
            XmlNode input = tr.AddElementNode(Document, "td").AddElementNode(Document, "input");
            tr.AddElementNode(Document, "td").AddTextNode(Document, HpkStrings.GetString(Question.Header.Help, UniqueID));

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
            XmlNode tr = Parent.AddTableNode(Document, "full", new string[,] { { null, "left" }, { null, "mid" }, { null, "right" } });
            tr.AddElementNode(Document, "td").AddTextNode(Document, HpkStrings.GetString(Question.Header.Prompt, UniqueID));
            XmlNode input = tr.AddElementNode(Document, "td").AddElementNode(Document, "select");
            tr.AddElementNode(Document, "td").AddTextNode(Document, HpkStrings.GetString(Question.Header.Help, UniqueID));

            input.SetAttribute(Document, "id", "form_" + FormId.ToString() + "_question_" + Question.QuestionId.ToString());

            return input;
        }

        /// <summary>
        /// Generates HTML linkage as XML nodes to a parent XML node
        /// </summary>
        /// <param name="Parent">Node is created as child of this element</param>
        /// <param name="Document">XmlDocument used for item generation</param>
        /// <param name="FriendlyUrlName">Displayed link text</param>
        /// <param name="FormSetGuid">Current FormSetGuid (Guid.Empty if not set)</param>
        /// <param name="FormId">Current FormId (0 if not set)</param>
        /// <param name="QuestionId">Current QuestionId (0 if not set)</param>
        /// <param name="GetQueryString">Optional GET parameters (without leading '?')</param>
        private void ProduceLink(XmlNode Parent, XmlDocument Document, string FriendlyUrlName, Guid FormSetGuid, UInt16 FormId = 0, UInt16 QuestionId = 0, string GetQueryString = null)
        {
            string Uri = FormSetGuid.ToString();

            if (bCompactHtmlOutput)
            {
                Uri += ".html";
                if (GetQueryString != null) Uri += "?" + GetQueryString;
            }
            else
            {
                if (0 != FormId) Uri += "_form_" + FormId.ToString();
                Uri += ".html";
            }

            // add GET parameters..
            if (GetQueryString != null) Uri += "?" + GetQueryString;

            // add bockmarks..
            if (0 != FormId)
            {
                Uri += "#form_" + FormId.ToString();
                if (0 != QuestionId) Uri += "_question_" + QuestionId.ToString();
            }

            Parent.AddElementNode(Document, "a").SetAttribute(Document, "href", Uri).AddTextNode(Document, FriendlyUrlName);
            Parent.AddElementNode(Document, "br");
        }

        /// <summary>
        /// Generates an empty HTML page with CSS styles and header
        /// </summary>
        /// <param name="Document">XmlDocument used for item generation</param>
        /// <param name="PageTitle">HTML page title</param>
        /// <returns>HTML body XML node</returns>
        private XmlNode ProduceHtmlBody(XmlDocument Document, string PageTitle)
        {
            // Embedded CSS style..
            string StyleString = Environment.NewLine
                // elements
                + "body {" + Environment.NewLine
                + "  font-family: Courier New;" + Environment.NewLine
                + "  background-color: lightgray;" + Environment.NewLine
                + "  color: blue;" + Environment.NewLine
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
                + "  color: black;" + Environment.NewLine
                + "}" + Environment.NewLine
                + "pre {" + Environment.NewLine
                + "  font-style: italic;" + Environment.NewLine
                + "  color: black;" + Environment.NewLine
                + "}" + Environment.NewLine
                // classes
                + ".full {" + Environment.NewLine
                + "  width: 100%;" + Environment.NewLine
                + "}" + Environment.NewLine
                + ".left {" + Environment.NewLine
                + "  width: 30%;" + Environment.NewLine
                + "}" + Environment.NewLine
                + ".mid {" + Environment.NewLine
                + "  width: 20%;" + Environment.NewLine
                + "}" + Environment.NewLine
                + ".right {" + Environment.NewLine
                + "  width: 50%;" + Environment.NewLine
                + "}" + Environment.NewLine
                + ".note {" + Environment.NewLine
                + "  font-size: 0.8em;" + Environment.NewLine
                + "  font-style: italic;" + Environment.NewLine
                + "  color: green;" + Environment.NewLine
                + "}" + Environment.NewLine;

            // Prepare new document..
            Document.AppendChild(Document.CreateDocumentType("html", null, null, null)); // produce DOCTYPE for HTML5
            XmlNode root_html = Document.AddElementNode(Document, "html");
            root_html.AddElementNode(Document, "head").AddElementNode(Document, "style").AddTextNode(Document, StyleString);
            root_html.AddElementNode(Document, "title").AddTextNode(Document, PageTitle);
            return root_html.AddElementNode(Document, "body");
        }

        /// <summary>
        /// Writes HTML data to file
        /// </summary>
        /// <param name="Document">XmlDocument that is written to file</param>
        /// <param name="Basename">Filename without file extension</param>
        private void WriteHtmlFile(XmlDocument Document, string Basename)
        {
            // Write file with correct encoding and BOM (HTML5 UTF-16)
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Encoding = System.Text.Encoding.Unicode,
                Indent = true,
                IndentChars = "  ",
                OmitXmlDeclaration = true // not used by HTML5
            };
            XmlWriter writer = XmlWriter.Create(Basename + ".html", settings);

            Document.Save(writer); // Save file!
            writer.Close();
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
    }
}
