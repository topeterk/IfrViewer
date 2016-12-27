//MIT License
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
using static IFR.IFRHelper;

namespace IFR
{
    /// <summary>
    /// Hii package base class
    /// </summary>
    class HiiPackage : HPKElement
    {
        /// <summary>
        /// Managed structure header
        /// </summary>
        protected EFI_HII_PACKAGE_HEADER _Header;
        /// <summary>
        /// Managed structure header
        /// </summary>
        public override object Header { get { return _Header; } }

        /// <summary>
        /// Type of HII package
        /// </summary>
        public readonly EFI_HII_PACKAGE_e Type;
        /// <summary>
        /// Friendly name of this object
        /// </summary> 
        public override string Name { get { return Type.ToString(); } }

        public HiiPackage(IfrRawDataBlock raw, EFI_HII_PACKAGE_e Type) : base(raw)
        {
            this.Type = Type;
        }
    }

    /// <summary>
    /// Hii package class for Forms
    /// </summary>
    class HiiPackageForm : HiiPackage
    {
        public HiiPackageForm(EFI_HII_FORM_PACKAGE_HDR hdr, IfrRawDataBlock raw) : base(raw, EFI_HII_PACKAGE_e.EFI_HII_PACKAGE_FORMS)
        {
            this._Header = hdr.Header;
            data.IncreaseOffset(hdr.GetPhysSize());

            // Parse all IFR opcodes..
            uint offset = 0;
            while (offset < data.Length)
            {
                EFI_IFR_OP_HEADER ifr_hdr = data.ToIfrType<EFI_IFR_OP_HEADER>(offset);
                if (data.Length < ifr_hdr.Length + offset)
                    throw new Exception("Payload length invalid");

                IfrRawDataBlock raw_data = new IfrRawDataBlock(data.Bytes, data.Offset + offset, ifr_hdr.Length);
                HPKElement hpk_element;

                switch (ifr_hdr.OpCode)
                {
                    case EFI_IFR_OPCODE_e.EFI_IFR_FORM_SET_OP: hpk_element = new HiiIfrOpCodeFormSet(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_DEFAULTSTORE_OP: hpk_element = new HiiIfrOpCode<EFI_IFR_DEFAULTSTORE>(raw_data); break;
                    default:
                        raw_data.DumpToDebugConsole(ifr_hdr.OpCode.ToString());
                        PrintConsoleMsg(IfrErrorSeverity.UNIMPLEMENTED, ifr_hdr.OpCode.ToString());
                        hpk_element = new HiiIfrOpCode<EFI_IFR_OP_HEADER>(raw_data);
                        break;
                }
                Childs.Add(hpk_element);

                offset += ifr_hdr.Length;
            }
        }
    }

    /// <summary>
    /// Hii Ifr Opcode base class
    /// </summary>
    class HiiIfrOpCode<T> : HPKElement
    {
        /// <summary>
        /// Type of IFR opcode
        /// </summary>
        public readonly EFI_IFR_OPCODE_e OpCode;

        /// <summary>
        /// Managed structure header
        /// </summary>
        protected T _Header;
        /// <summary>
        /// Managed structure header
        /// </summary>
        public override object Header { get { return _Header; } }

        /// <summary>
        /// Friendly name of this object
        /// </summary> 
        public override string Name { get { string name = Enum.GetName(OpCode.GetType(), OpCode); return name == null ? "UNKNOWN" : name; } }

        public HiiIfrOpCode(IfrRawDataBlock raw) : base(raw)
        {
            this._Header = data.ToIfrType<T>();

            // Get OpCode from header (first attempt, get the header structures "Header" field ; covers all EFI_IFR_? structs except EFI_IFR_OP_HEADER)..
            foreach (System.Reflection.MemberInfo mi in _Header.GetType().GetMember("Header"))
            {
                if (mi is System.Reflection.FieldInfo)
                {
                    System.Reflection.FieldInfo fi = (System.Reflection.FieldInfo)mi;
                    try
                    {
                        this.OpCode = ((EFI_IFR_OP_HEADER)fi.GetValue(_Header)).OpCode;
                    }
                    catch (Exception)
                    {
                        throw new Exception("Type \"" + _Header.ToString() + "\" has invalid property \"Header\"");
                    }
                    break;
                }
            }
            // Get OpCode directy (second attempt, read the value from header structure directly ; covers EFI_IFR_OP_HEADER)..
            if (OpCode == 0)
            {
                System.Reflection.PropertyInfo pi = _Header.GetType().GetProperty("OpCode");
                try
                {
                    this.OpCode = (EFI_IFR_OPCODE_e)pi.GetValue(_Header);
                }
                catch (Exception)
                {
                    throw new Exception("Type \"" + _Header.ToString() + "\" has invalid property \"OpCode\"");
                }
            }
            // Check if OpCode could be read correctly..
            if (Enum.GetName(OpCode.GetType(), OpCode) == null)
                PrintConsoleMsg(IfrErrorSeverity.ERROR, "Unknown OpCode \"" + OpCode.ToString() + "\"");
        }
    }

    /// <summary>
    /// Hii Ifr Opcode class of EFI_IFR_FORM_SET_OP
    /// </summary>
    class HiiIfrOpCodeFormSet : HiiIfrOpCode<EFI_IFR_FORM_SET>
    {
        /// <summary>
        /// Managed structure header
        /// </summary>
        protected List<EFI_GUID> _Payload;
        /// <summary>
        /// Managed structure header
        /// </summary>
        public override object Payload { get { return _Payload; } }

        public HiiIfrOpCodeFormSet(IfrRawDataBlock raw) : base(raw)
        {
            data.IncreaseOffset(this._Header.GetPhysSize());
            this._Payload = new List<EFI_GUID>();

            // Parse all GUIDs..
            uint offset = 0;
            while (offset < data.Length)
            {
                if (data.Length < 16)
                    throw new Exception("Payload length invalid");

                _Payload.Add(data.ToIfrType<EFI_GUID>(offset));

                offset += 16;
            }
        }
    }
}
