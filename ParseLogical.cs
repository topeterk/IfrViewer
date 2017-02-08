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
                                CreateLogEntryParser(LogSeverity.WARNING, "Unknown reference type [" + hpkelem.UniqueID + "]!");
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
                            branch.Name += GetIfrLogicString((HiiIfrOpCode)elem.Childs[0], StringDB);
                            for (int i = 1; i < elem.Childs.Count; i++) // skip first element, because it contains the (nested) logic
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

        #region Logic stack
        private class IfrLogicStack
        {
            public List<string> Elements;
            public IfrLogicStack() { Elements = new List<string>(); }
        }
        private static void Push(this IfrLogicStack Stack, string LogicStr)
        {
            Stack.Elements.Add(LogicStr);
        }
        private static string Pop(this IfrLogicStack Stack)
        {
            if (Stack.Elements.Count == 0)
            {
                CreateLogEntryParser(LogSeverity.WARNING, "Logic stack empty!");
                return "EMPTYSTACK";
            }
            string LogicStr = Stack.Elements[Stack.Elements.Count - 1];
            Stack.Elements.RemoveAt(Stack.Elements.Count - 1);
            return LogicStr;
        }
        private static void Op12(this IfrLogicStack Stack, string Operation)
        {
            string expr1 = Stack.Pop();
            string expr2 = Stack.Pop();
            Stack.Push("(" + expr1 + " " + Operation + " " + expr2 + ")");
        }
        private static void Op21(this IfrLogicStack Stack, string Operation)
        {
            string expr1 = Stack.Pop();
            string expr2 = Stack.Pop();
            Stack.Push("(" + expr2 + " " + Operation + " " + expr1 + ")");
        }
        #endregion

        private static string GetIfrLogicString(HiiIfrOpCode ifrelem, List<StringDataBase> StringDB)
        {
            IfrLogicStack Stack = new IfrLogicStack();
            string LogicString;

            ParseIfrLogicStack(ifrelem, Stack, StringDB);

            if (Stack.Elements.Count != 1)
            {
                string StackElements = "";
                foreach (string element in Stack.Elements) StackElements += Environment.NewLine + "   " + element;
                CreateLogEntryParser(LogSeverity.WARNING, "Logic stack has " + Stack.Elements.Count + " elements but must have 1 only [" + ifrelem.UniqueID + "]!" + StackElements);

                StackElements = "INVALIDSTACK(";
                foreach (string element in Stack.Elements) StackElements += element + ",";
                LogicString = StackElements + ")";
            }
            else LogicString = Stack.Elements[0];

            return LogicString;
        }
        
        private static void ParseIfrLogicStack(HiiIfrOpCode ifrelem, IfrLogicStack Stack, List<StringDataBase> StringDB)
        {
            switch (ifrelem.OpCode)
            {
                #region Logic OpCodes
                // -- built-in-function
                // EFI_IFR_DUP
                case EFI_IFR_OPCODE_e.EFI_IFR_EQ_ID_VAL_OP:
                    {
                        EFI_IFR_EQ_ID_VAL ifr_hdr = (EFI_IFR_EQ_ID_VAL)ifrelem.Header;
                        Stack.Push("(ValueOfId(" + ifr_hdr.QuestionId.ToString("00000") + ") == " + ifr_hdr.Value.ToString() + ")");
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_EQ_ID_ID_OP:
                    {
                        EFI_IFR_EQ_ID_ID ifr_hdr = (EFI_IFR_EQ_ID_ID)ifrelem.Header;
                        Stack.Push("(ValueOfId(" + ifr_hdr.QuestionId1.ToString("00000") + ") == ValueOfId(" + ifr_hdr.QuestionId2.ToString("00000") + "))");
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_EQ_ID_VAL_LIST_OP:
                    {
                        string ValueListStr = "";
                        foreach (IfrTypeUINT16 value in (List<IfrTypeUINT16>)ifrelem.Payload) ValueListStr += value.u16.ToString() + ",";
                        Stack.Push("(ValueOfId(" + ((EFI_IFR_EQ_ID_VAL_LIST)ifrelem.Header).QuestionId.ToString("00000") + ") is in [" + ValueListStr + "])");
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_GET_OP:
                    {
                        EFI_IFR_GET ifr_hdr = (EFI_IFR_GET)ifrelem.Header;
                        Stack.Push("ValueFromVar(Id = " + ifr_hdr.VarStoreId.ToString("00000")
                            + ", Offset/Name = " + ifr_hdr.VarStoreInfo.VarOffset.ToString("00000")
                            + ", Type = " + ifr_hdr.VarStoreType.ToString() + ")");
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_QUESTION_REF1_OP: Stack.Push("ValueOfId(" + ((EFI_IFR_QUESTION_REF1)ifrelem.Header).QuestionId.ToString("00000") + ")"); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_QUESTION_REF3_OP:
                    {
                        switch (ifrelem.Header.GetType().Name)
                        {
                            case "EFI_IFR_QUESTION_REF3": Stack.Push("ValueOfId(" + Stack.Pop() + ")"); break;
                            case "EFI_IFR_QUESTION_REF3_2":
                                {
                                    EFI_IFR_QUESTION_REF3_2 ifr_hdr = (EFI_IFR_QUESTION_REF3_2)ifrelem.Header;
                                    Stack.Push("ValueOfId(" + Stack.Pop() + (ifr_hdr.DevicePath == 0 ? "" : ", DevicePath = \"" + GetStringOfPackages(StringDB, ifr_hdr.DevicePath, ifrelem.UniqueID) + "\"") + ")");
                                }
                                break;
                            case "EFI_IFR_QUESTION_REF3_3":
                                {
                                    EFI_IFR_QUESTION_REF3_3 ifr_hdr = (EFI_IFR_QUESTION_REF3_3)ifrelem.Header;
                                    Stack.Push("ValueOfId(" + Stack.Pop() + ", Guid = " + ifr_hdr.Guid.Guid.ToString() + (ifr_hdr.DevicePath == 0 ? "" : ", DevicePath = \"" + GetStringOfPackages(StringDB, ifr_hdr.DevicePath, ifrelem.UniqueID) + "\"") + ")");
                                }
                                break;
                            default:
                                Stack.Push("UNKNWONOPCODE(" + ifrelem.OpCode.ToString() + ")");
                                CreateLogEntryParser(LogSeverity.UNIMPLEMENTED, "Logic reference type " + ifrelem.OpCode.ToString() + " [" + ifrelem.UniqueID + "]!");
                                break;
                        }
                    }
                    break;
// EFI_IFR_RULE_REF
                case EFI_IFR_OPCODE_e.EFI_IFR_STRING_REF1_OP: Stack.Push("\"" + GetStringOfPackages(StringDB, ((EFI_IFR_STRING_REF1)ifrelem.Header).StringId, ifrelem.UniqueID) + "\""); break;
/* EFI_IFR_THIS |
EFI_IFR_SECURITY */
                // -- constant
                case EFI_IFR_OPCODE_e.EFI_IFR_FALSE_OP: Stack.Push("FALSE"); break;
/* EFI_IFR_ONE |
EFI_IFR_ONES */
                case EFI_IFR_OPCODE_e.EFI_IFR_TRUE_OP: Stack.Push("TRUE"); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_UINT8_OP: Stack.Push(((EFI_IFR_UINT8)ifrelem.Header).Value.ToString()); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_UINT16_OP: Stack.Push(((EFI_IFR_UINT16)ifrelem.Header).Value.ToString()); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_UINT32_OP: Stack.Push(((EFI_IFR_UINT32)ifrelem.Header).Value.ToString()); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_UINT64_OP: Stack.Push(((EFI_IFR_UINT64)ifrelem.Header).Value.ToString()); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_UNDEFINED_OP: Stack.Push("Undefined"); break;
// EFI_IFR_VERSION
                case EFI_IFR_OPCODE_e.EFI_IFR_ZERO_OP: Stack.Push("0"); break;
                // -- binary-op
                case EFI_IFR_OPCODE_e.EFI_IFR_ADD_OP: Stack.Op21("+"); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_AND_OP: Stack.Op12("&&"); break;
/* EFI_IFR_BITWISE_AND |
EFI_IFR_BITWISE_OR |
EFI_IFR_CATENATE |
EFI_IFR_DIVIDE */
                case EFI_IFR_OPCODE_e.EFI_IFR_EQUAL_OP: Stack.Op12("=="); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_GREATER_EQUAL_OP: Stack.Op12(">="); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_GREATER_THAN_OP: Stack.Op12(">"); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_LESS_EQUAL_OP: Stack.Op12("<="); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_LESS_THAN_OP: Stack.Op12("<"); break;
/*EFI_IFR_MATCH |
EFI_IFR_MATCH2 |
EFI_IFR_MODULO |
EFI_IFR_MULTIPLY */
                case EFI_IFR_OPCODE_e.EFI_IFR_NOT_EQUAL_OP: Stack.Op12("!="); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_OR_OP: Stack.Op12("||"); break;
/* EFI_IFR_SHIFT_LEFT |
EFI_IFR_SHIFT_RIGHT */
                case EFI_IFR_OPCODE_e.EFI_IFR_SUBTRACT_OP: Stack.Op21("-"); break;
                // -- unary-op
// EFI_IFR_LENGTH
                case EFI_IFR_OPCODE_e.EFI_IFR_NOT_OP: Stack.Push("!" + Stack.Pop()); break;
// EFI_IFR_BITWISE_NOT
                case EFI_IFR_OPCODE_e.EFI_IFR_QUESTION_REF2_OP: Stack.Push("ValueOfId(" + Stack.Pop() + ")"); break;
/* EFI_IFR_SET |
EFI_IFR_STRING_REF2 |
EFI_IFR_TO_BOOLEAN |
EFI_IFR_TO_STRING |
EFI_IFR_TO_UINT |
EFI_IFR_TO_UPPER |
EFI_IFR_TO_LOWER */
                // -- ternary-op
/*EFI_IFR_CONDITIONAL |
EFI_IFR_FIND |
EFI_IFR_MID |
EFI_IFR_TOKEN |
EFI_IFR_SPAN */
                #endregion
                case EFI_IFR_OPCODE_e.EFI_IFR_END_OP: return;
                default:
                    Stack.Push("UNKNWONOPCODE(" + ifrelem.OpCode.ToString() + ")");
                    CreateLogEntryParser(LogSeverity.UNIMPLEMENTED, "Logic OpCode " + ifrelem.OpCode.ToString() + " [" + ifrelem.UniqueID + "]!");
                    break;
            }

            if (ifrelem.HasOwnScope)
            {
                foreach (HiiIfrOpCode child in ifrelem.Childs)
                    ParseIfrLogicStack(child, Stack, StringDB);
            }
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
