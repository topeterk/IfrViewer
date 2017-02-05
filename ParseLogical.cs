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
using System.Windows.Forms;
using static IFR.IFRHelper;

namespace IfrViewer
{
    static class HpkParser
    {
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

        private struct StringDataBase
        {
            public string Language;
            public List<KeyValuePair<UInt16, string>> Strings;
        }

        public static List<ParsedHpkNode> ParseHpkPackages(List<HiiPackageBase> Packages)
        {
            List<ParsedHpkNode> result = new List<ParsedHpkNode>();
            List<StringDataBase> StringDB = new List<StringDataBase>();

            foreach (HiiPackageBase pkg in Packages)
            {
                ParsedHpkNode root = new ParsedHpkNode(pkg, pkg.Name);

                try
                {
                    switch (pkg.PackageType)
                    {
                        case EFI_HII_PACKAGE_e.EFI_HII_PACKAGE_FORMS:
                            root.Name = "Form Package";
                            result.Add(root);
                            foreach (HPKElement child in root.Origin.Childs)
                                ParsePackageIfr(child, root, Packages, StringDB);

                            // clean up..
                            StringDB = new List<StringDataBase>();
                            break;
                        case EFI_HII_PACKAGE_e.EFI_HII_PACKAGE_STRINGS:
                            UInt16 StringID = 0; // First ID is 1 (will be increased later)
                            StringDataBase db;
                            db.Language = ((HiiPackageString.Payload_t)pkg.Payload).Language;
                            db.Strings = new List<KeyValuePair<UInt16, string>>();
                            StringDB.Add(db);

                            root.Name = "Strings Package \"" + db.Language + "\"";
                            result.Add(root);
                            foreach (HPKElement child in root.Origin.Childs)
                                ParsePackageSibt(child, root, Packages, ref StringID, db);
                            break;
                        default:
                            CreateLogEntryParser(LogSeverity.UNIMPLEMENTED, root.Name);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    CreateLogEntryParser(LogSeverity.ERROR, "Parsing failed!" + Environment.NewLine + ex.ToString());
                    MessageBox.Show("Parsing failed!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            return result;
        }

        private static void ParsePackageIfr(HPKElement hpkelem, ParsedHpkNode root, List<HiiPackageBase> Packages, List<StringDataBase> StringDB)
        {
            ParsedHpkNode branch = new ParsedHpkNode(hpkelem, hpkelem.Name);
            HiiIfrOpCode elem = (HiiIfrOpCode)hpkelem;
            bool bProcessChilds = true;

            switch (elem.OpCode)
            {
                #region Forms
                case EFI_IFR_OPCODE_e.EFI_IFR_FORM_SET_OP:
                    EFI_IFR_FORM_SET hdr = (EFI_IFR_FORM_SET)elem.Header;
                    ParsedHpkNode leaf = new ParsedHpkNode(hpkelem, "");
                    string prefix = "FormSet";
                    branch.Name = prefix + " \"" + GetStringOfPackages(StringDB, hdr.FormSetTitle, hpkelem.UniqueID) + "\"";
                    leaf.Name = prefix + "-Help = " + GetStringOfPackages(StringDB, hdr.Help, hpkelem.UniqueID);
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
                                ParsePackageIfr(child, varstores, Packages, StringDB); break;
                            default:
                                ParsePackageIfr(child, branch, Packages, StringDB); break;
                        }
                    }
                    bProcessChilds = false;
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_FORM_OP:
                    {
                        EFI_IFR_FORM ifr_hdr = (EFI_IFR_FORM)hpkelem.Header;
                        branch.Name = "Form"
                            + " [Id = " + ifr_hdr.FormId.ToString("00000") + "]"
                            + " \"" + GetStringOfPackages(StringDB, ifr_hdr.FormTitle, hpkelem.UniqueID) + "\"";
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_REF_OP:
                    {
                        string FormIdString = "FormId = ";
                        string FormSetIdString = "FormSetId = ";
                        string InfoString = "";
                        string RefName = null;
                        switch (hpkelem.Header.GetType().Name)
                        {
                            case "EFI_IFR_REF":
                                {
                                    EFI_IFR_REF ifr_hdr = (EFI_IFR_REF)hpkelem.Header;
                                    FormIdString += ifr_hdr.FormId == 0 ? "Current" : ifr_hdr.FormId.ToString("00000");
                                    InfoString = FormIdString;
                                    RefName = GetStringOfPackages(StringDB, ifr_hdr.Question.Header.Prompt, hpkelem.UniqueID);
                                }
                                break;
                            case "EFI_IFR_REF2":
                                {
                                    EFI_IFR_REF2 ifr_hdr = (EFI_IFR_REF2)hpkelem.Header;
                                    FormIdString += ifr_hdr.FormId == 0 ? "Current" : ifr_hdr.FormId.ToString("00000");
                                    InfoString = FormIdString;
                                    RefName = GetStringOfPackages(StringDB, ifr_hdr.Question.Header.Prompt, hpkelem.UniqueID);
                                }
                                break;
                            case "EFI_IFR_REF3":
                                {
                                    EFI_IFR_REF3 ifr_hdr = (EFI_IFR_REF3)hpkelem.Header;
                                    FormIdString += ifr_hdr.FormId == 0 ? "Current" : ifr_hdr.FormId.ToString("00000");
                                    FormSetIdString += ifr_hdr.FormSetId.Guid.ToString();
                                    InfoString = FormIdString + ", " + FormSetIdString;
                                    RefName = GetStringOfPackages(StringDB, ifr_hdr.Question.Header.Prompt, hpkelem.UniqueID);
                                }
                                break;
                            case "EFI_IFR_REF4":
                                {
                                    EFI_IFR_REF4 ifr_hdr = (EFI_IFR_REF4)hpkelem.Header;
                                    FormIdString += ifr_hdr.FormId == 0 ? "Current" : ifr_hdr.FormId.ToString("00000");
                                    FormSetIdString += ifr_hdr.FormSetId.Guid.ToString();
                                    InfoString = FormIdString + ", " + FormSetIdString + ", DevicePath = \"" + GetStringOfPackages(StringDB, ifr_hdr.DevicePath, hpkelem.UniqueID) + "\"";
                                    RefName = GetStringOfPackages(StringDB, ifr_hdr.Question.Header.Prompt, hpkelem.UniqueID);
                                }
                                break;
                            case "EFI_IFR_REF5":
                                {
                                    EFI_IFR_REF5 ifr_hdr = (EFI_IFR_REF5)hpkelem.Header;
                                    FormIdString += "Nested";
                                    InfoString = FormIdString;
                                    FormSetIdString = null;
                                }
                                break;
                            default:
                                InfoString = "?";
                                CreateLogEntryParser(LogSeverity.WARNING, "Unknown Ref type [" + hpkelem.UniqueID + "]!");
                                break;
                        }
                        branch.Name = "Reference to [" + InfoString + "]" + (RefName != null ? " \"" + RefName + "\"" : "");
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_GUID_OP: branch.Name = "GuidOp = " + ((EFI_IFR_GUID)hpkelem.Header).Guid.Guid.ToString(); break;
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
                            + " [Id = " + ifr_hdr.VarStoreId.ToString("00000") + ", Guid = " + ifr_hdr.Guid.Guid.ToString() + "]"
                            + " \"" + ((HiiIfrOpCodeWithAsciiNullTerminatedString<EFI_IFR_VARSTORE>.NamedPayload_t)hpkelem.Payload).Name + "\"";
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_VARSTORE_EFI_OP:
                    {
                        EFI_IFR_VARSTORE_EFI ifr_hdr = (EFI_IFR_VARSTORE_EFI)hpkelem.Header;
                        branch.Name = "VarStore"
                            + " [Id = " + ifr_hdr.VarStoreId.ToString("00000") + ", Guid = " + ifr_hdr.Guid.Guid.ToString() + "]"
                            + " \"" + ((HiiIfrOpCodeWithAsciiNullTerminatedString<EFI_IFR_VARSTORE_EFI>.NamedPayload_t)hpkelem.Payload).Name + "\"";
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_VARSTORE_NAME_VALUE_OP:
                    {
                        EFI_IFR_VARSTORE_NAME_VALUE ifr_hdr = (EFI_IFR_VARSTORE_NAME_VALUE)hpkelem.Header;
                        branch.Name = "VarStore" + " [Id = " + ifr_hdr.VarStoreId.ToString("00000") + ", Guid = " + ifr_hdr.Guid.Guid.ToString() + "]";
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_VARSTORE_DEVICE_OP:
                    {
                        EFI_IFR_VARSTORE_DEVICE ifr_hdr = (EFI_IFR_VARSTORE_DEVICE)hpkelem.Header;
                        branch.Name = "VarStore" + " \"" + GetStringOfPackages(StringDB, ifr_hdr.DevicePath, hpkelem.UniqueID) + "\"";
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
                            branch.Name += GetIfrLogicString((HiiIfrOpCode)elem.Childs[0]);
#warning "As long as Logic String is not complete, keep logic entries in tree"
for (int i = 0; i < elem.Childs.Count; i++) // skip first element, because it contains the (nested) logic
                            //for (int i = 1; i < elem.Childs.Count; i++) // skip first element, because it contains the (nested) logic
                                ParsePackageIfr(elem.Childs[i], branch, Packages, StringDB);
                            bProcessChilds = false;
                        }
                    }
                    break;
                #endregion
                #region Visuals
                case EFI_IFR_OPCODE_e.EFI_IFR_SUBTITLE_OP:
                    {
                        EFI_IFR_SUBTITLE ifr_hdr = (EFI_IFR_SUBTITLE)hpkelem.Header;
                        branch.Name = "Subtitle" + " \"" + GetStringOfPackages(StringDB, ifr_hdr.Statement.Prompt, hpkelem.UniqueID) + "\"";
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_TEXT_OP:
                    {
                        EFI_IFR_TEXT ifr_hdr = (EFI_IFR_TEXT)hpkelem.Header;
                        branch.Name = "Text"
                            + " \"" + GetStringOfPackages(StringDB, ifr_hdr.Statement.Prompt, hpkelem.UniqueID) + "\""
                            + " , \"" + GetStringOfPackages(StringDB, ifr_hdr.TextTwo, hpkelem.UniqueID) + "\""
                            + " , Help = \"" + GetStringOfPackages(StringDB, ifr_hdr.Statement.Help, hpkelem.UniqueID) + "\"";
                    }
                    break;
                #endregion
                case EFI_IFR_OPCODE_e.EFI_IFR_END_OP: return; // Skip
                default: break; // simply add all others 1:1 when no specific handler exists
            }

            if (bProcessChilds)
                foreach (HiiIfrOpCode child in elem.Childs)
                    ParsePackageIfr(child, branch, Packages, StringDB);

            root.Childs.Add(branch);
        }

        private static string GetIfrLogicString(HiiIfrOpCode ifrelem, string NestedResult = "")
        {
            string result = "";

            switch (ifrelem.OpCode)
            {
                case EFI_IFR_OPCODE_e.EFI_IFR_LOCKED_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_AND_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_OR_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_NOT_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_TO_LOWER_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_TO_UPPER_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_MATCH_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_EQUAL_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_NOT_EQUAL_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_GREATER_THAN_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_GREATER_EQUAL_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_LESS_THAN_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_LESS_EQUAL_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_BITWISE_AND_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_BITWISE_OR_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_BITWISE_NOT_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_SHIFT_LEFT_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_SHIFT_RIGHT_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_ADD_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_SUBTRACT_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_MULTIPLY_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_DIVIDE_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_MODULO_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_QUESTION_REF2_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_TRUE_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_FALSE_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_TO_UINT_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_TO_BOOLEAN_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_MID_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_TOKEN_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_STRING_REF2_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_CONDITIONAL_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_ZERO_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_ONE_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_ONES_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_UNDEFINED_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_LENGTH_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_DUP_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_THIS_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_VALUE_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_CATENATE_OP:
                case EFI_IFR_OPCODE_e.EFI_IFR_MODAL_TAG_OP: result = NestedResult + " " + ifrelem.OpCode.ToString(); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_END_OP: return NestedResult;
                default:
                    //CreateLogEntryParser(LogSeverity.UNIMPLEMENTED, "Logic OpCode " + ifrelem.OpCode.ToString() + " [" + ifrelem.UniqueID + "]!");
                    result = NestedResult + " " + ifrelem.OpCode.ToString(); break;
            }

            if (ifrelem.HasOwnScope)
            {
                NestedResult = result;
                foreach (HiiIfrOpCode child in ifrelem.Childs)
                    NestedResult = GetIfrLogicString(child, NestedResult);
                result = "(" + NestedResult + ")";
            }

            return result;
        }

        private static void ParsePackageSibt(HPKElement hpkelem, ParsedHpkNode root, List<HiiPackageBase> Packages, ref UInt16 StringID, StringDataBase db)
        {
            ParsedHpkNode branch = new ParsedHpkNode(hpkelem, hpkelem.Name);
            HiiSibtBlockBase elem = (HiiSibtBlockBase)hpkelem;

            switch (elem.BlockType)
            {
                case EFI_HII_SIBT_e.EFI_HII_SIBT_STRING_UCS2:
                    string StringText = ((HiiSibtBlockStringUcs2.Payload_t)elem.Payload).StringText;
                    StringID++;
                    branch.Name = StringID.ToString("00000") + " " + StringText;
                    db.Strings.Add(new KeyValuePair<ushort, string>(StringID, StringText));
                    break;
                case EFI_HII_SIBT_e.EFI_HII_SIBT_SKIP1: StringID += ((EFI_HII_SIBT_SKIP1_BLOCK)elem.Header).SkipCount; return; // Skip
                case EFI_HII_SIBT_e.EFI_HII_SIBT_SKIP2: StringID += ((EFI_HII_SIBT_SKIP2_BLOCK)elem.Header).SkipCount; return; // Skip
                case EFI_HII_SIBT_e.EFI_HII_SIBT_END: return; // Skip
                default:
                    foreach (HiiSibtBlockBase child in elem.Childs)
                        ParsePackageSibt(child, branch, Packages, ref StringID, db);
                    break; // simply add all others 1:1 when no specific handler exists
            }

            root.Childs.Add(branch);
        }

        private static string GetStringOfPackages(List<StringDataBase> StringDB, UInt16 StringID, int UniqueID, bool bZeroIsEmpty = true, string Language = "en-US")
        {
            if (StringID == 0 && bZeroIsEmpty)
                return "";

            foreach (StringDataBase db in StringDB)
            {
                if (db.Language != Language) continue;

                foreach (KeyValuePair<UInt16, string> entry in db.Strings)
                {
                    if (entry.Key == StringID)
                    {
                        return entry.Value;
                    }
                }
            }

            CreateLogEntryParser(LogSeverity.WARNING, "StringID " + StringID + " could not be translated [" + UniqueID + "]!");
            return "UNKNOWN_STRING_ID(" + StringID + ")";
        }

        private static void CreateLogEntryParser(LogSeverity severity, string msg)
        {
            CreateLogEntry(severity, "Parser", msg);
        }
    }
}
