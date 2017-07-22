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
using static IfrViewer.HpkParser;

namespace IfrViewer
{

    /// <summary>
    /// Parses HPK packages and gives access to a parsed HPK tree
    /// </summary>
    public class ParsedHpkContainer
    {
        /// <summary>
        /// Parsed HPK tree
        /// </summary>
        public readonly List<ParsedHpkNode> HpkPackages;

        /// <summary>
        /// Parsed HPK strings
        /// </summary>
        private readonly ParsedHpkStringContainer HpkStrings;

        /// <summary>
        /// Parses a set of packages
        /// </summary>
        /// <param name="Packages">List of packages that will be parsed</param>
        /// <param name="HpkStrings">Parsed HPK strings used for translations</param>
        public ParsedHpkContainer(List<HiiPackageBase> Packages, ParsedHpkStringContainer HpkStrings)
        {
            this.HpkStrings = HpkStrings;
            HpkPackages = new List<ParsedHpkNode>();

            // Copy HPK strings to HPK packages to be shown in logical view, too..
            foreach (ParsedHpkNode pkg in HpkStrings.HpkPackages)
                HpkPackages.Add(pkg);

            foreach (HiiPackageBase pkg in Packages)
            {
                try
                {
                    ParsedHpkNode root = new ParsedHpkNode(pkg, pkg.Name);
                    switch (pkg.PackageType)
                    {
                        case EFI_HII_PACKAGE_e.EFI_HII_PACKAGE_FORMS:
                            root.Name = "Form Package";
                            HpkPackages.Add(root);
                            foreach (HPKElement child in root.Origin.Childs)
                                ParsePackageIfr(child, root);
                            break;
                        case EFI_HII_PACKAGE_e.EFI_HII_PACKAGE_STRINGS: break; // Already done
                        default:
                            CreateLogEntryParser(LogSeverity.UNIMPLEMENTED, root.Name);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    CreateLogEntryParser(LogSeverity.ERROR, "Parsing failed!" + Environment.NewLine + ex.ToString());
                }
            }
        }

        /// <summary>
        /// Parses IFR elements and add them to a parsed HPK tree
        /// </summary>
        /// <param name="hpkelem">HPK element holding the input data</param>
        /// <param name="root">Tree node which new nodes will be added to</param>
        private void ParsePackageIfr(HPKElement hpkelem, ParsedHpkNode root)
        {
            ParsedHpkNode branch = new ParsedHpkNode(hpkelem, hpkelem.Name);
            HiiIfrOpCode elem = (HiiIfrOpCode)hpkelem;
            bool bProcessChilds = true;

            switch (elem.OpCode)
            {
                #region Forms
                case EFI_IFR_OPCODE_e.EFI_IFR_FORM_SET_OP:
                    {
                        EFI_IFR_FORM_SET hdr = (EFI_IFR_FORM_SET)elem.Header;
                        ParsedHpkNode leaf = new ParsedHpkNode(hpkelem, "");
                        string prefix = "FormSet";
                        branch.Name = prefix + " \"" + HpkStrings.GetString(hdr.FormSetTitle, hpkelem.UniqueID) + "\"";
                        leaf.Name = prefix + "-Help = " + HpkStrings.GetString(hdr.Help, hpkelem.UniqueID);
                        branch.Childs.Add(leaf);
                        leaf.Name = prefix + "-Guid = " + hdr.Guid.Guid.ToString();
                        branch.Childs.Add(leaf);
                        foreach (EFI_GUID classguid in (List<EFI_GUID>)elem.Payload)
                        {
                            leaf.Name = prefix + "-ClassGuid = " + classguid.Guid.ToString();
                            branch.Childs.Add(leaf);
                        }

                        ParsedHpkNode varstores = new ParsedHpkNode(hpkelem, prefix + "-Varstores");
                        branch.Childs.Add(varstores);
                        foreach (HiiIfrOpCode child in elem.Childs)
                        {
                            switch (child.OpCode)
                            {
                                case EFI_IFR_OPCODE_e.EFI_IFR_DEFAULTSTORE_OP:
                                case EFI_IFR_OPCODE_e.EFI_IFR_VARSTORE_OP:
                                case EFI_IFR_OPCODE_e.EFI_IFR_VARSTORE_EFI_OP:
                                case EFI_IFR_OPCODE_e.EFI_IFR_VARSTORE_NAME_VALUE_OP:
                                case EFI_IFR_OPCODE_e.EFI_IFR_VARSTORE_DEVICE_OP:
                                    ParsePackageIfr(child, varstores); break;
                                default:
                                    ParsePackageIfr(child, branch); break;
                            }
                        }
                        bProcessChilds = false;
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_FORM_OP:
                    {
                        EFI_IFR_FORM ifr_hdr = (EFI_IFR_FORM)hpkelem.Header;
                        branch.Name = "Form"
                            + " [Id = " + ifr_hdr.FormId.ToDecimalString(5) + "]"
                            + " \"" + HpkStrings.GetString(ifr_hdr.FormTitle, hpkelem.UniqueID) + "\"";
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_REF_OP:
                    {
                        string FormIdString = "FormId = ";
                        string FormSetIdString = "FormSetId = ";
                        string QuestionId = "QuestionId = ";
                        string InfoString = "";
                        string RefName = null;
                        switch (hpkelem.Header.GetType().Name)
                        {
                            case "EFI_IFR_REF":
                                {
                                    EFI_IFR_REF ifr_hdr = (EFI_IFR_REF)hpkelem.Header;
                                    FormIdString += ifr_hdr.FormId == 0 ? "Current" : ifr_hdr.FormId.ToDecimalString(5);
                                    InfoString = FormIdString;
                                    RefName = HpkStrings.GetString(ifr_hdr.Question.Header.Prompt, hpkelem.UniqueID);
                                }
                                break;
                            case "EFI_IFR_REF2":
                                {
                                    EFI_IFR_REF2 ifr_hdr = (EFI_IFR_REF2)hpkelem.Header;
                                    FormIdString += ifr_hdr.FormId == 0 ? "Current" : ifr_hdr.FormId.ToDecimalString(5);
                                    if (0 == ifr_hdr.QuestionId)
                                        QuestionId = "";
                                    else
                                        QuestionId += ifr_hdr.QuestionId.ToDecimalString(5);
                                    InfoString = FormIdString + ", " + QuestionId;
                                    RefName = HpkStrings.GetString(ifr_hdr.Question.Header.Prompt, hpkelem.UniqueID);
                                }
                                break;
                            case "EFI_IFR_REF3":
                                {
                                    EFI_IFR_REF3 ifr_hdr = (EFI_IFR_REF3)hpkelem.Header;
                                    FormIdString += ifr_hdr.FormId == 0 ? "Current" : ifr_hdr.FormId.ToDecimalString(5);
                                    if (0 == ifr_hdr.QuestionId)
                                        QuestionId = "";
                                    else
                                        QuestionId += ifr_hdr.QuestionId.ToDecimalString(5);
                                    InfoString = FormIdString + ", " + QuestionId;
                                    FormSetIdString += ifr_hdr.FormSetId.Guid.ToString();
                                    InfoString = FormIdString + ", " + FormSetIdString;
                                    RefName = HpkStrings.GetString(ifr_hdr.Question.Header.Prompt, hpkelem.UniqueID);
                                }
                                break;
                            case "EFI_IFR_REF4":
                                {
                                    EFI_IFR_REF4 ifr_hdr = (EFI_IFR_REF4)hpkelem.Header;
                                    FormIdString += ifr_hdr.FormId == 0 ? "Current" : ifr_hdr.FormId.ToDecimalString(5);
                                    if (0 == ifr_hdr.QuestionId)
                                        QuestionId = "";
                                    else
                                        QuestionId += ifr_hdr.QuestionId.ToDecimalString(5);
                                    InfoString = FormIdString + ", " + QuestionId;
                                    FormSetIdString += ifr_hdr.FormSetId.Guid.ToString();
                                    InfoString = FormIdString + ", " + FormSetIdString + ", DevicePath = \"" + HpkStrings.GetString(ifr_hdr.DevicePath, hpkelem.UniqueID) + "\"";
                                    RefName = HpkStrings.GetString(ifr_hdr.Question.Header.Prompt, hpkelem.UniqueID);
                                }
                                break;
                            case "EFI_IFR_REF5":
                                {
                                    EFI_IFR_REF5 ifr_hdr = (EFI_IFR_REF5)hpkelem.Header;
                                    FormIdString += "Nested";
                                    InfoString = FormIdString;
                                }
                                break;
                            default:
                                InfoString = "?";
                                CreateLogEntryParser(LogSeverity.WARNING, "Unknown reference type [" + hpkelem.UniqueID + "]!");
                                break;
                        }
                        branch.Name = "Reference to [" + InfoString + "]" + (RefName != null ? " \"" + RefName + "\"" : "");
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_GUID_OP: branch.Name = "GuidOp = " + ((EFI_IFR_GUID)hpkelem.Header).Guid.Guid.ToString(); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_FORM_MAP_OP:
                    {
                        EFI_IFR_FORM_MAP hdr = (EFI_IFR_FORM_MAP)elem.Header;
                        ParsedHpkNode leaf = new ParsedHpkNode(hpkelem, "");
                        string prefix = "FormMap";
                        branch.Name = prefix + " Id = " + hdr.FormId.ToDecimalString(5);
                        foreach (EFI_IFR_FORM_MAP_METHOD method in (List<EFI_IFR_FORM_MAP_METHOD>)elem.Payload)
                        {
                            leaf.Name = prefix + "-Method = " + method.MethodIdentifier.Guid.ToString()
                                + " " + method.MethodTitle.ToDecimalString(5) + " [\"" + HpkStrings.GetString(method.MethodTitle, hpkelem.UniqueID) + "\"]";
                            branch.Childs.Add(leaf);
                        }
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_MODAL_TAG_OP: branch.Name = "ModalTag"; break;
                case EFI_IFR_OPCODE_e.EFI_IFR_REFRESH_ID_OP: branch.Name = "RefreshId = " + ((EFI_IFR_REFRESH_ID)hpkelem.Header).RefreshEventGroupId.Guid.ToString(); break;
                #endregion

                #region Varstores
                case EFI_IFR_OPCODE_e.EFI_IFR_DEFAULTSTORE_OP:
                    {
                        EFI_IFR_DEFAULTSTORE ifr_hdr = (EFI_IFR_DEFAULTSTORE)hpkelem.Header;
                        branch.Name = "DefaultStore = " + ifr_hdr.DefaultName + " [" + ifr_hdr.DefaultId.ToString() + "]";
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_VARSTORE_OP:
                    {
                        EFI_IFR_VARSTORE ifr_hdr = (EFI_IFR_VARSTORE)hpkelem.Header;
                        branch.Name = "VarStore"
                            + " [Id = " + ifr_hdr.VarStoreId.ToDecimalString(5) + ", Guid = " + ifr_hdr.Guid.Guid.ToString() + "]"
                            + " \"" + ((HiiIfrOpCodeWithAsciiNullTerminatedString<EFI_IFR_VARSTORE>.NamedPayload_t)hpkelem.Payload).Name + "\"";
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_VARSTORE_EFI_OP:
                    {
                        EFI_IFR_VARSTORE_EFI ifr_hdr = (EFI_IFR_VARSTORE_EFI)hpkelem.Header;
                        branch.Name = "VarStore"
                            + " [Id = " + ifr_hdr.VarStoreId.ToDecimalString(5) + ", Guid = " + ifr_hdr.Guid.Guid.ToString() + "]"
                            + " \"" + ((HiiIfrOpCodeWithAsciiNullTerminatedString<EFI_IFR_VARSTORE_EFI>.NamedPayload_t)hpkelem.Payload).Name + "\"";
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_VARSTORE_NAME_VALUE_OP:
                    {
                        EFI_IFR_VARSTORE_NAME_VALUE ifr_hdr = (EFI_IFR_VARSTORE_NAME_VALUE)hpkelem.Header;
                        branch.Name = "VarStore" + " [Id = " + ifr_hdr.VarStoreId.ToDecimalString(5) + ", Guid = " + ifr_hdr.Guid.Guid.ToString() + "]";
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_VARSTORE_DEVICE_OP:
                    {
                        EFI_IFR_VARSTORE_DEVICE ifr_hdr = (EFI_IFR_VARSTORE_DEVICE)hpkelem.Header;
                        branch.Name = "VarStore" + " \"" + HpkStrings.GetString(ifr_hdr.DevicePath, hpkelem.UniqueID) + "\"";
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
                        switch (elem.OpCode)
                        {
                            case EFI_IFR_OPCODE_e.EFI_IFR_SUPPRESS_IF_OP: branch.Name = "SupressIf "; break;
                            case EFI_IFR_OPCODE_e.EFI_IFR_GRAY_OUT_IF_OP: branch.Name = "GrayOutIf "; break;
                            case EFI_IFR_OPCODE_e.EFI_IFR_DISABLE_IF_OP: branch.Name = "DisableIf "; break;
                            case EFI_IFR_OPCODE_e.EFI_IFR_WARNING_IF_OP: branch.Name = "WarningIf "; break;
                            case EFI_IFR_OPCODE_e.EFI_IFR_NO_SUBMIT_IF_OP: branch.Name = "NoSubmitIf "; break;
                            case EFI_IFR_OPCODE_e.EFI_IFR_INCONSISTENT_IF_OP: branch.Name = "InconsistendIf "; break;
                            default: break;
                        }
                        if (elem.Childs.Count < 1)
                            CreateLogEntryParser(LogSeverity.WARNING, "Too few logic elements [" + hpkelem.UniqueID + "]!");
                        else
                        {
                            branch.Name += HpkStrings.GetIfrLogicString((HiiIfrOpCode)elem.Childs[0]);
                            for (int i = 1; i < elem.Childs.Count; i++) // skip first element, because it contains the (nested) logic
                                ParsePackageIfr(elem.Childs[i], branch);
                            bProcessChilds = false;
                        }
                    }
                    break;
                #endregion

                #region Visuals
                case EFI_IFR_OPCODE_e.EFI_IFR_SUBTITLE_OP:
                    {
                        EFI_IFR_SUBTITLE ifr_hdr = (EFI_IFR_SUBTITLE)hpkelem.Header;
                        branch.Name = "Subtitle" + " \"" + HpkStrings.GetString(ifr_hdr.Statement.Prompt, hpkelem.UniqueID) + "\"";
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_TEXT_OP:
                    {
                        EFI_IFR_TEXT ifr_hdr = (EFI_IFR_TEXT)hpkelem.Header;
                        branch.Name = "Text"
                            + " \"" + HpkStrings.GetString(ifr_hdr.Statement.Prompt, hpkelem.UniqueID) + "\""
                            + " , \"" + HpkStrings.GetString(ifr_hdr.TextTwo, hpkelem.UniqueID) + "\""
                            + " , Help = \"" + HpkStrings.GetString(ifr_hdr.Statement.Help, hpkelem.UniqueID) + "\"";
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_IMAGE_OP: branch.Name = "Image Id = " + ((EFI_IFR_IMAGE)hpkelem.Header).Id; break;
                case EFI_IFR_OPCODE_e.EFI_IFR_ONE_OF_OP:
                    {
                        EFI_IFR_ONE_OF ifr_hdr = (EFI_IFR_ONE_OF)hpkelem.Header;
                        branch.Name = "OneOf Flags = 0x" + ifr_hdr.Flags.ToString("X2");
                        switch (ifr_hdr.Flags_DataSize)
                        {
                            case EFI_IFR_NUMERIC_SIZE_e.EFI_IFR_NUMERIC_SIZE_1:
                                {
                                    EFI_IFR_NUMERIC_MINMAXSTEP_DATA_8 data = (EFI_IFR_NUMERIC_MINMAXSTEP_DATA_8)hpkelem.Payload;
                                    branch.Name += ", Min = " + data.MinValue.ToString() + ", Max = " + data.MaxValue.ToString() + ", Step = " + data.Step.ToString();
                                }
                                break;
                            case EFI_IFR_NUMERIC_SIZE_e.EFI_IFR_NUMERIC_SIZE_2:
                                {
                                    EFI_IFR_NUMERIC_MINMAXSTEP_DATA_16 data = (EFI_IFR_NUMERIC_MINMAXSTEP_DATA_16)hpkelem.Payload;
                                    branch.Name += ", Min = " + data.MinValue.ToString() + ", Max = " + data.MaxValue.ToString() + ", Step = " + data.Step.ToString();
                                }
                                break;
                            case EFI_IFR_NUMERIC_SIZE_e.EFI_IFR_NUMERIC_SIZE_4:
                                {
                                    EFI_IFR_NUMERIC_MINMAXSTEP_DATA_32 data = (EFI_IFR_NUMERIC_MINMAXSTEP_DATA_32)hpkelem.Payload;
                                    branch.Name += ", Min = " + data.MinValue.ToString() + ", Max = " + data.MaxValue.ToString() + ", Step = " + data.Step.ToString();
                                }
                                break;
                            case EFI_IFR_NUMERIC_SIZE_e.EFI_IFR_NUMERIC_SIZE_8:
                                {
                                    EFI_IFR_NUMERIC_MINMAXSTEP_DATA_64 data = (EFI_IFR_NUMERIC_MINMAXSTEP_DATA_64)hpkelem.Payload;
                                    branch.Name += ", Min = " + data.MinValue.ToString() + ", Max = " + data.MaxValue.ToString() + ", Step = " + data.Step.ToString();
                                }
                                break;
                            default:
                                branch.Name += ", Min = ?, Max = ?, Step = ?";
                                CreateLogEntryParser(LogSeverity.WARNING, "Unknown numeric type [" + hpkelem.UniqueID + "]!");
                                break;
                        }
                        branch.Childs.Add(new ParsedHpkNode(hpkelem, "OneOf-Question " + GetIfrQuestionInfoString(ifr_hdr.Question, hpkelem.UniqueID)));
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_CHECKBOX_OP:
                    {
                        EFI_IFR_CHECKBOX ifr_hdr = (EFI_IFR_CHECKBOX)hpkelem.Header;
                        branch.Name = "Checkbox Flags = " + ifr_hdr.Flags.ToString();
                        branch.Childs.Add(new ParsedHpkNode(hpkelem, "Checkbox-Question " + GetIfrQuestionInfoString(ifr_hdr.Question, hpkelem.UniqueID)));
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_NUMERIC_OP:
                    {
                        EFI_IFR_NUMERIC ifr_hdr = (EFI_IFR_NUMERIC)hpkelem.Header;
                        branch.Name = "Numeric Flags = 0x" + ifr_hdr.Flags.ToString("X2");
                        switch (ifr_hdr.Flags_DataSize)
                        {
                            case EFI_IFR_NUMERIC_SIZE_e.EFI_IFR_NUMERIC_SIZE_1:
                                {
                                    EFI_IFR_NUMERIC_MINMAXSTEP_DATA_8 data = (EFI_IFR_NUMERIC_MINMAXSTEP_DATA_8)hpkelem.Payload;
                                    branch.Name += ", Min = " + data.MinValue.ToString() + ", Max = " + data.MaxValue.ToString() + ", Step = " + data.Step.ToString();
                                }
                                break;
                            case EFI_IFR_NUMERIC_SIZE_e.EFI_IFR_NUMERIC_SIZE_2:
                                {
                                    EFI_IFR_NUMERIC_MINMAXSTEP_DATA_16 data = (EFI_IFR_NUMERIC_MINMAXSTEP_DATA_16)hpkelem.Payload;
                                    branch.Name += ", Min = " + data.MinValue.ToString() + ", Max = " + data.MaxValue.ToString() + ", Step = " + data.Step.ToString();
                                }
                                break;
                            case EFI_IFR_NUMERIC_SIZE_e.EFI_IFR_NUMERIC_SIZE_4:
                                {
                                    EFI_IFR_NUMERIC_MINMAXSTEP_DATA_32 data = (EFI_IFR_NUMERIC_MINMAXSTEP_DATA_32)hpkelem.Payload;
                                    branch.Name += ", Min = " + data.MinValue.ToString() + ", Max = " + data.MaxValue.ToString() + ", Step = " + data.Step.ToString();
                                }
                                break;
                            case EFI_IFR_NUMERIC_SIZE_e.EFI_IFR_NUMERIC_SIZE_8:
                                {
                                    EFI_IFR_NUMERIC_MINMAXSTEP_DATA_64 data = (EFI_IFR_NUMERIC_MINMAXSTEP_DATA_64)hpkelem.Payload;
                                    branch.Name += ", Min = " + data.MinValue.ToString() + ", Max = " + data.MaxValue.ToString() + ", Step = " + data.Step.ToString();
                                }
                                break;
                            default:
                                branch.Name += ", Min = ?, Max = ?, Step = ?";
                                CreateLogEntryParser(LogSeverity.WARNING, "Unknown numeric type [" + hpkelem.UniqueID + "]!");
                                break;
                        }
                        branch.Childs.Add(new ParsedHpkNode(hpkelem, "Numeric-Question " + GetIfrQuestionInfoString(ifr_hdr.Question, hpkelem.UniqueID)));
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_PASSWORD_OP:
                    {
                        EFI_IFR_PASSWORD ifr_hdr = (EFI_IFR_PASSWORD)hpkelem.Header;
                        branch.Name = "Password "
                            + "Min = " + ifr_hdr.MinSize.ToString()
                            + ", Max = " + ifr_hdr.MaxSize.ToString();
                        branch.Childs.Add(new ParsedHpkNode(hpkelem, "Password-Question " + GetIfrQuestionInfoString(ifr_hdr.Question, hpkelem.UniqueID)));
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_ONE_OF_OPTION_OP:
                    {
                        EFI_IFR_ONE_OF_OPTION ifr_hdr = (EFI_IFR_ONE_OF_OPTION)hpkelem.Header;
                        object dummy = null;
                        branch.Name = "OneOf Option = " + ifr_hdr.Option.ToDecimalString(5) + " [\"" + HpkStrings.GetString(ifr_hdr.Option, hpkelem.UniqueID) + "\"]"
                            + ", Flags = " + ifr_hdr.Flags.ToString()
                            + ", " + HpkStrings.GetValueString(ifr_hdr.Type, hpkelem.Payload, hpkelem.UniqueID, ref dummy);

                        // Parse nested value logic..
                        if (ifr_hdr.Type == EFI_IFR_TYPE_e.EFI_IFR_TYPE_OTHER)
                        {
                            if (elem.Childs.Count < 2)
                                CreateLogEntryParser(LogSeverity.WARNING, "Too few value opcodes [" + hpkelem.UniqueID + "]!");
                            if (2 < elem.Childs.Count)
                                CreateLogEntryParser(LogSeverity.WARNING, "Too many value opcodes [" + hpkelem.UniqueID + "]!");
                            else
                            {
                                // Child index: 0 = Value opcode, 1 = END opcode
                                ParsedHpkNode leaf = new ParsedHpkNode(elem.Childs[0], "Nested value = " + HpkStrings.GetIfrLogicString((HiiIfrOpCode)elem.Childs[0]));
                                branch.Childs.Add(leaf);
                                bProcessChilds = false;
                            }
                        }
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_LOCKED_OP: branch.Name = "Locked"; break;
                case EFI_IFR_OPCODE_e.EFI_IFR_ACTION_OP:
                    {
                        branch.Name = "ActionButton";
                        switch (hpkelem.Header.GetType().Name)
                        {
                            case "EFI_IFR_ACTION":
                                {
                                    EFI_IFR_ACTION ifr_hdr = (EFI_IFR_ACTION)hpkelem.Header;
                                    if (ifr_hdr.QuestionConfig != 0)
                                        branch.Name += " Config = \"" + HpkStrings.GetString(ifr_hdr.QuestionConfig, hpkelem.UniqueID) + "\" ";
                                    branch.Childs.Add(new ParsedHpkNode(hpkelem, "ActionButton-Question " + GetIfrQuestionInfoString(ifr_hdr.Question, hpkelem.UniqueID)));
                                }
                                break;
                            case "EFI_IFR_ACTION_1":
                                {
                                    EFI_IFR_ACTION_1 ifr_hdr = (EFI_IFR_ACTION_1)hpkelem.Header;
                                    branch.Childs.Add(new ParsedHpkNode(hpkelem, "ActionButton-Question " + GetIfrQuestionInfoString(ifr_hdr.Question, hpkelem.UniqueID)));
                                }
                                break;
                            default:
                                branch.Name += "UNKNOWNTYPE";
                                CreateLogEntryParser(LogSeverity.WARNING, "Unknown action type [" + hpkelem.UniqueID + "]!");
                                break;
                        }
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_RESET_BUTTON_OP:
                    {
                        EFI_IFR_RESET_BUTTON ifr_hdr = (EFI_IFR_RESET_BUTTON)hpkelem.Header;
                        branch.Name = "ResetButton "
                            + ", Prompt = " + ifr_hdr.Statement.Prompt.ToDecimalString(5) + " [\"" + HpkStrings.GetString(ifr_hdr.Statement.Prompt, hpkelem.UniqueID) + "\"]"
                            + ", Help = " + ifr_hdr.Statement.Help.ToDecimalString(5) + " [\"" + HpkStrings.GetString(ifr_hdr.Statement.Help, hpkelem.UniqueID) + "\"]"
                            + ", DefaultId = " + ifr_hdr.DefaultId.ToDecimalString(5);
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_DATE_OP:
                    {
                        EFI_IFR_DATE ifr_hdr = (EFI_IFR_DATE)hpkelem.Header;
                        branch.Name = "Date Flags = 0x" + ifr_hdr.Flags.ToString("X2");
                        branch.Childs.Add(new ParsedHpkNode(hpkelem, "Date-Question " + GetIfrQuestionInfoString(ifr_hdr.Question, hpkelem.UniqueID)));
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_TIME_OP:
                    {
                        EFI_IFR_TIME ifr_hdr = (EFI_IFR_TIME)hpkelem.Header;
                        branch.Name = "Time Flags = 0x" + ifr_hdr.Flags.ToString("X2");
                        branch.Childs.Add(new ParsedHpkNode(hpkelem, "Time-Question " + GetIfrQuestionInfoString(ifr_hdr.Question, hpkelem.UniqueID)));
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_STRING_OP:
                    {
                        EFI_IFR_STRING ifr_hdr = (EFI_IFR_STRING)hpkelem.Header;
                        branch.Name = "String "
                            + "Min = " + ifr_hdr.MinSize.ToString()
                            + ", Max = " + ifr_hdr.MaxSize.ToString()
                            + ", Flags = " + ifr_hdr.Flags.ToString();
                        branch.Childs.Add(new ParsedHpkNode(hpkelem, "String-Question " + GetIfrQuestionInfoString(ifr_hdr.Question, hpkelem.UniqueID)));
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_REFRESH_OP: branch.Name = "Refresh Interval = " + ((EFI_IFR_REFRESH)hpkelem.Header).RefreshInterval.ToString(); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_ANIMATION_OP: branch.Name = "Animation Id = " + ((EFI_IFR_ANIMATION)hpkelem.Header).Id.ToString(); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_ORDERED_LIST_OP:
                    {
                        EFI_IFR_ORDERED_LIST ifr_hdr = (EFI_IFR_ORDERED_LIST)hpkelem.Header;
                        branch.Name = "OrderedList "
                            + "MaxContainers = " + ifr_hdr.MaxContainers.ToString()
                            + ", Flags = " + ifr_hdr.Flags.ToString();
                        branch.Childs.Add(new ParsedHpkNode(hpkelem, "OrderedList-Question " + GetIfrQuestionInfoString(ifr_hdr.Question, hpkelem.UniqueID)));
                    }
                    break;
                //EFI_IFR_READ_OP // Unclear what it does, therefore no implementation by now. If you know it, let me know ;)
                //EFI_IFR_WRITE_OP, // Unclear what it does, therefore no implementation by now. If you know it, let me know ;)
                #endregion

                #region Values
                case EFI_IFR_OPCODE_e.EFI_IFR_UINT8_OP: branch.Name = "UINT8 Value = 0x" + ((EFI_IFR_UINT8)hpkelem.Header).Value.ToString("X2"); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_UINT16_OP: branch.Name = "UINT16 Value = 0x" + ((EFI_IFR_UINT16)hpkelem.Header).Value.ToString("X4"); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_UINT32_OP: branch.Name = "UINT32 Value = 0x" + ((EFI_IFR_UINT32)hpkelem.Header).Value.ToString("X8"); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_UINT64_OP: branch.Name = "UINT64 Value = 0x" + ((EFI_IFR_UINT64)hpkelem.Header).Value.ToString("X16"); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_VALUE_OP: branch.Name = "Value"; break;
                case EFI_IFR_OPCODE_e.EFI_IFR_DEFAULT_OP:
                    {
                        EFI_IFR_DEFAULT ifr_hdr = (EFI_IFR_DEFAULT)hpkelem.Header;
                        object dummy = null;
                        branch.Name = "Default Id = " + ifr_hdr.DefaultId.ToDecimalString(5)
                            + ", " + HpkStrings.GetValueString(ifr_hdr.Type, hpkelem.Payload, hpkelem.UniqueID, ref dummy);

                        // Parse nested value logic..
                        if (ifr_hdr.Type == EFI_IFR_TYPE_e.EFI_IFR_TYPE_OTHER)
                        {
                            if (elem.Childs.Count < 2)
                                CreateLogEntryParser(LogSeverity.WARNING, "Too few value opcodes [" + hpkelem.UniqueID + "]!");
                            if (2 < elem.Childs.Count)
                                CreateLogEntryParser(LogSeverity.WARNING, "Too many value opcodes [" + hpkelem.UniqueID + "]!");
                            else
                            {
                                // Child index: 0 = Value opcode, 1 = END opcode
                                ParsedHpkNode leaf = new ParsedHpkNode(elem.Childs[0], "Nested value = " + HpkStrings.GetIfrLogicString((HiiIfrOpCode)elem.Childs[0]));
                                branch.Childs.Add(leaf);
                                bProcessChilds = false;
                            }
                        }
                    }
                    break;
                #endregion

                case EFI_IFR_OPCODE_e.EFI_IFR_END_OP: return; // Skip
                default: break; // simply add all others 1:1 when no specific handler exists
            }

            if (bProcessChilds)
                foreach (HiiIfrOpCode child in elem.Childs)
                    ParsePackageIfr(child, branch);

            root.Childs.Add(branch);
        }

        /// <summary>
        /// Builds humand readable string of an IFR question header
        /// </summary>
        /// <param name="Question">Input IFR question header</param>
        /// <param name="UniqueID">ID of the requesting HPK element (for reference on errors)</param>
        /// <returns>Humand readable string</returns>
        private string GetIfrQuestionInfoString(EFI_IFR_QUESTION_HEADER Question, int UniqueID)
        {
            return "Id = " + Question.QuestionId.ToDecimalString(5)
                + ", Prompt = " + Question.Header.Prompt.ToDecimalString(5) + " [\"" + HpkStrings.GetString(Question.Header.Prompt, UniqueID) + "\"]"
                + ", Help = " + Question.Header.Help.ToDecimalString(5) + " [\"" + HpkStrings.GetString(Question.Header.Help, UniqueID) + "\"]"
                + ", Flags = " + Question.Flags
                + ", VarId = " + Question.VarStoreId.ToDecimalString(5)
                + ", Offset/Name = " + Question.VarStoreInfo.VarOffset.ToDecimalString(5);
        }
    }
}
