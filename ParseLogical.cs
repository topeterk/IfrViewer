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
                            UInt16 StringID = 0;
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

            switch (elem.OpCode)
            {
                case EFI_IFR_OPCODE_e.EFI_IFR_FORM_SET_OP:
                    EFI_IFR_FORM_SET hdr = (EFI_IFR_FORM_SET)elem.Header;
                    ParsedHpkNode leaf = new ParsedHpkNode(hpkelem, "Settings");
                    ParsedHpkNode setting = new ParsedHpkNode(hpkelem, "");
                    branch.Name = "FormSet \"" + GetStringOfPackages(StringDB, hdr.FormSetTitle) + "\"";
                    branch.Childs.Add(leaf);
                    setting.Name = "Help = " + GetStringOfPackages(StringDB, hdr.Help);
                    leaf.Childs.Add(setting);
                    setting.Name = "Guid = " + hdr.Guid.Guid.ToString();
                    leaf.Childs.Add(setting);
                    foreach (EFI_GUID classguid in (List<EFI_GUID>)elem.Payload)
                    {
                        setting.Name = "ClassGuid = " + classguid.Guid.ToString();
                        leaf.Childs.Add(setting);
                    }
                    foreach (HiiIfrOpCode child in elem.Childs)
                    {
                        switch (child.OpCode)
                        {
                            default: ParsePackageIfr(child, branch, Packages, StringDB); break;
                        }
                    }
                    break;
                case EFI_IFR_OPCODE_e.EFI_IFR_END_OP: return; // Skip
                default:
                    foreach (HiiIfrOpCode child in elem.Childs)
                        ParsePackageIfr(child, branch, Packages, StringDB);
                    break; // simply add all others 1:1 when no specific handler exists
            }

            root.Childs.Add(branch);
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

        private static string GetStringOfPackages(List<StringDataBase> StringDB, UInt16 StringID, string Language = "en-US")
        {
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

            CreateLogEntryParser(LogSeverity.WARNING, "StringID " + StringID + " could not be translated!");
            return "UNKNOWN_STRING_ID(" + StringID + ")";
        }

        private static void CreateLogEntryParser(LogSeverity severity, string msg)
        {
            CreateLogEntry(severity, "Parser", msg);
        }
    }
}
