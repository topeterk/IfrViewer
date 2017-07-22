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
using static IFR.IFRHelper;
using static IfrViewer.HpkParser;

namespace IfrViewer
{
    public static class HpkParser
    {
        /// <summary>
        /// Creates a log entry for the "Parser" module
        /// </summary>
        /// <param name="severity">Severity of message</param>
        /// <param name="msg">Message string</param>
        /// <param name="bShowMsgBox">Shows message box when true</param>
        public static void CreateLogEntryParser(LogSeverity severity, string msg, bool bShowMsgBox = false)
        {
            CreateLogEntry(severity, "Parser", msg, bShowMsgBox);
        }

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
    }

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

    /// <summary>
    /// Parses HPK packages and gives access to the parsed strings
    /// </summary>
    public class ParsedHpkStringContainer
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

        /// <summary>
        /// Parsed HPK tree
        /// </summary>
        public readonly List<ParsedHpkNode> HpkPackages;
        /// <summary>
        /// String database
        /// </summary>
        public readonly List<StringDataBase> StringDB;

        /// <summary>
        /// Parses a set of packages
        /// </summary>
        /// <param name="Packages">List of packages that will be parsed</param>
        /// <param name="Language">Language which is used for parsing</param>
        public ParsedHpkStringContainer(List<HiiPackageBase> Packages, string Language)
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
                        CreateLogEntryParser(LogSeverity.ERROR, "Parsing failed!" + Environment.NewLine + ex.ToString());
                    }
                }
            }
        }

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
        public string GetString(UInt16 StringID, int UniqueID, bool bZeroIsEmpty = true)
        {
            if (StringID == 0 && bZeroIsEmpty)
                return "";

            foreach (string lang in new string[] { Language, "en-US" }) // try current first and english as fallback
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

            //CreateLogEntryParser(LogSeverity.WARNING, "StringID " + StringID + " could not be translated [" + UniqueID + "]!"); // this may occur pretty often, so keep line inactive by default
            return "UNKNOWN_STRING_ID(" + StringID + ")";
        }

        /// <summary>
        /// Builds humand readable string of an IFR typed values including its type and value
        /// </summary>
        /// <param name="type">The IFR type of the value</param>
        /// <param name="Value">Input value</param>
        /// <param name="UniqueID">ID of the requesting HPK element (for reference on errors)</param>
        /// <returns>Humand readable string</returns>
        public string GetValueString(EFI_IFR_TYPE_e type, object Value, int UniqueID, ref object RawValue)
        {
            string TypeStr;
            string ValueStr;
            switch (type)
            {
                case EFI_IFR_TYPE_e.EFI_IFR_TYPE_NUM_SIZE_8: TypeStr = "UINT8"; RawValue = ((IfrTypeUINT8)Value).u8; ValueStr = RawValue.ToString(); break;
                case EFI_IFR_TYPE_e.EFI_IFR_TYPE_NUM_SIZE_16: TypeStr = "UINT16"; RawValue = ((IfrTypeUINT16)Value).u16; ValueStr = RawValue.ToString(); break;
                case EFI_IFR_TYPE_e.EFI_IFR_TYPE_NUM_SIZE_32: TypeStr = "UINT32"; RawValue = ((IfrTypeUINT32)Value).u32; ValueStr = RawValue.ToString(); break;
                case EFI_IFR_TYPE_e.EFI_IFR_TYPE_NUM_SIZE_64: TypeStr = "UINT64"; RawValue = ((IfrTypeUINT64)Value).u64; ValueStr = RawValue.ToString(); break;
                case EFI_IFR_TYPE_e.EFI_IFR_TYPE_BOOLEAN: TypeStr = "BOOLEAN"; RawValue = (((IfrTypeBOOLEAN)Value).b == 0 ? "FALSE" : "TRUE"); ValueStr = RawValue.ToString(); break;
                case EFI_IFR_TYPE_e.EFI_IFR_TYPE_TIME: TypeStr = "TIME"; RawValue = ((EFI_HII_TIME)Value).Hour.ToString("D2") + ":" + ((EFI_HII_TIME)Value).Minute.ToString("D2") + ":" + ((EFI_HII_TIME)Value).Second.ToString("D2"); ValueStr = RawValue.ToString(); break;
                case EFI_IFR_TYPE_e.EFI_IFR_TYPE_DATE: TypeStr = "DATE"; RawValue = ((EFI_HII_DATE)Value).Year.ToString("D4") + "-" + ((EFI_HII_DATE)Value).Month.ToString("D2") + "-" + ((EFI_HII_DATE)Value).Day.ToString("D2"); ValueStr = RawValue.ToString(); break;
                case EFI_IFR_TYPE_e.EFI_IFR_TYPE_STRING: TypeStr = "STRING"; RawValue = GetString(((IfrTypeEFI_STRING_ID)Value).stringid, UniqueID); ValueStr = ((IfrTypeEFI_STRING_ID)Value).stringid.ToDecimalString(5) + " [\"" + RawValue.ToString() + "\"]"; break;
                case EFI_IFR_TYPE_e.EFI_IFR_TYPE_OTHER: TypeStr = "OTHER"; RawValue = ValueStr = "<SEE NESTED IFR>"; ValueStr = RawValue.ToString(); break; // There is no value. It is nested and part of next IFR OpCode object
                case EFI_IFR_TYPE_e.EFI_IFR_TYPE_UNDEFINED: TypeStr = "UNDEFINED"; RawValue = "UNDEFINED"; ValueStr = RawValue.ToString(); CreateLogEntryParser(LogSeverity.WARNING, "Data type not speficied [" + UniqueID + "]!"); break;
                case EFI_IFR_TYPE_e.EFI_IFR_TYPE_ACTION: TypeStr = "ACTION"; RawValue = GetString(((IfrTypeEFI_STRING_ID)Value).stringid, UniqueID); ValueStr = ((IfrTypeEFI_STRING_ID)Value).stringid.ToDecimalString(5) + " [\"" + RawValue.ToString() + "\"]"; break;
                case EFI_IFR_TYPE_e.EFI_IFR_TYPE_BUFFER: TypeStr = "BUFFER"; RawValue = "<SEE DETAILS>"; ValueStr = RawValue.ToString(); break;
                case EFI_IFR_TYPE_e.EFI_IFR_TYPE_REF: TypeStr = "REF"; RawValue = "<SEE DETAILS>"; ValueStr = RawValue.ToString(); break;
                default: TypeStr = "?"; RawValue = "?"; ValueStr = "?"; CreateLogEntryParser(LogSeverity.WARNING, "Unknown data type of [" + UniqueID + "]!"); break;
            }
            return "Type = " + TypeStr + ", Value = " + ValueStr;
        }

        #region Parser - IFR Logic

        /// <summary>
        /// Translates IFR logic Opcodes into human readable string
        /// </summary>
        /// <param name="ifrelem">IFR element holding the input data</param>
        /// <returns>Human readable string</returns>
        public string GetIfrLogicString(HiiIfrOpCode ifrelem)
        {
            IfrLogicStack Stack = new IfrLogicStack();
            string LogicString;

            ParseIfrLogicStack(ifrelem, Stack);

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
                                    Stack.Push("ValueOfId(" + Stack.Pop() + (ifr_hdr.DevicePath == 0 ? "" : ", DevicePath = \"" + GetString(ifr_hdr.DevicePath, ifrelem.UniqueID) + "\"") + ")");
                                }
                                break;
                            case "EFI_IFR_QUESTION_REF3_3":
                                {
                                    EFI_IFR_QUESTION_REF3_3 ifr_hdr = (EFI_IFR_QUESTION_REF3_3)ifrelem.Header;
                                    Stack.Push("ValueOfId(" + Stack.Pop() + ", Guid = " + ifr_hdr.Guid.Guid.ToString() + (ifr_hdr.DevicePath == 0 ? "" : ", DevicePath = \"" + GetString(ifr_hdr.DevicePath, ifrelem.UniqueID) + "\"") + ")");
                                }
                                break;
                            default:
                                Stack.Push("UNKNWONOPCODE(" + ifrelem.OpCode.ToString() + ")");
                                CreateLogEntryParser(LogSeverity.UNIMPLEMENTED, "Logic reference type " + ifrelem.OpCode.ToString() + " [" + ifrelem.UniqueID + "]!");
                                break;
                        }
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_RULE_REF_OP: Stack.Push(((EFI_IFR_RULE_REF)ifrelem.Header).RuleId.ToString()); break;
                case EFI_IFR_OPCODE_e.EFI_IFR_STRING_REF1_OP:
                    {
                        UInt16 StringID = ((EFI_IFR_STRING_REF1)ifrelem.Header).StringId;
                        Stack.Push("GetString(Id = " + StringID.ToDecimalString(5) + " [\"" + GetString(StringID, ifrelem.UniqueID) + "\"])");
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
                            Stack.Push("GetString(Id = " + StringID.ToDecimalString(5) + " [\"" + GetString(StringID, ifrelem.UniqueID) + "\"])");
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
                            CreateLogEntryParser(LogSeverity.WARNING, "Map has invalid expression pairs [" + ifrelem.UniqueID + "]!");
                        }
                        else
                        {
                            string result = "Switch(" + expr;
                            for (int i = 0; i < ifrelem.Childs.Count; i += 2) // for each expression pair
                            {
                                result += ", {" + GetIfrLogicString((HiiIfrOpCode)ifrelem.Childs[i]) // Match expression
                                    + ", " + GetIfrLogicString((HiiIfrOpCode)ifrelem.Childs[i + 1]) + "}"; // Result expression
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
                    CreateLogEntryParser(LogSeverity.UNIMPLEMENTED, "Logic OpCode " + ifrelem.OpCode.ToString() + " [" + ifrelem.UniqueID + "]!");
                    break;
            }

            if (ifrelem.HasOwnScope)
            {
                foreach (HiiIfrOpCode child in ifrelem.Childs)
                    ParseIfrLogicStack(child, Stack);
            }
        }
  
        #endregion
    }

    /// <summary>
    /// Structure for IFR logic stack implementation
    /// </summary>
    public class IfrLogicStack
    {
        public List<string> Elements;

        public IfrLogicStack()
        {
            Elements = new List<string>();
        }

        /// <summary>
        /// Adds a value ontop of the stack
        /// </summary>
        /// <param name="Expression">Value to add</param>
        public void Push(string ExpressionString)
        {
            Elements.Add(ExpressionString);
        }

        /// <summary>
        /// Retrieves top value on stack and removes it from stack
        /// </summary>
        /// <returns>Top stack value</returns>
        public string Pop()
        {
            if (Elements.Count == 0)
            {
                CreateLogEntryParser(LogSeverity.WARNING, "Logic stack empty!");
                return "EMPTYSTACK";
            }
            string LogicStr = Elements[Elements.Count - 1];
            Elements.RemoveAt(Elements.Count - 1);
            return LogicStr;
        }

        /// <summary>
        /// Performs an operation with 2 operands (first left, second right)
        /// </summary>
        /// <param name="Operation">Operation name/sign</param>
        public void Op12(string Operation)
        {
            string expr1 = Pop();
            string expr2 = Pop();
            Push("(" + expr1 + " " + Operation + " " + expr2 + ")");
        }

        /// <summary>
        /// Performs an operation with 2 operands (first right, second left)
        /// </summary>
        /// <param name="Operation">Operation name/sign</param>
        public void Op21(string Operation)
        {
            string expr1 = Pop();
            string expr2 = Pop();
            Push("(" + expr2 + " " + Operation + " " + expr1 + ")");
        }
    }
}
