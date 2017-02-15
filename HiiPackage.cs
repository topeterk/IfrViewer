//MIT License
//
//Copyright(c) 2016-2017 Peter Kirmeier
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

using System;
using System.Collections.Generic;

namespace IFR
{
    /// <summary>
    /// Hii package base class
    /// </summary>
    public class HiiPackageBase : HPKElement
    {
        /// <summary>
        /// Type of HII package
        /// </summary>
        public readonly EFI_HII_PACKAGE_e PackageType;

        /// <summary>
        /// Friendly name of this object
        /// </summary> 
        public override string Name { get { string name = Enum.GetName(PackageType.GetType(), PackageType); return name == null ? "UNKNOWN" : name; } }

        public HiiPackageBase(IfrRawDataBlock raw) : base(raw)
        {
            this.PackageType = data.ToIfrType<EFI_HII_PACKAGE_HEADER>().Type;
        }
    }

    /// <summary>
    /// Hii package generic class
    /// </summary>
    class HiiPackage<T> : HiiPackageBase
    {
        /// <summary>
        /// Managed structure header
        /// </summary>
        protected T _Header;
        /// <summary>
        /// Managed structure header
        /// </summary>
        public override object Header { get { return _Header; } }

        public HiiPackage(IfrRawDataBlock raw) : base(raw)
        {
            this._Header = data.ToIfrType<T>();
        }
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
            ParseIfrScope(null, ref offset);
        }

        private void ParseIfrScope(HiiIfrOpCode parent, ref uint offset)
        {
            // Parse all IFR opcodes of this scope..
            while (offset < data_payload.Length)
            {
                EFI_IFR_OP_HEADER ifr_hdr = data_payload.ToIfrType<EFI_IFR_OP_HEADER>(offset);
                if (data_payload.Length < ifr_hdr.Length + offset)
                    throw new Exception("Payload length invalid");

                IfrRawDataBlock raw_data = new IfrRawDataBlock(data_payload.Bytes, data_payload.Offset + offset, ifr_hdr.Length);
                HiiIfrOpCode ifr_element;

                switch (ifr_hdr.OpCode)
                {
                    // OpCodes which have more data rather just the header (each type has a specific class type)..
                    #region IFR OpCodes (more than just the header)
                    case EFI_IFR_OPCODE_e.EFI_IFR_FORM_OP: ifr_element = new HiiIfrOpCode<EFI_IFR_FORM>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_SUBTITLE_OP: ifr_element = new HiiIfrOpCode<EFI_IFR_SUBTITLE>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_TEXT_OP: ifr_element = new HiiIfrOpCode<EFI_IFR_TEXT>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_IMAGE_OP: ifr_element = new HiiIfrOpCode<EFI_IFR_IMAGE>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_ONE_OF_OP: ifr_element = new HiiIfrOpCodeWithEfiIfrNumericValue<EFI_IFR_ONE_OF>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_CHECKBOX_OP: ifr_element = new HiiIfrOpCode<EFI_IFR_CHECKBOX>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_NUMERIC_OP: ifr_element = new HiiIfrOpCodeWithEfiIfrNumericValue<EFI_IFR_NUMERIC>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_PASSWORD_OP: ifr_element = new HiiIfrOpCode<EFI_IFR_PASSWORD>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_ONE_OF_OPTION_OP: ifr_element = new HiiIfrOpCodeWithEfiIfrTypeValue<EFI_IFR_ONE_OF_OPTION>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_ACTION_OP: ifr_element = new HiiIfrOpCodeAction(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_RESET_BUTTON_OP: ifr_element = new HiiIfrOpCode<EFI_IFR_RESET_BUTTON>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_FORM_SET_OP: ifr_element = new HiiIfrOpCodeFormSet(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_REF_OP: ifr_element = new HiiIfrOpCodeRef(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_NO_SUBMIT_IF_OP: ifr_element = new HiiIfrOpCode<EFI_IFR_NO_SUBMIT_IF>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_INCONSISTENT_IF_OP: ifr_element = new HiiIfrOpCode<EFI_IFR_INCONSISTENT_IF>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_EQ_ID_VAL_OP: ifr_element = new HiiIfrOpCode<EFI_IFR_EQ_ID_VAL>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_EQ_ID_ID_OP: ifr_element = new HiiIfrOpCode<EFI_IFR_EQ_ID_ID>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_EQ_ID_VAL_LIST_OP: ifr_element = new HiiIfrOpCodeEqIdList(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_RULE_OP: ifr_element = new HiiIfrOpCode<EFI_IFR_RULE>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_DATE_OP: ifr_element = new HiiIfrOpCode<EFI_IFR_DATE>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_TIME_OP: ifr_element = new HiiIfrOpCode<EFI_IFR_TIME>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_STRING_OP: ifr_element = new HiiIfrOpCode<EFI_IFR_STRING>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_REFRESH_OP: ifr_element = new HiiIfrOpCode<EFI_IFR_REFRESH>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_ANIMATION_OP: ifr_element = new HiiIfrOpCode<EFI_IFR_ANIMATION>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_ORDERED_LIST_OP: ifr_element = new HiiIfrOpCode<EFI_IFR_ORDERED_LIST>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_VARSTORE_OP: ifr_element = new HiiIfrOpCodeWithAsciiNullTerminatedString<EFI_IFR_VARSTORE>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_VARSTORE_NAME_VALUE_OP: ifr_element = new HiiIfrOpCode<EFI_IFR_VARSTORE_NAME_VALUE>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_VARSTORE_EFI_OP: ifr_element = new HiiIfrOpCodeWithAsciiNullTerminatedString<EFI_IFR_VARSTORE_EFI>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_VARSTORE_DEVICE_OP: ifr_element = new HiiIfrOpCode<EFI_IFR_VARSTORE_DEVICE>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_GET_OP: ifr_element = new HiiIfrOpCode<EFI_IFR_GET>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_SET_OP: ifr_element = new HiiIfrOpCode<EFI_IFR_SET>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_RULE_REF_OP: ifr_element = new HiiIfrOpCode<EFI_IFR_RULE_REF>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_QUESTION_REF1_OP: ifr_element = new HiiIfrOpCode<EFI_IFR_QUESTION_REF1>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_UINT8_OP: ifr_element = new HiiIfrOpCode<EFI_IFR_UINT8>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_UINT16_OP: ifr_element = new HiiIfrOpCode<EFI_IFR_UINT16>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_UINT32_OP: ifr_element = new HiiIfrOpCode<EFI_IFR_UINT32>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_UINT64_OP: ifr_element = new HiiIfrOpCode<EFI_IFR_UINT64>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_TO_STRING_OP: ifr_element = new HiiIfrOpCode<EFI_IFR_TO_STRING>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_FIND_OP: ifr_element = new HiiIfrOpCode<EFI_IFR_FIND>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_STRING_REF1_OP: ifr_element = new HiiIfrOpCode<EFI_IFR_STRING_REF1>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_QUESTION_REF3_OP: ifr_element = new HiiIfrOpCodeQuestionRef(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_SPAN_OP: ifr_element = new HiiIfrOpCode<EFI_IFR_SPAN>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_DEFAULT_OP: ifr_element = new HiiIfrOpCodeWithEfiIfrTypeValue<EFI_IFR_DEFAULT>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_DEFAULTSTORE_OP: ifr_element = new HiiIfrOpCode<EFI_IFR_DEFAULTSTORE>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_FORM_MAP_OP: ifr_element = new HiiIfrOpCodeFormMap(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_GUID_OP: ifr_element = new HiiIfrOpCode<EFI_IFR_GUID>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_SECURITY_OP: ifr_element = new HiiIfrOpCode<EFI_IFR_SECURITY>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_REFRESH_ID_OP: ifr_element = new HiiIfrOpCode<EFI_IFR_REFRESH_ID>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_WARNING_IF_OP: ifr_element = new HiiIfrOpCode<EFI_IFR_WARNING_IF>(raw_data); break;
                    case EFI_IFR_OPCODE_e.EFI_IFR_MATCH2_OP: ifr_element = new HiiIfrOpCode<EFI_IFR_MATCH2>(raw_data); break;
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
                    case EFI_IFR_OPCODE_e.EFI_IFR_READ_OP:
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
                        ifr_element = new HiiIfrOpCode<EFI_IFR_OP_HEADER>(raw_data);
                        break;
                    // All OpCodes that are unknown to this application must consist of at least the header, but will rise an error message..
                    default:
                        //raw_data.GenerateAndLogDump(ifr_hdr.OpCode.ToString());
                        LogMessage(LogSeverity.UNIMPLEMENTED, ifr_hdr.OpCode.ToString());
                        ifr_element = new HiiIfrOpCode<EFI_IFR_OP_HEADER>(raw_data);
                        break;
                }

                // add element as child..
                if (parent == null)
                    this.Childs.Add(ifr_element);
                else
                    parent.Childs.Add(ifr_element);

                offset += ifr_hdr.Length;

                if (ifr_element.OpCode == EFI_IFR_OPCODE_e.EFI_IFR_END_OP)
                    return; // Scope is done!
                else if (ifr_element.HasOwnScope)
                    ParseIfrScope(ifr_element, ref offset);
            }
        }
    }

    /// <summary>
    /// Hii Ifr Opcode super base class
    /// </summary>
    class HiiIfrOpCode : HPKElement
    {
        /// <summary>
        /// Type of IFR opcode
        /// </summary>
        public readonly EFI_IFR_OPCODE_e OpCode;
        /// <summary>
        /// Signals if own IFR scope is opened by this IFR opcode
        /// </summary>
        public readonly bool HasOwnScope;

        /// <summary>
        /// Friendly name of this object
        /// </summary> 
        public override string Name { get { string name = Enum.GetName(OpCode.GetType(), OpCode); return name == null ? "UNKNOWN" : name; } }

        public HiiIfrOpCode(IfrRawDataBlock raw) : base(raw)
        {
            EFI_IFR_OP_HEADER hdr = data.ToIfrType<EFI_IFR_OP_HEADER>();
            this.OpCode = hdr.OpCode;
            this.HasOwnScope = hdr.Scope == 1 ? true : false;
        }
    }

    /// <summary>
    /// Hii Ifr Opcode base class
    /// </summary>
    /// <typeparam name="T">Type of IFR opcode header</typeparam>
    class HiiIfrOpCode<T> : HiiIfrOpCode where T : struct
    {
        /// <summary>
        /// Managed structure header
        /// </summary>
        protected T _Header;
        /// <summary>
        /// Managed structure header
        /// </summary>
        public override object Header { get { return _Header; } }

        public HiiIfrOpCode(IfrRawDataBlock raw) : base(raw)
        {
            this._Header = data.ToIfrType<T>();
            if (data.Length != 0)
            {
                this.data_payload = new IfrRawDataBlock(data);
                data_payload.IncreaseOffset(this._Header.GetPhysSize());
            }
        }
    }

    /// <summary>
    /// Hii Ifr Opcode base class with payload
    /// </summary>
    /// <typeparam name="T">Type of IFR opcode header</typeparam>
    /// <typeparam name="P">Type of payload</typeparam>
    class HiiIfrOpCodeWithPayload<T, P> : HiiIfrOpCode<T> where T : struct
    {
        /// <summary>
        /// Managed structure payload
        /// </summary>
        protected P _Payload;
        /// <summary>
        /// Managed structure payload
        /// </summary>
        public override object Payload { get { return _Payload; } }

        public HiiIfrOpCodeWithPayload(IfrRawDataBlock raw) : base(raw) { return; }
    }

    /// <summary>
    /// Hii Ifr Opcode class of EFI_IFR_FORM_SET_OP
    /// </summary>
    class HiiIfrOpCodeFormSet : HiiIfrOpCodeWithPayload<EFI_IFR_FORM_SET, List<EFI_GUID>>
    {
        public HiiIfrOpCodeFormSet(IfrRawDataBlock raw) : base(raw)
        {
            this._Payload = new List<EFI_GUID>();

            // Parse all GUIDs..
            uint offset = 0;
            while (offset < data_payload.Length)
            {
                if ((data_payload.Length + offset) < 16)
                {
                    LogMessage(LogSeverity.ERROR, Name + ": Payload length invalid!");
                    break;
                }

                _Payload.Add(data_payload.ToIfrType<EFI_GUID>(offset));

                offset += 16;
            }

            if (this._Header.Flags_ClassGuidCount != _Payload.Count) // information doubled in structure, so we use it for sanity check
                LogMessage(LogSeverity.ERROR, Name + ": Size of payload (" + _Payload.Count +  ") does not match with header (" + this._Header.Flags_ClassGuidCount + ")!");
        }
    }

    /// <summary>
    /// Hii Ifr Opcode class which has a null terminated ASCII string as payload
    /// </summary>
    /// <typeparam name="T">Type of IFR opcode header</typeparam>
    class HiiIfrOpCodeWithAsciiNullTerminatedString<T> : HiiIfrOpCodeWithPayload<T, object> where T : struct
    {
        public struct NamedPayload_t
        {
            public string Name;
        }
        public HiiIfrOpCodeWithAsciiNullTerminatedString(IfrRawDataBlock raw) : base(raw)
        {
            NamedPayload_t pl = new NamedPayload_t();
            pl.Name = data_payload.CopyOfAsciiNullTerminatedString;
            _Payload = pl;
        }
    }

    /// <summary>
    /// Hii Ifr Opcode class which has EFI_IFR_NUMERIC_MINMAXSTEP_DATA_x as payload
    /// </summary>
    /// <typeparam name="T">Type of IFR opcode header</typeparam>
    class HiiIfrOpCodeWithEfiIfrNumericValue<T> : HiiIfrOpCodeWithPayload<T, object> where T : struct, IEfiIfrNumericValue
    {
        public HiiIfrOpCodeWithEfiIfrNumericValue(IfrRawDataBlock raw) : base(raw)
        {
            switch (this._Header.Flags_DataSize)
            {
                case EFI_IFR_NUMERIC_SIZE_e.EFI_IFR_NUMERIC_SIZE_1: _Payload = data_payload.ToIfrType<EFI_IFR_NUMERIC_MINMAXSTEP_DATA_8>(); break;
                case EFI_IFR_NUMERIC_SIZE_e.EFI_IFR_NUMERIC_SIZE_2: _Payload = data_payload.ToIfrType<EFI_IFR_NUMERIC_MINMAXSTEP_DATA_16>(); break;
                case EFI_IFR_NUMERIC_SIZE_e.EFI_IFR_NUMERIC_SIZE_4: _Payload = data_payload.ToIfrType<EFI_IFR_NUMERIC_MINMAXSTEP_DATA_32>(); break;
                case EFI_IFR_NUMERIC_SIZE_e.EFI_IFR_NUMERIC_SIZE_8: _Payload = data_payload.ToIfrType<EFI_IFR_NUMERIC_MINMAXSTEP_DATA_64>(); break;
                default:
                    LogMessage(LogSeverity.ERROR, Name + ": Unknown data size of EFI_IFR_NUMERIC_MINMAXSTEP_DATA_x");
                    break;
            }
        }
    }

    /// <summary>
    /// Hii Ifr Opcode class which has EFI_IFR_TYPE_VALUE as payload
    /// </summary>
    /// <typeparam name="T">Type of IFR opcode header</typeparam>
    class HiiIfrOpCodeWithEfiIfrTypeValue<T> : HiiIfrOpCodeWithPayload<T, object> where T : struct, IEfiIfrType
    {
        public HiiIfrOpCodeWithEfiIfrTypeValue(IfrRawDataBlock raw) : base(raw)
        {
            switch (this._Header.Type)
            {
                case EFI_IFR_TYPE_e.EFI_IFR_TYPE_NUM_SIZE_8: _Payload = data_payload.ToIfrType<IfrTypeUINT8>(); break;
                case EFI_IFR_TYPE_e.EFI_IFR_TYPE_NUM_SIZE_16: _Payload = data_payload.ToIfrType<IfrTypeUINT16>(); break;
                case EFI_IFR_TYPE_e.EFI_IFR_TYPE_NUM_SIZE_32: _Payload = data_payload.ToIfrType<IfrTypeUINT32>(); break;
                case EFI_IFR_TYPE_e.EFI_IFR_TYPE_NUM_SIZE_64: _Payload = data_payload.ToIfrType<IfrTypeUINT64>(); break;
                case EFI_IFR_TYPE_e.EFI_IFR_TYPE_BOOLEAN: _Payload = data_payload.ToIfrType<IfrTypeBOOLEAN>(); break;
                case EFI_IFR_TYPE_e.EFI_IFR_TYPE_TIME: _Payload = data_payload.ToIfrType<EFI_HII_TIME>(); break;
                case EFI_IFR_TYPE_e.EFI_IFR_TYPE_DATE: _Payload = data_payload.ToIfrType<EFI_HII_DATE>(); break;
                case EFI_IFR_TYPE_e.EFI_IFR_TYPE_STRING: _Payload = data_payload.ToIfrType<IfrTypeEFI_STRING_ID>(); break;
                case EFI_IFR_TYPE_e.EFI_IFR_TYPE_OTHER: break; // There is no value. It is nested and part of next IFR OpCode object
                case EFI_IFR_TYPE_e.EFI_IFR_TYPE_UNDEFINED: LogMessage(LogSeverity.WARNING, Name + ": Data type not speficied"); break;
                case EFI_IFR_TYPE_e.EFI_IFR_TYPE_ACTION: _Payload = data_payload.ToIfrType<IfrTypeEFI_STRING_ID>(); break;
                case EFI_IFR_TYPE_e.EFI_IFR_TYPE_BUFFER: _Payload = data_payload.CopyOfSelectedBytes; break;
                case EFI_IFR_TYPE_e.EFI_IFR_TYPE_REF: _Payload = data_payload.ToIfrType<EFI_HII_REF>(); break;
                default: LogMessage(LogSeverity.ERROR, Name + ": Unknown data type of EFI_IFR_TYPE_VALUE"); break;
            }
        }
    }

    /// <summary>
    /// Hii Ifr Opcode class of EFI_IFR_EQ_ID_VAL_LIST_OP
    /// </summary>
    class HiiIfrOpCodeEqIdList : HiiIfrOpCodeWithPayload<EFI_IFR_EQ_ID_VAL_LIST, List<IfrTypeUINT16>>
    {
        public HiiIfrOpCodeEqIdList(IfrRawDataBlock raw) : base(raw)
        {
            this._Payload = new List<IfrTypeUINT16>();

            // Parse all IDs..
            uint offset = 0;
            while (offset < data_payload.Length)
            {
                if ((data_payload.Length + offset) < typeof(IfrTypeUINT16).StructLayoutAttribute.Size)
                {
                    LogMessage(LogSeverity.ERROR, Name + ": Payload length invalid!");
                    break;
                }

                _Payload.Add(data_payload.ToIfrType<IfrTypeUINT16>(offset));

                offset += (uint)typeof(IfrTypeUINT16).StructLayoutAttribute.Size;
            }

            if (this._Header.ListLength != _Payload.Count) // information doubled in structure, so we use it for sanity check
                LogMessage(LogSeverity.ERROR, Name + ": Size of payload (" + _Payload.Count + ") does not match with header (" + this._Header.ListLength + ")!");
        }
    }

    /// <summary>
    /// Hii Ifr Opcode class of EFI_IFR_QUESTION_REF3_OP
    /// </summary>
    class HiiIfrOpCodeQuestionRef : HiiIfrOpCode<EFI_IFR_QUESTION_REF3>
    {
        /// <summary>
        /// Managed structure header (Extended due to successfull header validation)
        /// </summary>
        protected object _HeaderEx;
        /// <summary>
        /// Managed structure header
        /// </summary>
        public override object Header { get { return _HeaderEx; } }

        public HiiIfrOpCodeQuestionRef(IfrRawDataBlock raw) : base(raw)
        {
            if (_Header.Header.Length == typeof(EFI_IFR_QUESTION_REF3_2).StructLayoutAttribute.Size)
            {
                _HeaderEx = raw.ToIfrType<EFI_IFR_QUESTION_REF3_2>();
                this.data_payload = null; // payload is part of header, now
            }
            else if (_Header.Header.Length == typeof(EFI_IFR_QUESTION_REF3_3).StructLayoutAttribute.Size)
            {
                _HeaderEx = raw.ToIfrType<EFI_IFR_QUESTION_REF3_3>();
                this.data_payload = null; // payload is part of header, now
            }
            else
                LogMessage(LogSeverity.ERROR, Name + ": Unknown data size of EFI_IFR_QUESTION_REF3");
        }
    }

    /// <summary>
    /// Hii Ifr Opcode class of EFI_IFR_REF_OP
    /// </summary>
    class HiiIfrOpCodeRef : HiiIfrOpCode<EFI_IFR_REF>
    {
        /// <summary>
        /// Managed structure header (Extended due to successfull header validation)
        /// </summary>
        protected object _HeaderEx;
        /// <summary>
        /// Managed structure header
        /// </summary>
        public override object Header { get { return _HeaderEx; } }

        public HiiIfrOpCodeRef(IfrRawDataBlock raw) : base(raw)
        {
            if (_Header.Header.Length == typeof(EFI_IFR_REF).StructLayoutAttribute.Size)
            {
                _HeaderEx = raw.ToIfrType<EFI_IFR_REF>();
                this.data_payload = null; // payload is part of header, now
            }
            else if (_Header.Header.Length == typeof(EFI_IFR_REF2).StructLayoutAttribute.Size)
            {
                _HeaderEx = raw.ToIfrType<EFI_IFR_REF2>();
                this.data_payload = null; // payload is part of header, now
            }
            else if (_Header.Header.Length == typeof(EFI_IFR_REF3).StructLayoutAttribute.Size)
            {
                _HeaderEx = raw.ToIfrType<EFI_IFR_REF3>();
                this.data_payload = null; // payload is part of header, now
            }
            else if (_Header.Header.Length == typeof(EFI_IFR_REF4).StructLayoutAttribute.Size)
            {
                _HeaderEx = raw.ToIfrType<EFI_IFR_REF4>();
                this.data_payload = null; // payload is part of header, now
            }
            else if (_Header.Header.Length == typeof(EFI_IFR_REF5).StructLayoutAttribute.Size)
            {
                _HeaderEx = raw.ToIfrType<EFI_IFR_REF5>();
                this.data_payload = null; // payload is part of header, now
            }
            else
                LogMessage(LogSeverity.ERROR, Name + ": Unknown data size of EFI_IFR_REF");
        }
    }

    /// <summary>
    /// Hii Ifr Opcode class of EFI_IFR_ACTION_OP
    /// </summary>
    class HiiIfrOpCodeAction : HiiIfrOpCode<EFI_IFR_ACTION_1>
    {
        /// <summary>
        /// Managed structure header (Extended due to successfull header validation)
        /// </summary>
        protected object _HeaderEx;
        /// <summary>
        /// Managed structure header
        /// </summary>
        public override object Header { get { return _HeaderEx; } }

        public HiiIfrOpCodeAction(IfrRawDataBlock raw) : base(raw)
        {
            if (_Header.Header.Length == typeof(EFI_IFR_ACTION).StructLayoutAttribute.Size)
            {
                _HeaderEx = raw.ToIfrType<EFI_IFR_ACTION>();
                this.data_payload = null; // payload is part of header, now
            }
            else if (_Header.Header.Length == typeof(EFI_IFR_ACTION_1).StructLayoutAttribute.Size)
            {
                _HeaderEx = raw.ToIfrType<EFI_IFR_ACTION_1>();
                this.data_payload = null; // payload is part of header, now
            }
            else
                LogMessage(LogSeverity.ERROR, Name + ": Unknown data size of EFI_IFR_QUESTION_REF3");
        }
    }

    /// <summary>
    /// Hii Ifr Opcode class of EFI_IFR_FORM_MAP_OP
    /// </summary>
    class HiiIfrOpCodeFormMap : HiiIfrOpCodeWithPayload<EFI_IFR_FORM_MAP, List<EFI_IFR_FORM_MAP_METHOD>>
    {
        public HiiIfrOpCodeFormMap(IfrRawDataBlock raw) : base(raw)
        {
            this._Payload = new List<EFI_IFR_FORM_MAP_METHOD>();

            // Parse all methods..
            uint offset = 0;
            while (offset < data_payload.Length)
            {
                if ((data_payload.Length + offset) < typeof(EFI_IFR_FORM_MAP_METHOD).StructLayoutAttribute.Size)
                {
                    LogMessage(LogSeverity.ERROR, Name + ": Payload length invalid!");
                    break;
                }

                _Payload.Add(data_payload.ToIfrType<EFI_IFR_FORM_MAP_METHOD>(offset));

                offset += (uint)typeof(EFI_IFR_FORM_MAP_METHOD).StructLayoutAttribute.Size;
            }
        }
    }

    #endregion

    #region Definitions for String Package
    /// <summary>
    /// Hii package class for Strings
    /// </summary>
    class HiiPackageString : HiiPackage<EFI_HII_STRING_PACKAGE_HDR>
    {
        public struct Payload_t
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
                    case EFI_HII_SIBT_e.EFI_HII_SIBT_END: hpk_element = new HiiSibtBlockNoPayload<EFI_HII_STRING_BLOCK>(raw_data); break;
                    /*case EFI_HII_SIBT_e.EFI_HII_SIBT_STRING_SCSU:
                    case EFI_HII_SIBT_e.EFI_HII_SIBT_STRING_SCSU_FONT:
                    case EFI_HII_SIBT_e.EFI_HII_SIBT_STRINGS_SCSU:
                    case EFI_HII_SIBT_e.EFI_HII_SIBT_STRINGS_SCSU_FONT:*/
                    case EFI_HII_SIBT_e.EFI_HII_SIBT_STRING_UCS2: hpk_element = new HiiSibtBlockStringUcs2(raw_data); break;
                    /*case EFI_HII_SIBT_e.EFI_HII_SIBT_STRING_UCS2_FONT:
                    case EFI_HII_SIBT_e.EFI_HII_SIBT_STRINGS_UCS2:
                    case EFI_HII_SIBT_e.EFI_HII_SIBT_STRINGS_UCS2_FONT:
                    case EFI_HII_SIBT_e.EFI_HII_SIBT_DUPLICATE:*/
                    case EFI_HII_SIBT_e.EFI_HII_SIBT_SKIP2: hpk_element = new HiiSibtBlockNoPayload<EFI_HII_SIBT_SKIP2_BLOCK>(raw_data); break;
                    case EFI_HII_SIBT_e.EFI_HII_SIBT_SKIP1: hpk_element = new HiiSibtBlockNoPayload<EFI_HII_SIBT_SKIP1_BLOCK>(raw_data); break;
                    case EFI_HII_SIBT_e.EFI_HII_SIBT_EXT1: hpk_element = new HiiSibtBlockExt<EFI_HII_SIBT_EXT1_BLOCK>(raw_data); break;
                    case EFI_HII_SIBT_e.EFI_HII_SIBT_EXT2: hpk_element = new HiiSibtBlockExt<EFI_HII_SIBT_EXT2_BLOCK>(raw_data); break;
                    case EFI_HII_SIBT_e.EFI_HII_SIBT_EXT4: hpk_element = new HiiSibtBlockExt<EFI_HII_SIBT_EXT4_BLOCK>(raw_data); break;
                    /*case EFI_HII_SIBT_e.EFI_HII_SIBT_FONT:*/
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
    class HiiSibtBlockBase : HPKElement
    {
        /// <summary>
        /// Type of string information block type
        /// </summary>
        public readonly EFI_HII_SIBT_e BlockType;

        /// <summary>
        /// Friendly name of this object
        /// </summary> 
        public override string Name { get { string name = Enum.GetName(BlockType.GetType(), BlockType); return name == null ? "UNKNOWN" : name; } }

        public HiiSibtBlockBase(IfrRawDataBlock raw) : base(raw)
        {
            this.BlockType = data.ToIfrType<EFI_HII_STRING_BLOCK>().BlockType;
        }
    }

    /// <summary>
    /// Hii Sibt generic class
    /// </summary>
    class HiiSibtBlock<T> : HiiSibtBlockBase where T : struct
    {
        /// <summary>
        /// Managed structure header
        /// </summary>
        protected T _Header;
        /// <summary>
        /// Managed structure header
        /// </summary>
        public override object Header { get { return _Header; } }
        
        public HiiSibtBlock(IfrRawDataBlock raw) : base(raw)
        {
            this._Header = data.ToIfrType<T>();
            if (data.Length != 0)
            {
                this.data_payload = new IfrRawDataBlock(data);
                data_payload.IncreaseOffset(this._Header.GetPhysSize());
            }
        }
    }

    /// <summary>
    /// Hii Sibt class with UCS2 string
    /// </summary>
    class HiiSibtBlockStringUcs2 : HiiSibtBlock<EFI_HII_SIBT_STRING_UCS2_BLOCK>
    {
        public struct Payload_t
        {
            public string StringText;
        }
        /// <summary>
        /// Managed structure payload
        /// </summary>
        private Payload_t _Payload;
        /// <summary>
        /// Managed structure payload
        /// </summary>
        public override object Payload { get { return _Payload; } }

        public HiiSibtBlockStringUcs2(IfrRawDataBlock raw) : base(raw)
        {
            _Payload.StringText = data_payload.CopyOfUnicodeNullTerminatedString;

            // know we know actual data after parsing payload, so fix up lengths..
            data_payload.Length = ((uint)_Payload.StringText.Length)*2 + 2; // n*CHAR16 + NULL
            data.Length = this._Header.GetPhysSize() + data_payload.Length;
        }
    }

    /// <summary>
    /// Hii Sibt class without payload
    /// </summary>
    /// <typeparam name="T">Type of SIBT block header</typeparam>
    class HiiSibtBlockNoPayload<T> : HiiSibtBlock<T> where T : struct
    {
        public HiiSibtBlockNoPayload(IfrRawDataBlock raw) : base(raw)
        {
            this.data_payload = null;
            data.Length = this._Header.GetPhysSize(); // shorten header length since we have no payload
        }
    }

    /// <summary>
    /// Hii Sibt class with payload
    /// </summary>
    /// <typeparam name="T">Type of SIBT block header</typeparam>
    /// <typeparam name="P">Type of payload</typeparam>
    class HiiSibtBlockWithPayload<T, P> : HiiSibtBlock<T> where T : struct
    {
        /// <summary>
        /// Managed structure payload
        /// </summary>
        protected P _Payload;
        /// <summary>
        /// Managed structure payload
        /// </summary>
        public override object Payload { get { return _Payload; } }

        public HiiSibtBlockWithPayload(IfrRawDataBlock raw) : base(raw) { return; }
    }

    /// <summary>
    /// Hii Sibt class with future expansion
    /// </summary>
    class HiiSibtBlockExt<T> : HiiSibtBlockWithPayload<T, byte[]> where T : struct
    {
        public HiiSibtBlockExt(IfrRawDataBlock raw) : base(raw)
        {
            uint len = 0;

            switch (this.BlockType)
            {
                case EFI_HII_SIBT_e.EFI_HII_SIBT_EXT1: len = data.ToIfrType<EFI_HII_SIBT_EXT1_BLOCK>().Length; break;
                case EFI_HII_SIBT_e.EFI_HII_SIBT_EXT2: len = data.ToIfrType<EFI_HII_SIBT_EXT2_BLOCK>().Length; break;
                case EFI_HII_SIBT_e.EFI_HII_SIBT_EXT4: len = data.ToIfrType<EFI_HII_SIBT_EXT4_BLOCK>().Length; break;
                default: LogMessage(LogSeverity.ERROR, Name + ": Unknown data type \"" + this.BlockType + "\""); break;
            }

            if (0 == len)
            {
                data_payload = null;
                data.Length = this._Header.GetPhysSize(); // shorten header length since we have no payload
            }
            else if (data_payload.Length < len)
            {
                data_payload = null;
                data.Length = this._Header.GetPhysSize(); // shorten header length since we removed payload
                LogMessage(LogSeverity.ERROR, Name + ": Payload length invalid!");
            }
            else
            {
                data.Length = data_payload.Length = len;
                data_payload.IncreaseOffset(this._Header.GetPhysSize());
                _Payload = data_payload.CopyOfSelectedBytes;
            }
        }
    }
    #endregion
}
