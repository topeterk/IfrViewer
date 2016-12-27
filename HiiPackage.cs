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

                switch (ifr_hdr.OpCode)
                {
                    case EFI_IFR_OPCODE_e.EFI_IFR_FORM_SET_OP:
                        Childs.Add(new HiiIfrOpCodeFormSet(ifr_hdr, raw_data));
                        break;
                    default:
                        raw_data.DumpToDebugConsole(ifr_hdr.OpCode.ToString());
                        PrintConsoleMsg(IfrErrorSeverity.UNIMPLEMENTED, ifr_hdr.OpCode.ToString());
                        Childs.Add(new HiiIfrOpCode(ifr_hdr, raw_data));
                        break;
                }

                offset += ifr_hdr.Length;
            }
        }
    }

    /// <summary>
    /// Hii Ifr Opcode base class
    /// </summary>
    class HiiIfrOpCode : HPKElement
    {

        /// <summary>
        /// Type of IFR opcode
        /// </summary>
        public readonly EFI_IFR_OPCODE_e OpCode;

        /// <summary>
        /// Friendly name of this object
        /// </summary> 
        public override string Name { get { return OpCode.ToString(); } }

        public HiiIfrOpCode(EFI_IFR_OP_HEADER hdr, IfrRawDataBlock raw) : base(raw)
        {
            this.OpCode = hdr.OpCode;
        }
    }

    /// <summary>
    /// Hii Ifr Opcode class of EFI_IFR_FORM_SET_OP
    /// </summary>
    class HiiIfrOpCodeFormSet : HiiIfrOpCode
    {
        /// <summary>
        /// Managed structure header
        /// </summary>
        protected EFI_IFR_FORM_SET _Header;
        /// <summary>
        /// Managed structure header
        /// </summary>
        public override object Header { get { return _Header; } }

        public HiiIfrOpCodeFormSet(EFI_IFR_OP_HEADER hdr, IfrRawDataBlock raw) : base(hdr, raw)
        {
            EFI_IFR_FORM_SET ifr_hdr = data.ToIfrType<EFI_IFR_FORM_SET>();
            data.IncreaseOffset(ifr_hdr.GetPhysSize());
            this._Header = ifr_hdr;

            PrintConsoleMsg(IfrErrorSeverity.UNIMPLEMENTED, ifr_hdr.ToString());

            // Parse all GUIDs..
            uint offset = 0;
            while (offset < data.Length)
            {
                if (data.Length < 16)
                    throw new Exception("Payload length invalid");

                EFI_GUID guid = data.ToIfrType<EFI_GUID>(offset);

                offset += 16;
            }
        }
    }
}
