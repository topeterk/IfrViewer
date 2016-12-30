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
    class HiiPackage<T> : HPKElement
    {
        #region HiiPackage definition
        /// <summary>
        /// Type of HII package
        /// </summary>
        public readonly EFI_HII_PACKAGE_e PackageType;

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
        public override string Name { get { string name = Enum.GetName(PackageType.GetType(), PackageType); return name == null ? "UNKNOWN" : name; } }

        public HiiPackage(IfrRawDataBlock raw) : base(raw)
        {
            this.PackageType = data.ToIfrType<EFI_HII_PACKAGE_HEADER>().Type;
            this._Header = data.ToIfrType<T>();
        }
        #endregion
    }

    #region Definitions for Forms Package
    /// <summary>
    /// Hii package class for Forms
    /// </summary>
    class HiiPackageForm : HiiPackage<EFI_HII_FORM_PACKAGE_HDR>
    {
        public HiiPackageForm(IfrRawDataBlock raw) : base(raw)
        {
            data_payload = new IfrRawDataBlock(data);
            data_payload.IncreaseOffset(_Header.GetPhysSize());

            // Parse all IFR opcodes..
            uint offset = 0;
            while (offset < data_payload.Length)
            {
                EFI_IFR_OP_HEADER ifr_hdr = data_payload.ToIfrType<EFI_IFR_OP_HEADER>(offset);
                if (data_payload.Length < ifr_hdr.Length + offset)
                    throw new Exception("Payload length invalid");

                IfrRawDataBlock raw_data = new IfrRawDataBlock(data_payload.Bytes, data_payload.Offset + offset, ifr_hdr.Length);
                HPKElement hpk_element;

                switch (ifr_hdr.OpCode)
                {
                    // OpCodes which have more data rather just the header (each type has a specific class type)..
                    #region IFR OpCodes (more than just the header)
                    case EFI_IFR_OPCODE_e.EFI_IFR_FORM_OP: hpk_element = new HiiIfrOpCode<EFI_IFR_FORM>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_SUBTITLE_OP: hpk_element = new HiiIfrOpCode<EFI_IFR_SUBTITLE>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_ACTION_OP: hpk_element = new HiiIfrOpCode<EFI_IFR_ACTION>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_FORM_SET_OP: hpk_element = new HiiIfrOpCodeFormSet(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_VARSTORE_OP: hpk_element = new HiiIfrOpCodeVarstore(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_VARSTORE_EFI_OP: hpk_element = new HiiIfrOpCodeVarstoreEfi(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_DEFAULTSTORE_OP: hpk_element = new HiiIfrOpCode<EFI_IFR_DEFAULTSTORE>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_GUID_OP: hpk_element = new HiiIfrOpCodeGuid(raw_data); break;
                    #endregion
                    // OpCode which consists of the header only (there is no special structure, we just use the header itself)..
                    #region IFR OpCodes (just the header)
                    case EFI_IFR_OPCODE_e.EFI_IFR_SUPPRESS_IF_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_LOCKED_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_AND_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_OR_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_NOT_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_GRAY_OUT_IF_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_DISABLE_IF_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_TO_LOWER_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_TO_UPPER_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_MAP_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_VERSION_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_END_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_MATCH_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_WRITE_OP:
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
                    case EFI_IFR_OPCODE_e.EFI_IFR_QUESTION_REF3_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_ZERO_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_ONE_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_ONES_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_UNDEFINED_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_LENGTH_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_DUP_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_THIS_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_VALUE_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_CATENATE_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_MODAL_TAG_OP:
                    #endregion
                        hpk_element = new HiiIfrOpCode<EFI_IFR_OP_HEADER>(raw_data);
                        break;
                    // All OpCodes that are unknown to this application must consist of at least the header, but will rise an error message..
                    #region IFR OpCodes (not yet implemented)
                    /*
                    case EFI_IFR_OPCODE_e.EFI_IFR_TEXT_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_IMAGE_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_ONE_OF_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_CHECKBOX_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_NUMERIC_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_PASSWORD_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_ONE_OF_OPTION_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_RESET_BUTTON_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_REF_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_NO_SUBMIT_IF_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_INCONSISTENT_IF_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_EQ_ID_VAL_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_EQ_ID_ID_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_EQ_ID_VAL_LIST_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_RULE_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_DATE_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_TIME_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_STRING_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_REFRESH_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_ANIMATION_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_ORDERED_LIST_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_VARSTORE_NAME_VALUE_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_VARSTORE_DEVICE_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_GET_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_SET_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_READ_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_RULE_REF_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_QUESTION_REF1_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_UINT8_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_UINT16_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_UINT32_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_UINT64_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_TO_STRING_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_FIND_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_STRING_REF1_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_SPAN_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_DEFAULT_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_FORM_MAP_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_SECURITY_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_REFRESH_ID_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_WARNING_IF_OP:
                    case EFI_IFR_OPCODE_e.EFI_IFR_MATCH2_OP:
                    */
                    #endregion
                    default:
                        //raw_data.GenerateAndLogDump(ifr_hdr.OpCode.ToString());
                        LogMessage(LogSeverity.UNIMPLEMENTED, ifr_hdr.OpCode.ToString());
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
        #region HiiIfrOpCode definition
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
            this.OpCode = data.ToIfrType<EFI_IFR_OP_HEADER>().OpCode;
            this._Header = data.ToIfrType<T>();
        }
        #endregion
    }

    /// <summary>
    /// Hii Ifr Opcode class of EFI_IFR_FORM_SET_OP
    /// </summary>
    class HiiIfrOpCodeFormSet : HiiIfrOpCode<EFI_IFR_FORM_SET>
    {
        /// <summary>
        /// Managed structure payload
        /// </summary>
        protected List<EFI_GUID> _Payload;
        /// <summary>
        /// Managed structure payload
        /// </summary>
        public override object Payload { get { return _Payload; } }

        public HiiIfrOpCodeFormSet(IfrRawDataBlock raw) : base(raw)
        {
            this.data_payload = new IfrRawDataBlock(data);
            data_payload.IncreaseOffset(this._Header.GetPhysSize());
            this._Payload = new List<EFI_GUID>();

            // Parse all GUIDs..
            uint offset = 0;
            while (offset < data_payload.Length)
            {
                if (data_payload.Length < 16)
                    throw new Exception("Payload length invalid");

                _Payload.Add(data_payload.ToIfrType<EFI_GUID>(offset));

                offset += 16;
            }
        }
    }

    /// <summary>
    /// Hii Ifr Opcode class of EFI_IFR_GUID_OP
    /// </summary>
    class HiiIfrOpCodeGuid : HiiIfrOpCode<EFI_IFR_GUID>
    {
        public HiiIfrOpCodeGuid(IfrRawDataBlock raw) : base(raw)
        {
            this.data_payload = new IfrRawDataBlock(data);
            data_payload.IncreaseOffset(this._Header.GetPhysSize());
        }
    }

    /// <summary>
    /// Hii Ifr Opcode class of EFI_IFR_VARSTORE_OP
    /// </summary>
    class HiiIfrOpCodeVarstore : HiiIfrOpCode<EFI_IFR_VARSTORE>
    {
        private struct Payload_t
        {
            public string Name;
        }
        /// <summary>
        /// String text of this block
        /// </summary>
        private Payload_t _Payload;
        /// <summary>
        /// String text of this block
        /// </summary>
        public override object Payload { get { return _Payload; } }

        public HiiIfrOpCodeVarstore(IfrRawDataBlock raw) : base(raw)
        {
            this.data_payload = new IfrRawDataBlock(data);
            data_payload.IncreaseOffset(this._Header.GetPhysSize());
            _Payload.Name = data_payload.CopyOfAsciiNullTerminatedString;
        }
    }

    /// <summary>
    /// Hii Ifr Opcode class of EFI_IFR_VARSTORE_OP
    /// </summary>
    class HiiIfrOpCodeVarstoreEfi : HiiIfrOpCode<EFI_IFR_VARSTORE_EFI>
    {
        private struct Payload_t
        {
            public string Name;
        }
        /// <summary>
        /// String text of this block
        /// </summary>
        private Payload_t _Payload;
        /// <summary>
        /// String text of this block
        /// </summary>
        public override object Payload { get { return _Payload; } }

        public HiiIfrOpCodeVarstoreEfi(IfrRawDataBlock raw) : base(raw)
        {
            this.data_payload = new IfrRawDataBlock(data);
            data_payload.IncreaseOffset(this._Header.GetPhysSize());
            _Payload.Name = data_payload.CopyOfAsciiNullTerminatedString;
        }
    }
    #endregion

    #region Definitions for String Package
    /// <summary>
    /// Hii package class for Strings
    /// </summary>
    class HiiPackageString : HiiPackage<EFI_HII_STRING_PACKAGE_HDR>
    {
        private struct Payload_t
        {
            public string Language;
        }
        /// <summary>
        /// Language identifier for this package
        /// </summary>
        private Payload_t _Payload;
        /// <summary>
        /// Language identifier for this package
        /// </summary>
        public override object Payload { get { return _Payload; } }

        public HiiPackageString(IfrRawDataBlock raw) : base(raw)
        {
            data_payload = new IfrRawDataBlock(data);
            data_payload.IncreaseOffset(_Header.GetPhysSize());

            _Payload.Language = data_payload.CopyOfAsciiNullTerminatedString;

            // Parse all string information block types..
            uint offset = (uint)_Payload.Language.Length + 1;
            while (offset < data_payload.Length)
            {
                IfrRawDataBlock raw_data = new IfrRawDataBlock(data_payload.Bytes, data_payload.Offset + offset, data_payload.Length - offset);
                EFI_HII_STRING_BLOCK block_hdr = raw_data.ToIfrType<EFI_HII_STRING_BLOCK>();
                HPKElement hpk_element;

                switch (block_hdr.BlockType)
                {
                    case EFI_HII_SIBT_e.EFI_HII_SIBT_END: hpk_element = new HiiSibtBlockNoPayload(raw_data); break;
                    /*case EFI_HII_SIBT_e.EFI_HII_SIBT_STRING_SCSU:
                    case EFI_HII_SIBT_e.EFI_HII_SIBT_STRING_SCSU_FONT:
                    case EFI_HII_SIBT_e.EFI_HII_SIBT_STRINGS_SCSU:
                    case EFI_HII_SIBT_e.EFI_HII_SIBT_STRINGS_SCSU_FONT:*/
                    case EFI_HII_SIBT_e.EFI_HII_SIBT_STRING_UCS2: hpk_element = new HiiSibtBlockStringUcs2(raw_data); break;
                    /*case EFI_HII_SIBT_e.EFI_HII_SIBT_STRING_UCS2_FONT:
                    case EFI_HII_SIBT_e.EFI_HII_SIBT_STRINGS_UCS2:
                    case EFI_HII_SIBT_e.EFI_HII_SIBT_STRINGS_UCS2_FONT:
                    case EFI_HII_SIBT_e.EFI_HII_SIBT_DUPLICATE:
                    case EFI_HII_SIBT_e.EFI_HII_SIBT_SKIP2:
                    case EFI_HII_SIBT_e.EFI_HII_SIBT_SKIP1:
                    case EFI_HII_SIBT_e.EFI_HII_SIBT_EXT1:
                    case EFI_HII_SIBT_e.EFI_HII_SIBT_EXT2:
                    case EFI_HII_SIBT_e.EFI_HII_SIBT_EXT4:
                    case EFI_HII_SIBT_e.EFI_HII_SIBT_FONT:*/
                    default:
                        //raw_data.GenerateAndLogDump(ifr_hdr.OpCode.ToString());
                        LogMessage(LogSeverity.UNIMPLEMENTED, block_hdr.BlockType.ToString());
                        LogMessage(LogSeverity.ERROR, "Parsing aborted due to unimplemented type!");
                        return;
                }
                Childs.Add(hpk_element);

                offset += hpk_element.PhysicalSize;
            }
        }
    }

    /// <summary>
    /// Hii Sibt base class
    /// </summary>
    class HiiSibtBlock<T> : HPKElement
    {
        #region HiiSibtBlock definition
        /// <summary>
        /// Type of string information block type
        /// </summary>
        public readonly EFI_HII_SIBT_e BlockType;

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
        public override string Name { get { string name = Enum.GetName(BlockType.GetType(), BlockType); return name == null ? "UNKNOWN" : name; } }

        public HiiSibtBlock(IfrRawDataBlock raw) : base(raw)
        {
            this.BlockType = data.ToIfrType<EFI_HII_STRING_BLOCK>().BlockType;
            this._Header = data.ToIfrType<T>();
        }
        #endregion
    }

    /// <summary>
    /// Hii Sibt class of EFI_IFR_FORM_SET_OP
    /// </summary>
    class HiiSibtBlockStringUcs2 : HiiSibtBlock<EFI_HII_SIBT_STRING_UCS2_BLOCK>
    {
        private struct Payload_t
        {
            public string StringText;
        }
        /// <summary>
        /// String text of this block
        /// </summary>
        private Payload_t _Payload;
        /// <summary>
        /// String text of this block
        /// </summary>
        public override object Payload { get { return _Payload; } }

        public HiiSibtBlockStringUcs2(IfrRawDataBlock raw) : base(raw)
        {
            this.data_payload = new IfrRawDataBlock(data);
            data_payload.IncreaseOffset(this._Header.GetPhysSize());
            _Payload.StringText = data_payload.CopyOfUnicodeNullTerminatedString;

            // know we know actual data after parsing payload, so fix up lengths..
            data_payload.Length = ((uint)_Payload.StringText.Length)*2 + 2; // n*CHAR16 + NULL
            data.Length = this._Header.GetPhysSize() + data_payload.Length;
        }
    }

    /// <summary>
    /// Hii Sibt class of EFI_IFR_FORM_SET_OP
    /// </summary>
    class HiiSibtBlockNoPayload : HiiSibtBlock<EFI_HII_STRING_BLOCK>
    {
        public HiiSibtBlockNoPayload(IfrRawDataBlock raw) : base(raw)
        {
            data.Length = this._Header.GetPhysSize(); // shorten header length since we have no payload
        }
    }
    #endregion
}
