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

// This file contains the EDKII structures of "UefiInternalFormRepresentation.h"

using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;
using static IFR.IFRHelper;

/// <summary>
/// This namespace contains the Internal Form Representation definisions
/// (see: EDKII structures of "UefiInternalFormRepresentation.h")
/// </summary>
namespace IFR
{
    #region 1:1 type assignments between C <-> C#
    using UINT8 = Byte;
    using UINT16 = UInt16;
    using UINT32 = UInt32;
    using UINT64 = UInt64;
    using CHAR16 = Char;
    using EFI_IMAGE_ID = UInt16;
    using EFI_QUESTION_ID = UInt16;
    using EFI_STRING_ID = UInt16;
    using EFI_FORM_ID = UInt16;
    using EFI_VARSTORE_ID = UInt16;
    using EFI_ANIMATION_ID = UInt16;
    using EFI_DEFAULT_ID = UInt16;
    using EFI_HII_FONT_STYLE = UInt32;

    /// <summary>
    /// Wrapper for EFI_GUID
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 16)]
    struct EFI_GUID
    {
        public Guid Guid;
    };
    /// <summary>
    /// Wrapper for UINT16
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 1)]
    struct IfrTypeUINT8
    {
        public UINT8 u8;
    };
    /// <summary>
    /// Wrapper for UINT16
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 2)]
    struct IfrTypeUINT16
    {
        public UINT16 u16;
    };
    /// <summary>
    /// Wrapper for UINT16
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 4)]
    struct IfrTypeUINT32
    {
        public UINT32 u32;
    };
    /// <summary>
    /// Wrapper for UINT16
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 8)]
    struct IfrTypeUINT64
    {
        public UINT64 u64;
    };
    /// <summary>
    /// Wrapper for BOOLEAN
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 1)]
    struct IfrTypeBOOLEAN
    {
        /// <summary>
        /// 0 = false, 1 = true
        /// </summary>
        public UINT8 b;
    };
    /// <summary>
    /// Wrapper for EFI_STRING_ID
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 2)]
    struct IfrTypeEFI_STRING_ID
    {
        public UINT16 stringid; // = "string" but not working in C#
    };
    #endregion

    #region Common stuff (not related to UEFI source)

    #region Interfaces
    public interface IEfiIfrNumericValue
    {
        EFI_IFR_NUMERIC_SIZE_e Flags_DataSize { get; }
    }
    public interface IEfiIfrType
    {
        EFI_IFR_TYPE_e Type { get; }
    }
    #endregion

    /// <summary>
    /// Wrapper to access raw data directly as single memory source
    /// </summary>
    public class IfrRawDataBlock
    {
        public byte[] Bytes;
        public uint Offset;
        public uint Length;

        #region Constructors
        /// <summary>
        /// Creates new IFR raw data block.
        /// Sets initially Offset to 0 and Length according to given raw data length
        /// </summary>
        /// <param name="raw">Data this block should refer</param>
        public IfrRawDataBlock(byte[] raw)
        {
            Bytes = raw;
            Offset = 0;
            Length = (uint)raw.Length;
        }
        /// <summary>
        /// Creates new IFR raw data block.
        /// Sets initially Offset to 0 and Length according to given raw data length
        /// </summary>
        /// <param name="raw">Data this block should refer</param>
        /// <param name="offset">Starting offset where payload resides</param>
        /// <param name="length">Amount of paylod's data bytes</param>
        public IfrRawDataBlock(byte[] raw, uint offset, uint length)
        {
            Bytes = raw;
            Offset = offset;
            Length = length;
        }
        /// <summary>
        /// Creates new IFR raw data block.
        /// Copy constructur
        /// </summary>
        /// <param name="origin">Instance from which data is copied to this new instance</param>
        public IfrRawDataBlock(IfrRawDataBlock origin)
        {
            Bytes = origin.Bytes;
            Offset = origin.Offset;
            Length = origin.Length;
        }
        #endregion

        #region CopyOf[type] methods
        /// <summary>
        /// Copies the whole selected buffer at the current data position
        /// </summary>
        /// <returns>Byte array</returns>
        public byte[] CopyOfSelectedBytes { get { return Bytes.SubArray((int)Offset, (int)Length); } }

        /// <summary>
        /// Retrieves a null terminated ASCII string from the current data position
        /// </summary>
        /// <returns>Managed string filled with corresponding raw data (without NULL)</returns>
        public string CopyOfAsciiNullTerminatedString
        {
            get
            {
                const int startindex = 0;

                int IdxStart = (int)(Offset + startindex);
                int IdxEnd = IdxStart + (int)Length;
                int IdxNull = -1;
                for (int i = IdxStart; i < IdxEnd; i++)
                {
                    if (Bytes[i] == '\0') // null terminated string
                    {
                        IdxNull = i;
                        break;
                    }
                }

                if (IdxNull == -1)
                    throw new Exception("Expected string is not NULL terminated!");

                return System.Text.Encoding.ASCII.GetString(Bytes.SubArray(IdxStart, IdxNull - IdxStart)); ;
            }
        }

        /// <summary>
        /// Retrieves a null terminated Unicode string from the current data position
        /// </summary>
        /// <returns>Managed string filled with corresponding raw data (without NULL)</returns>
        public string CopyOfUnicodeNullTerminatedString
        {
            get
            {
                const int startindex = 0;

                int IdxStart = (int)(Offset + startindex);
                int IdxEnd = IdxStart + (int)Length;
                int IdxNull = -1;
                for (int i = IdxStart; i < IdxEnd - 1; i += 2)
                {
                    if ((Bytes[i] == '\0') && (Bytes[i+1] == '\0')) // null terminated string
                    {
                        IdxNull = i;
                        break;
                    }
                }

                if (IdxNull == -1)
                    throw new Exception("Expected string is not NULL terminated!");

                return System.Text.Encoding.Unicode.GetString(Bytes.SubArray(IdxStart, IdxNull - IdxStart)); ;
            }
        }
        #endregion

        #region Other methods
        /// <summary>
        /// Changes Offset and Length accordingly. Cheks for out of bounds.
        /// </summary>
        /// <param name="amount">Amount of bytes to skip</param>
        public void IncreaseOffset(uint amount)
        {
            if (Length < amount)
                throw new Exception("Out of bounds!");
            Offset += amount;
            Length -= amount;
        }

        /// <summary>
        /// Creates a managed object at the current data position
        /// </summary>
        /// <typeparam name="T">Type of managed structure</typeparam>
        /// <param name="startindex">Additional starting offset</param>
        /// <returns>Managed structure filled with corresponding raw data</returns>
        public T ToIfrType<T>(uint startindex = 0)
        {
            // Sanity check of broken structs..
            if (0 == typeof(T).StructLayoutAttribute.Size)
                throw new Exception("Hey dev, assign structure size for \"" + typeof(T).ToString() + "\" in order to allow size checking!");

            // Pin the buffer and copy structure into managed type..
            int PtrOffset = (int)(Offset + startindex);
            GCHandle handle = GCHandle.Alloc(Bytes, GCHandleType.Pinned);
            T result = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject() + PtrOffset, typeof(T));
            handle.Free();

            // Check if casted structure is within our memory
            if (Length < typeof(T).StructLayoutAttribute.Size)
                throw new Exception("Data has " + Length + " bytes left. Casting to " + typeof(T).StructLayoutAttribute.Size  + " bytes long \"" + typeof(T).ToString() + "\" failed!");

            return result;
        }
        #endregion

        #region Debug methods
        /// <summary>
        /// Dumps the selection of IFR raw data block to log window
        /// </summary>
        /// <param name="title">Firendly name of the dumped object</param>
        /// <param name="bytesPerLine">Amount of bytes shown at a single line</param>
        /// <returns>Generated message</returns>
        public string GenerateAndLogDump(string title = "Unnamed", uint bytesPerLine = 16)
        {
            string message = "Data \"" + title + "\" dumped (Offset=" + Offset + ", Length=" + Length + "):" + Environment.NewLine + CopyOfSelectedBytes.HexDump(bytesPerLine);
            IFRHelper.CreateLogEntry(LogSeverity.INFO, "Debug", message);
            return message;
        }
        #endregion
    }

    /// <summary>
    /// Severity for console messages
    /// </summary>
    public enum LogSeverity { INFO, SUCCESS, STATUS, WARNING, ERROR, UNIMPLEMENTED };

    static class IFRHelper
    {
        #region Methods for generic types
        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        public static uint GetPhysSize<T>(this T s) where T : struct
        {
            uint result = (uint)s.GetType().StructLayoutAttribute.Size;

            // Sanity check of broken structs..
            if (0 == result)
                throw new Exception("Hey dev, get your structure size of \"" + typeof(T).ToString() + "\" fixed!");

            return result;
        }
        #endregion

        #region Methods specific for byte arrays
        /// <summary>
        /// Dumps a byte array to debugger console
        /// </summary>
        /// <param name="data">Data that shall be dumped</param>
        /// <param name="bytesPerLine">Amount of bytes shown at a single line</param>
        public static void DumpToDebugConsole(this byte[] bytes, uint bytesPerLine = 16)
        {
            System.Console.WriteLine(bytes.HexDump(bytesPerLine));
        }
        /// <summary>
        /// Dumps a byte array
        /// (see: http://stackoverflow.com/questions/26206257/packet-dump-hex-view-format-for-byte-to-text-file)
        /// </summary>
        /// <param name="bytes">Byte array to be dumped</param>
        /// <param name="bytesPerLine">Amount of bytes shown at a single line</param>
        /// <returns>String holding the hexdump</returns>
        public static string HexDump(this byte[] bytes, uint BytesPerLine = 16)
        {
            int bytesPerLine = (int)BytesPerLine;

            if (bytes == null) return "<null>";
            int bytesLength = bytes.Length;

            char[] HexChars = "0123456789ABCDEF".ToCharArray();

            int firstHexColumn =
                  8                   // 8 characters for the address
                + 3;                  // 3 spaces

            int firstCharColumn = firstHexColumn
                + bytesPerLine * 3       // - 2 digit for the hexadecimal value and 1 space
                + (bytesPerLine - 1) / 8 // - 1 extra space every 8 characters from the 9th
                + 2;                  // 2 spaces 

            int lineLength = firstCharColumn
                + bytesPerLine           // - characters to show the ascii value
                + Environment.NewLine.Length; // Carriage return and line feed (should normally be 2)

            char[] line = (new String(' ', lineLength - 2) + Environment.NewLine).ToCharArray();
            int expectedLines = (bytesLength + bytesPerLine - 1) / bytesPerLine;
            System.Text.StringBuilder result = new System.Text.StringBuilder(expectedLines * lineLength);

            for (int i = 0; i < bytesLength; i += bytesPerLine)
            {
                line[0] = HexChars[(i >> 28) & 0xF];
                line[1] = HexChars[(i >> 24) & 0xF];
                line[2] = HexChars[(i >> 20) & 0xF];
                line[3] = HexChars[(i >> 16) & 0xF];
                line[4] = HexChars[(i >> 12) & 0xF];
                line[5] = HexChars[(i >> 8) & 0xF];
                line[6] = HexChars[(i >> 4) & 0xF];
                line[7] = HexChars[(i >> 0) & 0xF];

                int hexColumn = firstHexColumn;
                int charColumn = firstCharColumn;

                for (int j = 0; j < bytesPerLine; j++)
                {
                    if (j > 0 && (j & 7) == 0) hexColumn++;
                    if (i + j >= bytesLength)
                    {
                        line[hexColumn] = ' ';
                        line[hexColumn + 1] = ' ';
                        line[charColumn] = ' ';
                    }
                    else
                    {
                        byte b = bytes[i + j];
                        line[hexColumn] = HexChars[(b >> 4) & 0xF];
                        line[hexColumn + 1] = HexChars[b & 0xF];
                        line[charColumn] = (b < 32 ? '·' : (char)b);
                    }
                    hexColumn += 3;
                    charColumn++;
                }
                result.Append(line);
            }
            return result.ToString();
        }
        #endregion

        #region Methods specific for numeric bitmasks
        private static T _internal_GetBits<T>(UINT64 Value, UINT64 BitMask = UINT64.MaxValue, UINT8 ShiftedOffset = 0)
        {
            return (T)(dynamic)((Value >> ShiftedOffset) & BitMask);
        }
        #region GetBits[Type] wrappers
        /// <summary>
        /// Gets a typecasted value from a bit mask represented by the object's numeric value.
        /// Origin's value gets shifted first and then masked.
        /// If you plan to keep bit position alive, simply use BitMask only.
        /// </summary>
        /// <typeparam name="T">Type for typecasted value</typeparam>
        /// <param name="Value">The objects numeric value</param>
        /// <param name="BitMask">Mask of relevant bits for value, default = 0xFFFFFFFFFFFFFFFF</param>
        /// <param name="ShiftedOffset">Amount of bits shifted, default = 0</param>
        /// <returns>Value of selected bits</returns>
        public static T GetBits<T>(this UINT64 Value, UINT64 BitMask = UINT64.MaxValue, UINT8 ShiftedOffset = 0) where T : IConvertible
        {
            return _internal_GetBits<T>(Value, BitMask, ShiftedOffset);
        }
        /// <summary>
        /// Gets a typecasted value from a bit mask represented by the object's numeric value.
        /// Origin's value gets shifted first and then masked.
        /// If you plan to keep bit position alive, simply use BitMask only.
        /// </summary>
        /// <typeparam name="T">Type for typecasted value</typeparam>
        /// <param name="Value">The objects numeric value</param>
        /// <param name="BitMask">Mask of relevant bits for value, default = 0xFFFFFFFFFFFFFFFF</param>
        /// <param name="ShiftedOffset">Amount of bits shifted, default = 0</param>
        /// <returns>Value of selected bits</returns>
        public static T GetBits<T>(this UINT32 Value, UINT32 BitMask = UINT32.MaxValue, UINT8 ShiftedOffset = 0) where T : IConvertible
        {
            return _internal_GetBits<T>(Value, BitMask, ShiftedOffset);
        }
        /// <summary>
        /// Gets a typecasted value from a bit mask represented by the object's numeric value.
        /// Origin's value gets shifted first and then masked.
        /// If you plan to keep bit position alive, simply use BitMask only.
        /// </summary>
        /// <typeparam name="T">Type for typecasted value</typeparam>
        /// <param name="Value">The objects numeric value</param>
        /// <param name="BitMask">Mask of relevant bits for value, default = 0xFFFFFFFFFFFFFFFF</param>
        /// <param name="ShiftedOffset">Amount of bits shifted, default = 0</param>
        /// <returns>Value of selected bits</returns>
        public static T GetBits<T>(this UINT16 Value, UINT16 BitMask = UINT16.MaxValue, UINT8 ShiftedOffset = 0) where T : IConvertible
        {
            return _internal_GetBits<T>(Value, BitMask, ShiftedOffset);
        }
        /// <summary>
        /// Gets a typecasted value from a bit mask represented by the object's numeric value.
        /// Origin's value gets shifted first and then masked.
        /// If you plan to keep bit position alive, simply use BitMask only.
        /// </summary>
        /// <typeparam name="T">Type for typecasted value</typeparam>
        /// <param name="Value">The objects numeric value</param>
        /// <param name="BitMask">Mask of relevant bits for value, default = 0xFFFFFFFFFFFFFFFF</param>
        /// <param name="ShiftedOffset">Amount of bits shifted, default = 0</param>
        /// <returns>Value of selected bits</returns>
        public static T GetBits<T>(this UINT8 Value, UINT8 BitMask = UINT8.MaxValue, UINT8 ShiftedOffset = 0) where T : IConvertible
        {
            return _internal_GetBits<T>(Value, BitMask, ShiftedOffset);
        }
        #endregion

        private static T _internal_SetBits<T>(UINT64 Value, UINT64 NewValue, UINT64 BitMask = UINT64.MaxValue, UINT8 ShiftedOffset = 0)
        {
            return (T)(dynamic)((Value & ~(BitMask << ShiftedOffset)) | ((NewValue & BitMask) << ShiftedOffset));
        }
        #region SetBits[Type] wrappers
        /// <summary>
        /// Sets bits of the object's numeric value to a typecasted numeric value in the masked bitfield.
        /// Given value gets shifted first and then masked.
        /// If you plan to keep bit position alive, simply use BitMask only.
        /// </summary>
        /// <typeparam name="T">Type of given numeric value</typeparam>
        /// <param name="OldValue">The objects numeric value</param>
        /// <param name="NewValue">Value of given type to set</param>
        /// <param name="BitMask">Mask of relevant bits for value, default = 0xFFFFFFFFFFFFFFFF</param>
        /// <param name="ShiftedOffset">Amount of bits shifted, default = 0</param>
        public static UINT64 SetBits<T>(UINT64 OldValue, T NewValue, UINT64 BitMask = UINT64.MaxValue, UINT8 ShiftedOffset = 0) where T : IConvertible
        {
            return _internal_SetBits<UINT64>(OldValue, (UINT64)(dynamic)NewValue, BitMask, ShiftedOffset);
        }
        /// <summary>
        /// Sets bits of the object's numeric value to a typecasted numeric value in the masked bitfield.
        /// Given value gets shifted first and then masked.
        /// If you plan to keep bit position alive, simply use BitMask only.
        /// </summary>
        /// <typeparam name="T">Type of given numeric value</typeparam>
        /// <param name="OldValue">The objects numeric value</param>
        /// <param name="NewValue">Value of given type to set</param>
        /// <param name="BitMask">Mask of relevant bits for value, default = 0xFFFFFFFFFFFFFFFF</param>
        /// <param name="ShiftedOffset">Amount of bits shifted, default = 0</param>
        public static UINT32 SetBits<T>(UINT32 OldValue, T NewValue, UINT32 BitMask = UINT32.MaxValue, UINT8 ShiftedOffset = 0) where T : IConvertible
        {
            return _internal_SetBits<UINT32>(OldValue, (UINT64)(dynamic)NewValue, BitMask, ShiftedOffset);
        }
        /// <summary>
        /// Sets bits of the object's numeric value to a typecasted numeric value in the masked bitfield.
        /// Given value gets shifted first and then masked.
        /// If you plan to keep bit position alive, simply use BitMask only.
        /// </summary>
        /// <typeparam name="T">Type of given numeric value</typeparam>
        /// <param name="OldValue">The objects numeric value</param>
        /// <param name="NewValue">Value of given type to set</param>
        /// <param name="BitMask">Mask of relevant bits for value, default = 0xFFFFFFFFFFFFFFFF</param>
        /// <param name="ShiftedOffset">Amount of bits shifted, default = 0</param>
        public static UINT16 SetBits<T>(UINT16 OldValue, T NewValue, UINT16 BitMask = UINT16.MaxValue, UINT8 ShiftedOffset = 0) where T : IConvertible
        {
            return _internal_SetBits<UINT16>(OldValue, (UINT64)(dynamic)NewValue, BitMask, ShiftedOffset);
        }
        /// <summary>
        /// Sets bits of the object's numeric value to a typecasted numeric value in the masked bitfield.
        /// Given value gets shifted first and then masked.
        /// If you plan to keep bit position alive, simply use BitMask only.
        /// </summary>
        /// <typeparam name="T">Type of given numeric value</typeparam>
        /// <param name="OldValue">The objects numeric value</param>
        /// <param name="NewValue">Value of given type to set</param>
        /// <param name="BitMask">Mask of relevant bits for value, default = 0xFFFFFFFFFFFFFFFF</param>
        /// <param name="ShiftedOffset">Amount of bits shifted, default = 0</param>
        public static UINT8 SetBits<T>(UINT8 OldValue, T NewValue, UINT8 BitMask = UINT8.MaxValue, UINT8 ShiftedOffset = 0) where T : IConvertible
        {
            return _internal_SetBits<UINT8>(OldValue, (UINT64)(dynamic)NewValue, BitMask, ShiftedOffset);
        }
        #endregion
        #endregion

        #region Debug and console functions
        /// <summary>
        /// Debug window which is used to print debug messages
        /// </summary>
        public static DataGridView log = null;

        /// <summary>
        /// Generates a logged message and adds it to the logging window
        /// </summary>
        /// <param name="severity">Severity of message</param>
        /// <param name="origin">Short origin name of message (to group messges)</param>
        /// <param name="msg">Message string</param>
        /// <param name="bShowMsgBox">Shows message box when true</param>
        public static void CreateLogEntry(LogSeverity severity, string origin, string msg, bool bShowMsgBox = false)
        {
            string typename = severity.ToString();

            Color color;
            MessageBoxIcon Icon;
            switch (severity)
            {
                case LogSeverity.SUCCESS: color = Color.LimeGreen; Icon = MessageBoxIcon.None; break;
                case LogSeverity.STATUS: color = Color.LightGreen; Icon = MessageBoxIcon.Information; break;
                case LogSeverity.WARNING: color = Color.LightCoral; Icon = MessageBoxIcon.Warning; break;
                case LogSeverity.ERROR: color = Color.OrangeRed; Icon = MessageBoxIcon.Error; break;
                case LogSeverity.UNIMPLEMENTED: color = Color.HotPink; Icon = MessageBoxIcon.Exclamation; break;
                //case LogSeverity.INFO:
                default: color = Color.White; Icon = MessageBoxIcon.None; break;
            }

            //Is debug window connected?
            if (log != null)
            {
                // print debug message and assign color
                log.Rows[log.Rows.Add(new object[]{ typename, origin, msg.Replace(Environment.NewLine, Environment.NewLine + " > ") })].SetRowBackgroundColor(color);
                log.AutoResizeColumns();
                log.AutoResizeRow(log.Rows.Count - 1);
            }

            // Show error in message box?
            if (bShowMsgBox) MessageBox.Show(msg, origin, MessageBoxButtons.OK, Icon);
        }

        public static void SetRowBackgroundColor(this DataGridViewRow row, Color color)
        {
            foreach (DataGridViewCell col in row.Cells)
                col.Style.BackColor = color;
        }
        #endregion
    }
    #endregion

    #region Definitions for Package Lists and Package Headers (Section 27.3.1)
    /*
        ///
        /// The header found at the start of each package list.
        ///
        typedef struct {
              EFI_GUID PackageListGuid;
            UINT32 PackageLength;
        }
        EFI_HII_PACKAGE_LIST_HEADER;
        */
    /// <summary>
    /// The header found at the start of each package.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode, Pack=1, Size=4)]
    struct EFI_HII_PACKAGE_HEADER
    {
        [FieldOffset(0)]
        private UINT32 _Length;
        [FieldOffset(3)]
        private UINT8 _Type;
        // UINT8  Data[...];

        public UINT32 Length { get { return _Length.GetBits<UINT32>(0x00FFFFFF); } set { _Length = SetBits<UINT32>(_Length, value, 0x00FFFFFF); } }
        public EFI_HII_PACKAGE_e Type { get { return _Type.GetBits<EFI_HII_PACKAGE_e>(); } set { _Type = SetBits(_Type, value); } }
    };

    /// <summary>
    /// Value of HII package type
    /// </summary>
    public enum EFI_HII_PACKAGE_e
    {
        EFI_HII_PACKAGE_TYPE_ALL = 0x00,
        EFI_HII_PACKAGE_TYPE_GUID = 0x01,
        EFI_HII_PACKAGE_FORMS = 0x02,
        EFI_HII_PACKAGE_STRINGS = 0x04,
        EFI_HII_PACKAGE_FONTS = 0x05,
        EFI_HII_PACKAGE_IMAGES = 0x06,
        EFI_HII_PACKAGE_SIMPLE_FONTS = 0x07,
        EFI_HII_PACKAGE_DEVICE_PATH = 0x08,
        EFI_HII_PACKAGE_KEYBOARD_LAYOUT = 0x09,
        EFI_HII_PACKAGE_ANIMATIONS = 0x0A,
        EFI_HII_PACKAGE_END = 0xDF,
        EFI_HII_PACKAGE_TYPE_SYSTEM_BEGIN = 0xE0,
        EFI_HII_PACKAGE_TYPE_SYSTEM_END = 0xFF,
    };
    #endregion

    #region Definitions for Simplified Font Package
    /*
        ///
        /// Contents of EFI_NARROW_GLYPH.Attributes.
        ///@{
        #define EFI_GLYPH_NON_SPACING                0x01
        #define EFI_GLYPH_WIDE                       0x02
        #define EFI_GLYPH_HEIGHT                     19
        #define EFI_GLYPH_WIDTH                      8
        ///@}

        ///
        /// The EFI_NARROW_GLYPH has a preferred dimension (w x h) of 8 x 19 pixels.
        ///
        typedef struct {
          ///
          /// The Unicode representation of the glyph. The term weight is the 
          /// technical term for a character code.
          ///
    CHAR16 UnicodeWeight;
    ///
    /// The data element containing the glyph definitions.
    ///
    UINT8 Attributes;
    ///
    /// The column major glyph representation of the character. Bits 
    /// with values of one indicate that the corresponding pixel is to be
    /// on when normally displayed; those with zero are off.
    ///
    UINT8 GlyphCol1[EFI_GLYPH_HEIGHT];
        } EFI_NARROW_GLYPH;

        ///
        /// The EFI_WIDE_GLYPH has a preferred dimension (w x h) of 16 x 19 pixels, which is large enough 
        /// to accommodate logographic characters.
        ///
        typedef struct {
          ///
          /// The Unicode representation of the glyph. The term weight is the 
          /// technical term for a character code.
          ///
    CHAR16 UnicodeWeight;
    ///
    /// The data element containing the glyph definitions.
    ///
    UINT8 Attributes;
    ///
    /// The column major glyph representation of the character. Bits 
    /// with values of one indicate that the corresponding pixel is to be 
    /// on when normally displayed; those with zero are off.
    ///
    UINT8 GlyphCol1[EFI_GLYPH_HEIGHT];
    ///
    /// The column major glyph representation of the character. Bits 
    /// with values of one indicate that the corresponding pixel is to be 
    /// on when normally displayed; those with zero are off.
    ///
    UINT8 GlyphCol2[EFI_GLYPH_HEIGHT];
    ///
    /// Ensures that sizeof (EFI_WIDE_GLYPH) is twice the 
    /// sizeof (EFI_NARROW_GLYPH). The contents of Pad must 
    /// be zero.
    ///
    UINT8 Pad[3];
        } EFI_WIDE_GLYPH;

        ///
        /// A simplified font package consists of a font header
        /// followed by a series of glyph structures.
        ///
        typedef struct _EFI_HII_SIMPLE_FONT_PACKAGE_HDR
    {
        EFI_HII_PACKAGE_HEADER Header;
        UINT16 NumberOfNarrowGlyphs;
        UINT16 NumberOfWideGlyphs;
        // EFI_NARROW_GLYPH       NarrowGlyphs[];
        // EFI_WIDE_GLYPH         WideGlyphs[];
    }
    EFI_HII_SIMPLE_FONT_PACKAGE_HDR;

        //
        // Definitions for Font Package
        // Section 27.3.3
        //

        //
        // Value for font style
        //
        #define EFI_HII_FONT_STYLE_NORMAL            0x00000000
        #define EFI_HII_FONT_STYLE_BOLD              0x00000001
        #define EFI_HII_FONT_STYLE_ITALIC            0x00000002
        #define EFI_HII_FONT_STYLE_EMBOSS            0x00010000
        #define EFI_HII_FONT_STYLE_OUTLINE           0x00020000
        #define EFI_HII_FONT_STYLE_SHADOW            0x00040000
        #define EFI_HII_FONT_STYLE_UNDERLINE         0x00080000
        #define EFI_HII_FONT_STYLE_DBL_UNDER         0x00100000

        typedef struct _EFI_HII_GLYPH_INFO
    {
        UINT16 Width;
        UINT16 Height;
        INT16 OffsetX;
        INT16 OffsetY;
        INT16 AdvanceX;
    }
    EFI_HII_GLYPH_INFO;

        ///
        /// The fixed header consists of a standard record header,
        /// then the character values in this section, the flags
        /// (including the encoding method) and the offsets of the glyph
        /// information, the glyph bitmaps and the character map.
        ///
        typedef struct _EFI_HII_FONT_PACKAGE_HDR
    {
        EFI_HII_PACKAGE_HEADER Header;
        UINT32 HdrSize;
        UINT32 GlyphBlockOffset;
        EFI_HII_GLYPH_INFO Cell;
        EFI_HII_FONT_STYLE FontStyle;
        CHAR16 FontFamily[1];
    }
    EFI_HII_FONT_PACKAGE_HDR;

        //
        // Value of different glyph info block types
        //
        #define EFI_HII_GIBT_END                  0x00
        #define EFI_HII_GIBT_GLYPH                0x10
        #define EFI_HII_GIBT_GLYPHS               0x11
        #define EFI_HII_GIBT_GLYPH_DEFAULT        0x12
        #define EFI_HII_GIBT_GLYPHS_DEFAULT       0x13
        #define EFI_HII_GIBT_DUPLICATE            0x20
        #define EFI_HII_GIBT_SKIP2                0x21
        #define EFI_HII_GIBT_SKIP1                0x22
        #define EFI_HII_GIBT_DEFAULTS             0x23
        #define EFI_HII_GIBT_EXT1                 0x30
        #define EFI_HII_GIBT_EXT2                 0x31
        #define EFI_HII_GIBT_EXT4                 0x32

        typedef struct _EFI_HII_GLYPH_BLOCK
    {
        UINT8 BlockType;
    }
    EFI_HII_GLYPH_BLOCK;

        //
        // Definition of different glyph info block types
        //

        typedef struct _EFI_HII_GIBT_DEFAULTS_BLOCK
    {
        EFI_HII_GLYPH_BLOCK Header;
        EFI_HII_GLYPH_INFO Cell;
    }
    EFI_HII_GIBT_DEFAULTS_BLOCK;

        typedef struct _EFI_HII_GIBT_DUPLICATE_BLOCK
    {
        EFI_HII_GLYPH_BLOCK Header;
        CHAR16 CharValue;
    }
    EFI_HII_GIBT_DUPLICATE_BLOCK;

        typedef struct _EFI_GLYPH_GIBT_END_BLOCK
    {
        EFI_HII_GLYPH_BLOCK Header;
    }
    EFI_GLYPH_GIBT_END_BLOCK;

        typedef struct _EFI_HII_GIBT_EXT1_BLOCK
    {
        EFI_HII_GLYPH_BLOCK Header;
        UINT8 BlockType2;
        UINT8 Length;
    }
    EFI_HII_GIBT_EXT1_BLOCK;

        typedef struct _EFI_HII_GIBT_EXT2_BLOCK
    {
        EFI_HII_GLYPH_BLOCK Header;
        UINT8 BlockType2;
        UINT16 Length;
    }
    EFI_HII_GIBT_EXT2_BLOCK;

        typedef struct _EFI_HII_GIBT_EXT4_BLOCK
    {
        EFI_HII_GLYPH_BLOCK Header;
        UINT8 BlockType2;
        UINT32 Length;
    }
    EFI_HII_GIBT_EXT4_BLOCK;

        typedef struct _EFI_HII_GIBT_GLYPH_BLOCK
    {
        EFI_HII_GLYPH_BLOCK Header;
        EFI_HII_GLYPH_INFO Cell;
        UINT8 BitmapData[1];
    }
    EFI_HII_GIBT_GLYPH_BLOCK;

        typedef struct _EFI_HII_GIBT_GLYPHS_BLOCK
    {
        EFI_HII_GLYPH_BLOCK Header;
        EFI_HII_GLYPH_INFO Cell;
        UINT16 Count;
        UINT8 BitmapData[1];
    }
    EFI_HII_GIBT_GLYPHS_BLOCK;

        typedef struct _EFI_HII_GIBT_GLYPH_DEFAULT_BLOCK
    {
        EFI_HII_GLYPH_BLOCK Header;
        UINT8 BitmapData[1];
    }
    EFI_HII_GIBT_GLYPH_DEFAULT_BLOCK;

        typedef struct _EFI_HII_GIBT_GLYPHS_DEFAULT_BLOCK
    {
        EFI_HII_GLYPH_BLOCK Header;
        UINT16 Count;
        UINT8 BitmapData[1];
    }
    EFI_HII_GIBT_GLYPHS_DEFAULT_BLOCK;

        typedef struct _EFI_HII_GIBT_SKIP1_BLOCK
    {
        EFI_HII_GLYPH_BLOCK Header;
        UINT8 SkipCount;
    }
    EFI_HII_GIBT_SKIP1_BLOCK;

        typedef struct _EFI_HII_GIBT_SKIP2_BLOCK
    {
        EFI_HII_GLYPH_BLOCK Header;
        UINT16 SkipCount;
    }
    EFI_HII_GIBT_SKIP2_BLOCK;
    */
    #endregion

    #region Definitions for Device Path Package (Section 27.3.4)
    /*
        ///
        /// The device path package is used to carry a device path
        /// associated with the package list.
        ///
        typedef struct _EFI_HII_DEVICE_PATH_PACKAGE_HDR
    {
        EFI_HII_PACKAGE_HEADER Header;
        // EFI_DEVICE_PATH_PROTOCOL DevicePath[];
    }
    EFI_HII_DEVICE_PATH_PACKAGE_HDR;
    */
    #endregion

    #region Definitions for GUID Package (Section 27.3.5)
    /*
    ///
    /// The GUID package is used to carry data where the format is defined by a GUID.
    ///
    typedef struct _EFI_HII_GUID_PACKAGE_HDR
    {
    EFI_HII_PACKAGE_HEADER Header;
    EFI_GUID Guid;
    // Data per GUID definition may follow
    }
    EFI_HII_GUID_PACKAGE_HDR;
    */
    #endregion

    #region Definitions for String Package (Section 27.3.6)
    /*
    #define UEFI_CONFIG_LANG   "x-UEFI"
    #define UEFI_CONFIG_LANG_2 "x-i-UEFI"
        */
    /// <summary>
    /// The fixed header consists of a standard record header and then the string identifiers
    /// contained in this section and the offsets of the string and language information.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 46)]
    struct EFI_HII_STRING_PACKAGE_HDR
    {
        public EFI_HII_PACKAGE_HEADER Header;
        public UINT32 HdrSize;
        public UINT32 StringInfoOffset;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public CHAR16[] LanguageWindow;
        public EFI_STRING_ID LanguageName;
        // CHAR8 Language[...];
        // EFI_HII_STRING_BLOCK Blocks[...];
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 1)]
    struct EFI_HII_STRING_BLOCK
    {
        private UINT8 _BlockType;
        // UINT8 BlockBody[...];

        public EFI_HII_SIBT_e BlockType { get { return _BlockType.GetBits<EFI_HII_SIBT_e>(); } set { _BlockType = SetBits(_BlockType, value); } }
    }

    /// <summary>
    /// Value of different string information block types
    /// </summary>
    enum EFI_HII_SIBT_e
    {
        EFI_HII_SIBT_END = 0x00,
        EFI_HII_SIBT_STRING_SCSU = 0x10,
        EFI_HII_SIBT_STRING_SCSU_FONT = 0x11,
        EFI_HII_SIBT_STRINGS_SCSU = 0x12,
        EFI_HII_SIBT_STRINGS_SCSU_FONT = 0x13,
        EFI_HII_SIBT_STRING_UCS2 = 0x14,
        EFI_HII_SIBT_STRING_UCS2_FONT = 0x15,
        EFI_HII_SIBT_STRINGS_UCS2 = 0x16,
        EFI_HII_SIBT_STRINGS_UCS2_FONT = 0x17,
        EFI_HII_SIBT_DUPLICATE = 0x20,
        EFI_HII_SIBT_SKIP2 = 0x21,
        EFI_HII_SIBT_SKIP1 = 0x22,
        EFI_HII_SIBT_EXT1 = 0x30,
        EFI_HII_SIBT_EXT2 = 0x31,
        EFI_HII_SIBT_EXT4 = 0x32,
        EFI_HII_SIBT_FONT = 0x40,
    };
    /*
            //
            // Definition of different string information block types
            //

            typedef struct _EFI_HII_SIBT_DUPLICATE_BLOCK
        {
            EFI_HII_STRING_BLOCK Header;
            EFI_STRING_ID StringId;
        }
        EFI_HII_SIBT_DUPLICATE_BLOCK;
*/
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 3)]
    struct EFI_HII_SIBT_EXT1_BLOCK
    {
        public EFI_HII_STRING_BLOCK Header;
        public UINT8 BlockType2;
        public UINT8 Length;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 4)]
    struct EFI_HII_SIBT_EXT2_BLOCK
    {
        public EFI_HII_STRING_BLOCK Header;
        public UINT8 BlockType2;
        public UINT16 Length;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 6)]
    struct EFI_HII_SIBT_EXT4_BLOCK
    {
        public EFI_HII_STRING_BLOCK Header;
        public UINT8 BlockType2;
        public UINT32 Length;
    }
/*
            typedef struct _EFI_HII_SIBT_FONT_BLOCK
        {
            EFI_HII_SIBT_EXT2_BLOCK Header;
            UINT8 FontId;
            UINT16 FontSize;
            EFI_HII_FONT_STYLE FontStyle;
            CHAR16 FontName[1];
        }
        EFI_HII_SIBT_FONT_BLOCK;
*/
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 2)]
    struct EFI_HII_SIBT_SKIP1_BLOCK
    {
        public EFI_HII_STRING_BLOCK Header;
        public UINT8 SkipCount;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 3)]
    struct EFI_HII_SIBT_SKIP2_BLOCK
    {
        public EFI_HII_STRING_BLOCK Header;
        public UINT16 SkipCount;
    }
/*
            typedef struct _EFI_HII_SIBT_STRING_SCSU_BLOCK
        {
            EFI_HII_STRING_BLOCK Header;
            UINT8 StringText[1];
        }
        EFI_HII_SIBT_STRING_SCSU_BLOCK;

            typedef struct _EFI_HII_SIBT_STRING_SCSU_FONT_BLOCK
        {
            EFI_HII_STRING_BLOCK Header;
            UINT8 FontIdentifier;
            UINT8 StringText[1];
        }
        EFI_HII_SIBT_STRING_SCSU_FONT_BLOCK;

            typedef struct _EFI_HII_SIBT_STRINGS_SCSU_BLOCK
        {
            EFI_HII_STRING_BLOCK Header;
            UINT16 StringCount;
            UINT8 StringText[1];
        }
        EFI_HII_SIBT_STRINGS_SCSU_BLOCK;

            typedef struct _EFI_HII_SIBT_STRINGS_SCSU_FONT_BLOCK
        {
            EFI_HII_STRING_BLOCK Header;
            UINT8 FontIdentifier;
            UINT16 StringCount;
            UINT8 StringText[1];
        }
        EFI_HII_SIBT_STRINGS_SCSU_FONT_BLOCK;
*/
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 1)]
    struct EFI_HII_SIBT_STRING_UCS2_BLOCK
    {
        public EFI_HII_STRING_BLOCK Header;
        // CHAR16 StringText[...];
    }
/*
            typedef struct _EFI_HII_SIBT_STRING_UCS2_BLOCK
        {
            EFI_HII_STRING_BLOCK Header;
            CHAR16 StringText[1];
        }
        EFI_HII_SIBT_STRING_UCS2_BLOCK;

            typedef struct _EFI_HII_SIBT_STRING_UCS2_FONT_BLOCK
        {
            EFI_HII_STRING_BLOCK Header;
            UINT8 FontIdentifier;
            CHAR16 StringText[1];
        }
        EFI_HII_SIBT_STRING_UCS2_FONT_BLOCK;

            typedef struct _EFI_HII_SIBT_STRINGS_UCS2_BLOCK
        {
            EFI_HII_STRING_BLOCK Header;
            UINT16 StringCount;
            CHAR16 StringText[1];
        }
        EFI_HII_SIBT_STRINGS_UCS2_BLOCK;

            typedef struct _EFI_HII_SIBT_STRINGS_UCS2_FONT_BLOCK
        {
            EFI_HII_STRING_BLOCK Header;
            UINT8 FontIdentifier;
            UINT16 StringCount;
            CHAR16 StringText[1];
        }
        EFI_HII_SIBT_STRINGS_UCS2_FONT_BLOCK;
        */
    #endregion

    #region Definitions for Image Package(Section 27.3.7)
    /*
        typedef struct _EFI_HII_IMAGE_PACKAGE_HDR
    {
        EFI_HII_PACKAGE_HEADER Header;
        UINT32 ImageInfoOffset;
        UINT32 PaletteInfoOffset;
    }
    EFI_HII_IMAGE_PACKAGE_HDR;

        typedef struct _EFI_HII_IMAGE_BLOCK
    {
        UINT8 BlockType;
    }
    EFI_HII_IMAGE_BLOCK;

        //
        // Value of different image information block types
        //
        #define EFI_HII_IIBT_END               0x00
        #define EFI_HII_IIBT_IMAGE_1BIT        0x10
        #define EFI_HII_IIBT_IMAGE_1BIT_TRANS  0x11
        #define EFI_HII_IIBT_IMAGE_4BIT        0x12
        #define EFI_HII_IIBT_IMAGE_4BIT_TRANS  0x13
        #define EFI_HII_IIBT_IMAGE_8BIT        0x14
        #define EFI_HII_IIBT_IMAGE_8BIT_TRANS  0x15
        #define EFI_HII_IIBT_IMAGE_24BIT       0x16
        #define EFI_HII_IIBT_IMAGE_24BIT_TRANS 0x17
        #define EFI_HII_IIBT_IMAGE_JPEG        0x18
        #define EFI_HII_IIBT_DUPLICATE         0x20
        #define EFI_HII_IIBT_SKIP2             0x21
        #define EFI_HII_IIBT_SKIP1             0x22
        #define EFI_HII_IIBT_EXT1              0x30
        #define EFI_HII_IIBT_EXT2              0x31
        #define EFI_HII_IIBT_EXT4              0x32

        //
        // Definition of different image information block types
        //

        typedef struct _EFI_HII_IIBT_END_BLOCK
    {
        EFI_HII_IMAGE_BLOCK Header;
    }
    EFI_HII_IIBT_END_BLOCK;

        typedef struct _EFI_HII_IIBT_EXT1_BLOCK
    {
        EFI_HII_IMAGE_BLOCK Header;
        UINT8 BlockType2;
        UINT8 Length;
    }
    EFI_HII_IIBT_EXT1_BLOCK;

        typedef struct _EFI_HII_IIBT_EXT2_BLOCK
    {
        EFI_HII_IMAGE_BLOCK Header;
        UINT8 BlockType2;
        UINT16 Length;
    }
    EFI_HII_IIBT_EXT2_BLOCK;

        typedef struct _EFI_HII_IIBT_EXT4_BLOCK
    {
        EFI_HII_IMAGE_BLOCK Header;
        UINT8 BlockType2;
        UINT32 Length;
    }
    EFI_HII_IIBT_EXT4_BLOCK;

        typedef struct _EFI_HII_IIBT_IMAGE_1BIT_BASE
    {
        UINT16 Width;
        UINT16 Height;
        UINT8 Data[1];
    }
    EFI_HII_IIBT_IMAGE_1BIT_BASE;

        typedef struct _EFI_HII_IIBT_IMAGE_1BIT_BLOCK
    {
        EFI_HII_IMAGE_BLOCK Header;
        UINT8 PaletteIndex;
        EFI_HII_IIBT_IMAGE_1BIT_BASE Bitmap;
    }
    EFI_HII_IIBT_IMAGE_1BIT_BLOCK;

        typedef struct _EFI_HII_IIBT_IMAGE_1BIT_TRANS_BLOCK
    {
        EFI_HII_IMAGE_BLOCK Header;
        UINT8 PaletteIndex;
        EFI_HII_IIBT_IMAGE_1BIT_BASE Bitmap;
    }
    EFI_HII_IIBT_IMAGE_1BIT_TRANS_BLOCK;

        typedef struct _EFI_HII_RGB_PIXEL
    {
        UINT8 b;
        UINT8 g;
        UINT8 r;
    }
    EFI_HII_RGB_PIXEL;

        typedef struct _EFI_HII_IIBT_IMAGE_24BIT_BASE
    {
        UINT16 Width;
        UINT16 Height;
        EFI_HII_RGB_PIXEL Bitmap[1];
    }
    EFI_HII_IIBT_IMAGE_24BIT_BASE;

        typedef struct _EFI_HII_IIBT_IMAGE_24BIT_BLOCK
    {
        EFI_HII_IMAGE_BLOCK Header;
        EFI_HII_IIBT_IMAGE_24BIT_BASE Bitmap;
    }
    EFI_HII_IIBT_IMAGE_24BIT_BLOCK;

        typedef struct _EFI_HII_IIBT_IMAGE_24BIT_TRANS_BLOCK
    {
        EFI_HII_IMAGE_BLOCK Header;
        EFI_HII_IIBT_IMAGE_24BIT_BASE Bitmap;
    }
    EFI_HII_IIBT_IMAGE_24BIT_TRANS_BLOCK;

        typedef struct _EFI_HII_IIBT_IMAGE_4BIT_BASE
    {
        UINT16 Width;
        UINT16 Height;
        UINT8 Data[1];
    }
    EFI_HII_IIBT_IMAGE_4BIT_BASE;

        typedef struct _EFI_HII_IIBT_IMAGE_4BIT_BLOCK
    {
        EFI_HII_IMAGE_BLOCK Header;
        UINT8 PaletteIndex;
        EFI_HII_IIBT_IMAGE_4BIT_BASE Bitmap;
    }
    EFI_HII_IIBT_IMAGE_4BIT_BLOCK;

        typedef struct _EFI_HII_IIBT_IMAGE_4BIT_TRANS_BLOCK
    {
        EFI_HII_IMAGE_BLOCK Header;
        UINT8 PaletteIndex;
        EFI_HII_IIBT_IMAGE_4BIT_BASE Bitmap;
    }
    EFI_HII_IIBT_IMAGE_4BIT_TRANS_BLOCK;

        typedef struct _EFI_HII_IIBT_IMAGE_8BIT_BASE
    {
        UINT16 Width;
        UINT16 Height;
        UINT8 Data[1];
    }
    EFI_HII_IIBT_IMAGE_8BIT_BASE;

        typedef struct _EFI_HII_IIBT_IMAGE_8BIT_PALETTE_BLOCK
    {
        EFI_HII_IMAGE_BLOCK Header;
        UINT8 PaletteIndex;
        EFI_HII_IIBT_IMAGE_8BIT_BASE Bitmap;
    }
    EFI_HII_IIBT_IMAGE_8BIT_BLOCK;

        typedef struct _EFI_HII_IIBT_IMAGE_8BIT_TRANS_BLOCK
    {
        EFI_HII_IMAGE_BLOCK Header;
        UINT8 PaletteIndex;
        EFI_HII_IIBT_IMAGE_8BIT_BASE Bitmap;
    }
    EFI_HII_IIBT_IMAGE_8BIT_TRAN_BLOCK;

        typedef struct _EFI_HII_IIBT_DUPLICATE_BLOCK
    {
        EFI_HII_IMAGE_BLOCK Header;
        EFI_IMAGE_ID ImageId;
    }
    EFI_HII_IIBT_DUPLICATE_BLOCK;

        typedef struct _EFI_HII_IIBT_JPEG_BLOCK
    {
        EFI_HII_IMAGE_BLOCK Header;
        UINT32 Size;
        UINT8 Data[1];
    }
    EFI_HII_IIBT_JPEG_BLOCK;

        typedef struct _EFI_HII_IIBT_SKIP1_BLOCK
    {
        EFI_HII_IMAGE_BLOCK Header;
        UINT8 SkipCount;
    }
    EFI_HII_IIBT_SKIP1_BLOCK;

        typedef struct _EFI_HII_IIBT_SKIP2_BLOCK
    {
        EFI_HII_IMAGE_BLOCK Header;
        UINT16 SkipCount;
    }
    EFI_HII_IIBT_SKIP2_BLOCK;

        //
        // Definitions for Palette Information
        //

        typedef struct _EFI_HII_IMAGE_PALETTE_INFO_HEADER
    {
        UINT16 PaletteCount;
    }
    EFI_HII_IMAGE_PALETTE_INFO_HEADER;

        typedef struct _EFI_HII_IMAGE_PALETTE_INFO
    {
        UINT16 PaletteSize;
        EFI_HII_RGB_PIXEL PaletteValue[1];
    }
    EFI_HII_IMAGE_PALETTE_INFO;
    */
    #endregion

    #region Definitions for Forms Package (Section 27.3.8)

    /// <summary>
    /// The Form package is used to carry form-based encoding data.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 4)]
    struct EFI_HII_FORM_PACKAGE_HDR
    {
        public EFI_HII_PACKAGE_HEADER Header;
        // EFI_IFR_OP_HEADER  Data[...];
    }

    /// <summary>
    /// Same as EFI_IFR_TYPE_TIME
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 3)]
    struct EFI_HII_TIME
    {
        public UINT8 Hour;
        public UINT8 Minute;
        public UINT8 Second;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 4)]
    struct EFI_HII_DATE
    {
        public UINT16 Year;
        public UINT8 Month;
        public UINT8 Day;
    };

    /// <summary>
    /// Same as EFI_IFR_TYPE_REF
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 22)]
    struct EFI_HII_REF
    {
        public EFI_QUESTION_ID QuestionId;
        public EFI_FORM_ID FormId;
        public EFI_GUID FormSetGuid;
        public EFI_STRING_ID DevicePath;
    };

    /// <summary>
    /// Value of IFR opcodes
    /// </summary>
    public enum EFI_IFR_OPCODE_e
    {
        #region IFR Opcodes
        EFI_IFR_FORM_OP = 0x01,
        EFI_IFR_SUBTITLE_OP = 0x02,
        EFI_IFR_TEXT_OP = 0x03,
        EFI_IFR_IMAGE_OP = 0x04,
        EFI_IFR_ONE_OF_OP = 0x05,
        EFI_IFR_CHECKBOX_OP = 0x06,
        EFI_IFR_NUMERIC_OP = 0x07,
        EFI_IFR_PASSWORD_OP = 0x08,
        EFI_IFR_ONE_OF_OPTION_OP = 0x09,
        EFI_IFR_SUPPRESS_IF_OP = 0x0A,
        EFI_IFR_LOCKED_OP = 0x0B,
        EFI_IFR_ACTION_OP = 0x0C,
        EFI_IFR_RESET_BUTTON_OP = 0x0D,
        EFI_IFR_FORM_SET_OP = 0x0E,
        EFI_IFR_REF_OP = 0x0F,
        EFI_IFR_NO_SUBMIT_IF_OP = 0x10,
        EFI_IFR_INCONSISTENT_IF_OP = 0x11,
        EFI_IFR_EQ_ID_VAL_OP = 0x12,
        EFI_IFR_EQ_ID_ID_OP = 0x13,
        EFI_IFR_EQ_ID_VAL_LIST_OP = 0x14,
        EFI_IFR_AND_OP = 0x15,
        EFI_IFR_OR_OP = 0x16,
        EFI_IFR_NOT_OP = 0x17,
        EFI_IFR_RULE_OP = 0x18,
        EFI_IFR_GRAY_OUT_IF_OP = 0x19,
        EFI_IFR_DATE_OP = 0x1A,
        EFI_IFR_TIME_OP = 0x1B,
        EFI_IFR_STRING_OP = 0x1C,
        EFI_IFR_REFRESH_OP = 0x1D,
        EFI_IFR_DISABLE_IF_OP = 0x1E,
        EFI_IFR_ANIMATION_OP = 0x1F,
        EFI_IFR_TO_LOWER_OP = 0x20,
        EFI_IFR_TO_UPPER_OP = 0x21,
        EFI_IFR_MAP_OP = 0x22,
        EFI_IFR_ORDERED_LIST_OP = 0x23,
        EFI_IFR_VARSTORE_OP = 0x24,
        EFI_IFR_VARSTORE_NAME_VALUE_OP = 0x25,
        EFI_IFR_VARSTORE_EFI_OP = 0x26,
        EFI_IFR_VARSTORE_DEVICE_OP = 0x27,
        EFI_IFR_VERSION_OP = 0x28,
        EFI_IFR_END_OP = 0x29,
        EFI_IFR_MATCH_OP = 0x2A,
        EFI_IFR_GET_OP = 0x2B,
        EFI_IFR_SET_OP = 0x2C,
        EFI_IFR_READ_OP = 0x2D,
        EFI_IFR_WRITE_OP = 0x2E,
        EFI_IFR_EQUAL_OP = 0x2F,
        EFI_IFR_NOT_EQUAL_OP = 0x30,
        EFI_IFR_GREATER_THAN_OP = 0x31,
        EFI_IFR_GREATER_EQUAL_OP = 0x32,
        EFI_IFR_LESS_THAN_OP = 0x33,
        EFI_IFR_LESS_EQUAL_OP = 0x34,
        EFI_IFR_BITWISE_AND_OP = 0x35,
        EFI_IFR_BITWISE_OR_OP = 0x36,
        EFI_IFR_BITWISE_NOT_OP = 0x37,
        EFI_IFR_SHIFT_LEFT_OP = 0x38,
        EFI_IFR_SHIFT_RIGHT_OP = 0x39,
        EFI_IFR_ADD_OP = 0x3A,
        EFI_IFR_SUBTRACT_OP = 0x3B,
        EFI_IFR_MULTIPLY_OP = 0x3C,
        EFI_IFR_DIVIDE_OP = 0x3D,
        EFI_IFR_MODULO_OP = 0x3E,
        EFI_IFR_RULE_REF_OP = 0x3F,
        EFI_IFR_QUESTION_REF1_OP = 0x40,
        EFI_IFR_QUESTION_REF2_OP = 0x41,
        EFI_IFR_UINT8_OP = 0x42,
        EFI_IFR_UINT16_OP = 0x43,
        EFI_IFR_UINT32_OP = 0x44,
        EFI_IFR_UINT64_OP = 0x45,
        EFI_IFR_TRUE_OP = 0x46,
        EFI_IFR_FALSE_OP = 0x47,
        EFI_IFR_TO_UINT_OP = 0x48,
        EFI_IFR_TO_STRING_OP = 0x49,
        EFI_IFR_TO_BOOLEAN_OP = 0x4A,
        EFI_IFR_MID_OP = 0x4B,
        EFI_IFR_FIND_OP = 0x4C,
        EFI_IFR_TOKEN_OP = 0x4D,
        EFI_IFR_STRING_REF1_OP = 0x4E,
        EFI_IFR_STRING_REF2_OP = 0x4F,
        EFI_IFR_CONDITIONAL_OP = 0x50,
        EFI_IFR_QUESTION_REF3_OP = 0x51,
        EFI_IFR_ZERO_OP = 0x52,
        EFI_IFR_ONE_OP = 0x53,
        EFI_IFR_ONES_OP = 0x54,
        EFI_IFR_UNDEFINED_OP = 0x55,
        EFI_IFR_LENGTH_OP = 0x56,
        EFI_IFR_DUP_OP = 0x57,
        EFI_IFR_THIS_OP = 0x58,
        EFI_IFR_SPAN_OP = 0x59,
        EFI_IFR_VALUE_OP = 0x5A,
        EFI_IFR_DEFAULT_OP = 0x5B,
        EFI_IFR_DEFAULTSTORE_OP = 0x5C,
        EFI_IFR_FORM_MAP_OP = 0x5D,
        EFI_IFR_CATENATE_OP = 0x5E,
        EFI_IFR_GUID_OP = 0x5F,
        EFI_IFR_SECURITY_OP = 0x60,
        EFI_IFR_MODAL_TAG_OP = 0x61,
        EFI_IFR_REFRESH_ID_OP = 0x62,
        EFI_IFR_WARNING_IF_OP = 0x63,
        EFI_IFR_MATCH2_OP = 0x64,
        #endregion
    };

    /// <summary>
    /// Definitions of IFR Standard Headers (Section 27.3.8.2)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 2)]
    struct EFI_IFR_OP_HEADER
    {
        private UINT8 _OpCode;
        private UINT8 _LengthAndscope;
        
        public EFI_IFR_OPCODE_e OpCode { get { return _OpCode.GetBits<EFI_IFR_OPCODE_e>(); } set { _OpCode = SetBits(_OpCode, value); } }
        public UINT8 Length { get { return _LengthAndscope.GetBits<UINT8>(0x7F); } set { _LengthAndscope = SetBits(_LengthAndscope, value, 0x7F); } }
        public UINT8 Scope { get { return _LengthAndscope.GetBits<UINT8>(0x01,7); } set { _LengthAndscope = SetBits(_LengthAndscope, value, 0x01, 7); } }
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 4)]
    struct EFI_IFR_STATEMENT_HEADER
    {
        public EFI_STRING_ID Prompt;
        public EFI_STRING_ID Help;
    };

    [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode, Pack = 1, Size = 2)]
    struct EFI_IFR_QUESTION_HEADER_VarStoreInfo
    {
        [FieldOffset(0)] // union type 1
        public EFI_QUESTION_ID VarName;
        [FieldOffset(0)] // union type 2
        public UINT16 VarOffset;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 11)]
    struct EFI_IFR_QUESTION_HEADER
    {
        public EFI_IFR_STATEMENT_HEADER Header;
        public EFI_QUESTION_ID QuestionId;
        public EFI_QUESTION_ID VarStoreId;
        public EFI_IFR_QUESTION_HEADER_VarStoreInfo VarStoreInfo;
        private UINT8 _Flags;

        public EFI_IFR_QUESTION_HEADER_FLAGS_e Flags { get { return _Flags.GetBits<EFI_IFR_QUESTION_HEADER_FLAGS_e>(); } set { _Flags = SetBits(_Flags, value); } }
     };

    /// <summary>
    /// Flag values of EFI_IFR_QUESTION_HEADER
    /// </summary>
    [Flags]
    enum EFI_IFR_QUESTION_HEADER_FLAGS_e
    {
        EFI_IFR_FLAG_READ_ONLY = 0x01,
        EFI_IFR_FLAG_CALLBACK = 0x04,
        EFI_IFR_FLAG_RESET_REQUIRED = 0x10,
        EFI_IFR_FLAG_RECONNECT_REQUIRED = 0x40,
        EFI_IFR_FLAG_OPTIONS_ONLY = 0x80,
    };

    /// <summary>
    /// Definition for Opcode Reference (Section Section 27.3.8.3)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 6)]
    struct EFI_IFR_DEFAULTSTORE
    {
        public EFI_IFR_OP_HEADER Header;
        public EFI_STRING_ID DefaultName;
        private UINT16 _DefaultId;

        public EFI_HII_DEFAULT_CLASS_e DefaultId { get { return _DefaultId.GetBits<EFI_HII_DEFAULT_CLASS_e>(); } set { _DefaultId = SetBits(_DefaultId, value); } }
    };

    /// <summary>
    /// Default Identifier of default store 
    /// </summary>
    enum EFI_HII_DEFAULT_CLASS_e
    {
        EFI_HII_DEFAULT_CLASS_STANDARD = 0x0000,
        EFI_HII_DEFAULT_CLASS_MANUFACTURING = 0x0001,
        EFI_HII_DEFAULT_CLASS_SAFE = 0x0002,
        EFI_HII_DEFAULT_CLASS_PLATFORM_BEGIN = 0x4000,
        EFI_HII_DEFAULT_CLASS_PLATFORM_END = 0x7fff,
        EFI_HII_DEFAULT_CLASS_HARDWARE_BEGIN = 0x8000,
        EFI_HII_DEFAULT_CLASS_HARDWARE_END = 0xbfff,
        EFI_HII_DEFAULT_CLASS_FIRMWARE_BEGIN = 0xc000,
        EFI_HII_DEFAULT_CLASS_FIRMWARE_END = 0xffff,
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 22)]
    struct EFI_IFR_VARSTORE
    {
        public EFI_IFR_OP_HEADER Header;
        public EFI_GUID Guid;
        public EFI_VARSTORE_ID VarStoreId;
        public UINT16 Size;
        // UINT8 Name[...];
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 26)]
    struct EFI_IFR_VARSTORE_EFI
    {
        public EFI_IFR_OP_HEADER Header;
        public EFI_VARSTORE_ID VarStoreId;
        public EFI_GUID Guid;
        public UINT32 Attributes;
        public UINT16 Size;
        // UINT8 Name[...];
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 20)]
    struct EFI_IFR_VARSTORE_NAME_VALUE
    {
        public EFI_IFR_OP_HEADER Header;
        public EFI_VARSTORE_ID VarStoreId;
        public EFI_GUID Guid;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack=1, Size=23)]
    struct EFI_IFR_FORM_SET
    {
        public EFI_IFR_OP_HEADER Header;
        public EFI_GUID Guid;
        public EFI_STRING_ID FormSetTitle;
        public EFI_STRING_ID Help;
        public UINT8 Flags; // bits 2:7 reserved
        // EFI_GUID ClassGuid[...];

        public UINT8 Flags_ClassGuidCount { get { return Flags.GetBits<UINT8>(0x03); } set { Flags = SetBits(Flags, value, 0x03); } }
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 6)]
    struct EFI_IFR_FORM
    {
        public EFI_IFR_OP_HEADER Header;
        public UINT16 FormId;
        public EFI_STRING_ID FormTitle;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 4)]
    struct EFI_IFR_IMAGE
    {
        public EFI_IFR_OP_HEADER Header;
        public EFI_IMAGE_ID Id;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 3)]
    struct EFI_IFR_RULE
    {
        public EFI_IFR_OP_HEADER Header;
        public UINT8 RuleId;
    };

    /// <summary>
    /// Same as _EFI_IFR_DEFAULT_2
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 5)]
    struct EFI_IFR_DEFAULT : IEfiIfrType
    {
        public EFI_IFR_OP_HEADER Header;
        public UINT16 DefaultId;
        private UINT8 _Type;
        // EFI_IFR_TYPE_VALUE Value; // = buffer which type depends on Type
        
        public EFI_IFR_TYPE_e Type { get { return _Type.GetBits<EFI_IFR_TYPE_e>(); } set { _Type = SetBits(_Type, value); } }
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 7)]
    struct EFI_IFR_SUBTITLE
    {
        public EFI_IFR_OP_HEADER Header;
        public EFI_IFR_STATEMENT_HEADER Statement;
        private UINT8 _Flags;

        public EFI_IFR_SUBTITLE_FLAGS_e Flags { get { return _Flags.GetBits<EFI_IFR_SUBTITLE_FLAGS_e>(); } set { _Flags = SetBits(_Flags, value); } }
    };

    [Flags]
    enum EFI_IFR_SUBTITLE_FLAGS_e
    {
        EFI_IFR_FLAGS_HORIZONTAL = 0x01,
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 14)]
    struct EFI_IFR_CHECKBOX
    {
        public EFI_IFR_OP_HEADER Header;
        public EFI_IFR_QUESTION_HEADER Question;
        private UINT8 _Flags;

        public EFI_IFR_CHECKBOX_e Flags { get { return _Flags.GetBits<EFI_IFR_CHECKBOX_e>(); } set { _Flags = SetBits(_Flags, value); } }
    };

    [Flags]
    enum EFI_IFR_CHECKBOX_e
    {
        EFI_IFR_CHECKBOX_DEFAULT = 0x01,
        EFI_IFR_CHECKBOX_DEFAULT_MFG = 0x02,
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 8)]
    struct EFI_IFR_TEXT
    {
        public EFI_IFR_OP_HEADER Header;
        public EFI_IFR_STATEMENT_HEADER Statement;
        public EFI_STRING_ID TextTwo;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 15)]
    struct EFI_IFR_REF
    {
        public EFI_IFR_OP_HEADER Header;
        public EFI_IFR_QUESTION_HEADER Question;
        public EFI_FORM_ID FormId;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 17)]
    struct EFI_IFR_REF2
    {
        public EFI_IFR_OP_HEADER Header;
        public EFI_IFR_QUESTION_HEADER Question;
        public EFI_FORM_ID FormId;
        public EFI_QUESTION_ID QuestionId;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 33)]
    struct EFI_IFR_REF3
    {
        public EFI_IFR_OP_HEADER Header;
        public EFI_IFR_QUESTION_HEADER Question;
        public EFI_FORM_ID FormId;
        public EFI_QUESTION_ID QuestionId;
        public EFI_GUID FormSetId;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 35)]
    struct EFI_IFR_REF4
    {
        public EFI_IFR_OP_HEADER Header;
        public EFI_IFR_QUESTION_HEADER Question;
        public EFI_FORM_ID FormId;
        public EFI_QUESTION_ID QuestionId;
        public EFI_GUID FormSetId;
        public EFI_STRING_ID DevicePath;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 13)]
    struct EFI_IFR_REF5
    {
        public EFI_IFR_OP_HEADER Header;
        public EFI_IFR_QUESTION_HEADER Question;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 8)]
    struct EFI_IFR_RESET_BUTTON
    {
        public EFI_IFR_OP_HEADER Header;
        public EFI_IFR_STATEMENT_HEADER Statement;
        public EFI_DEFAULT_ID DefaultId;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 15)]
    struct EFI_IFR_ACTION
    {
        public EFI_IFR_OP_HEADER Header;
        public EFI_IFR_QUESTION_HEADER Question;
        public EFI_STRING_ID QuestionConfig;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 13)]
    struct EFI_IFR_ACTION_1
    {
        public EFI_IFR_OP_HEADER Header;
        public EFI_IFR_QUESTION_HEADER Question;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 14)]
    struct EFI_IFR_DATE
    {
        public EFI_IFR_OP_HEADER Header;
        public EFI_IFR_QUESTION_HEADER Question;
        /// <summary>
        /// Use Flags_Supress or Flags_Storage if you want to access implemented bits only!
        /// </summary>
        public UINT8 Flags;

        public EFI_QF_DATE_SUPPRESS_FLAGS_e Flags_Supress { get { return Flags.GetBits<EFI_QF_DATE_SUPPRESS_FLAGS_e>(0x07); } set { Flags = SetBits(Flags, value, 0x07); } }
        public EFI_QF_DATE_STORAGE_e Flags_Storage { get { return Flags.GetBits<EFI_QF_DATE_STORAGE_e>(0x30); } set { Flags = SetBits(Flags, value, 0x30); } }
    };

    /// <summary>
    /// Flags that describe the behavior of the question.
    /// </summary>
    [Flags]
    enum EFI_QF_DATE_SUPPRESS_FLAGS_e
    {
        EFI_QF_DATE_YEAR_SUPPRESS = 0x01,
        EFI_QF_DATE_MONTH_SUPPRESS = 0x02,
        EFI_QF_DATE_DAY_SUPPRESS = 0x04,
    };

    /// <summary>
    /// Flags that describe the behavior of the question.
    /// </summary>
    enum EFI_QF_DATE_STORAGE_e
    {
        //EFI_QF_DATE_STORAGE = 0x30,
        QF_DATE_STORAGE_NORMAL = 0x00,
        QF_DATE_STORAGE_TIME = 0x10,
        QF_DATE_STORAGE_WAKEUP = 0x20,
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 3)]
    struct EFI_IFR_NUMERIC_MINMAXSTEP_DATA_8
    {
        public UINT8 MinValue;
        public UINT8 MaxValue;
        public UINT8 Step;
    };
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 6)]
    struct EFI_IFR_NUMERIC_MINMAXSTEP_DATA_16
    {
        public UINT16 MinValue;
        public UINT16 MaxValue;
        public UINT16 Step;
    };
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 12)]
    struct EFI_IFR_NUMERIC_MINMAXSTEP_DATA_32
    {
        public UINT32 MinValue;
        public UINT32 MaxValue;
        public UINT32 Step;
    };
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 24)]
    struct EFI_IFR_NUMERIC_MINMAXSTEP_DATA_64
    {
        public UINT64 MinValue;
        public UINT64 MaxValue;
        public UINT64 Step;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 14)]
    struct EFI_IFR_NUMERIC : IEfiIfrNumericValue
    {
        public EFI_IFR_OP_HEADER Header;
        public EFI_IFR_QUESTION_HEADER Question;
        /// <summary>
        /// Use Flags_DataSize or Flags_DisplayType if you want to access implemented bits only!
        /// </summary>
        public UINT8 Flags;
        // EFI_IFR_NUMERIC_MINMAXSTEP_DATA_x data;

        public EFI_IFR_NUMERIC_SIZE_e Flags_DataSize { get { return Flags.GetBits<EFI_IFR_NUMERIC_SIZE_e>(0x03); } set { Flags = SetBits(Flags, value, 0x03); } }
        public EFI_IFR_DISPLAY_e Flags_DisplayType { get { return Flags.GetBits<EFI_IFR_DISPLAY_e>(0x30); } set { Flags = SetBits(Flags, value, 0x30); } }
    };

    /// <summary>
    /// Flags related to the numeric question
    /// </summary>
    public enum EFI_IFR_NUMERIC_SIZE_e
    {
        //EFI_IFR_NUMERIC_SIZE = 0x03,
        EFI_IFR_NUMERIC_SIZE_1 = 0x00,
        EFI_IFR_NUMERIC_SIZE_2 = 0x01,
        EFI_IFR_NUMERIC_SIZE_4 = 0x02,
        EFI_IFR_NUMERIC_SIZE_8 = 0x03,
    };

    /// <summary>
    /// Flags related to the numeric question
    /// </summary>
    enum EFI_IFR_DISPLAY_e
    {
        //EFI_IFR_DISPLAY = 0x30,
        EFI_IFR_DISPLAY_INT_DEC = 0x00,
        EFI_IFR_DISPLAY_UINT_DEC = 0x10,
        EFI_IFR_DISPLAY_UINT_HEX = 0x20,
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 14)]
    struct EFI_IFR_ONE_OF : IEfiIfrNumericValue
    {
        public EFI_IFR_OP_HEADER Header;
        public EFI_IFR_QUESTION_HEADER Question;
        /// <summary>
        /// Use Flags_DataSize or Flags_DisplayType if you want to access implemented bits only!
        /// </summary>
        public UINT8 Flags;
        // EFI_IFR_NUMERIC_MINMAXSTEP_DATA_x data;

        public EFI_IFR_NUMERIC_SIZE_e Flags_DataSize { get { return Flags.GetBits<EFI_IFR_NUMERIC_SIZE_e>(0x03); } set { Flags = SetBits(Flags, value, 0x03); } }
        public EFI_IFR_DISPLAY_e Flags_DisplayType { get { return Flags.GetBits<EFI_IFR_DISPLAY_e>(0x30); } set { Flags = SetBits(Flags, value, 0x30); } }
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 16)]
    struct EFI_IFR_STRING
    {
        public EFI_IFR_OP_HEADER Header;
        public EFI_IFR_QUESTION_HEADER Question;
        public UINT8 MinSize;
        public UINT8 MaxSize;
        private UINT8 _Flags;

        public EFI_IFR_STRING_FLAGS_e Flags { get { return _Flags.GetBits<EFI_IFR_STRING_FLAGS_e>(); } set { _Flags = SetBits(_Flags, value); } }
    };

    [Flags]
    enum EFI_IFR_STRING_FLAGS_e
    {
        EFI_IFR_STRING_MULTI_LINE = 0x01,
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 15)]
    struct EFI_IFR_PASSWORD
    {
        public EFI_IFR_OP_HEADER Header;
        public EFI_IFR_QUESTION_HEADER Question;
        public UINT8 MinSize;
        public UINT8 MaxSize;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 15)]
    struct EFI_IFR_ORDERED_LIST
    {
        public EFI_IFR_OP_HEADER Header;
        public EFI_IFR_QUESTION_HEADER Question;
        public UINT8 MaxContainers;
        private UINT8 _Flags;
 
        public EFI_IFR_ORDERED_LIST_FLAGS_e Flags { get { return _Flags.GetBits<EFI_IFR_ORDERED_LIST_FLAGS_e>(); } set { _Flags = SetBits(_Flags, value); } }
    };

    [Flags]
    enum EFI_IFR_ORDERED_LIST_FLAGS_e
    {
        EFI_IFR_UNIQUE_SET = 0x01,
        EFI_IFR_NO_EMPTY_SET = 0x02,
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 14)]
    struct EFI_IFR_TIME
    {
        public EFI_IFR_OP_HEADER Header;
        public EFI_IFR_QUESTION_HEADER Question;
        /// <summary>
        /// Use Flags_Supress or Flags_Storage if you want to access implemented bits only!
        /// </summary>
        public UINT8 Flags;

        public EFI_QF_TIME_SUPPRESS_FLAGS_e Flags_Suppress { get { return Flags.GetBits<EFI_QF_TIME_SUPPRESS_FLAGS_e>(0x07); } set { Flags = SetBits(Flags, value, 0x07); } }
        public EFI_QF_TIME_STORAGE_e Flags_Storage { get { return Flags.GetBits<EFI_QF_TIME_STORAGE_e>(0x30); } set { Flags = SetBits(Flags, value, 0x30); } }
    };

    /// <summary>
    /// A bit-mask that determines which unique settings are active for this opcode.
    /// </summary>
    [Flags]
    enum EFI_QF_TIME_SUPPRESS_FLAGS_e
    {
        QF_TIME_HOUR_SUPPRESS = 0x01,
        QF_TIME_MINUTE_SUPPRESS = 0x02,
        QF_TIME_SECOND_SUPPRESS = 0x04,
    };

    /// <summary>
    /// A bit-mask that determines which unique settings are active for this opcode.
    /// </summary>
    enum EFI_QF_TIME_STORAGE_e
    {
        //QF_TIME_STORAGE = 0x30,
        QF_TIME_STORAGE_NORMAL = 0x00,
        QF_TIME_STORAGE_TIME = 0x10,
        QF_TIME_STORAGE_WAKEUP = 0x20,
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 4)]
    struct EFI_IFR_INCONSISTENT_IF
    {
        public EFI_IFR_OP_HEADER Header;
        public EFI_STRING_ID Error;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 4)]
    struct EFI_IFR_NO_SUBMIT_IF
    {
        public EFI_IFR_OP_HEADER Header;
        public EFI_STRING_ID Error;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 5)]
    struct EFI_IFR_WARNING_IF
    {
        public EFI_IFR_OP_HEADER Header;
        public EFI_STRING_ID Error;
        public UINT8 TimeOut;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 3)]
    struct EFI_IFR_REFRESH
    {
        public EFI_IFR_OP_HEADER Header;
        public UINT8 RefreshInterval;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 4)]
    struct EFI_IFR_VARSTORE_DEVICE
    {
        public EFI_IFR_OP_HEADER Header;
        public EFI_STRING_ID DevicePath;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 6)]
    struct EFI_IFR_ONE_OF_OPTION : IEfiIfrType
    {
        public EFI_IFR_OP_HEADER Header;
        public EFI_STRING_ID Option;
        private UINT8 _Flags;
        private UINT8 _Type;
        // EFI_IFR_TYPE_VALUE Value; // = buffer which type depends on Type

        public EFI_IFR_OPTION_e Flags { get { return _Flags.GetBits<EFI_IFR_OPTION_e>(); } set { _Flags = SetBits(_Flags, value); } }
        public EFI_IFR_TYPE_e Type { get { return _Type.GetBits<EFI_IFR_TYPE_e>(); } set { _Type = SetBits(_Type, value); } }
    };

    /// <summary>
    /// Types of the option's value.
    /// </summary>
    public enum EFI_IFR_TYPE_e
    {
        EFI_IFR_TYPE_NUM_SIZE_8 = 0x00,
        EFI_IFR_TYPE_NUM_SIZE_16 = 0x01,
        EFI_IFR_TYPE_NUM_SIZE_32 = 0x02,
        EFI_IFR_TYPE_NUM_SIZE_64 = 0x03,
        EFI_IFR_TYPE_BOOLEAN = 0x04,
        EFI_IFR_TYPE_TIME = 0x05,
        EFI_IFR_TYPE_DATE = 0x06,
        EFI_IFR_TYPE_STRING = 0x07,
        EFI_IFR_TYPE_OTHER = 0x08,
        EFI_IFR_TYPE_UNDEFINED = 0x09,
        EFI_IFR_TYPE_ACTION = 0x0A,
        EFI_IFR_TYPE_BUFFER = 0x0B,
        EFI_IFR_TYPE_REF = 0x0C,
    };

    enum EFI_IFR_OPTION_e
    {
        EFI_IFR_OPTION_DEFAULT = 0x10,
        EFI_IFR_OPTION_DEFAULT_MFG = 0x20,
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 18)]
    struct EFI_IFR_GUID
    {
        public EFI_IFR_OP_HEADER Header;
        public EFI_GUID Guid;
        //Optional Data Follows
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 18)]
    struct EFI_IFR_REFRESH_ID
    {
        public EFI_IFR_OP_HEADER Header;
        public EFI_GUID RefreshEventGroupId;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 6)]
    struct EFI_IFR_EQ_ID_ID
    {
        public EFI_IFR_OP_HEADER Header;
        public EFI_QUESTION_ID QuestionId1;
        public EFI_QUESTION_ID QuestionId2;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 6)]
    struct EFI_IFR_EQ_ID_VAL
    {
        public EFI_IFR_OP_HEADER Header;
        public EFI_QUESTION_ID QuestionId;
        public UINT16 Value;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 6)]
    struct EFI_IFR_EQ_ID_VAL_LIST
    {
        public EFI_IFR_OP_HEADER Header;
        public EFI_QUESTION_ID QuestionId;
        public UINT16 ListLength;
        // UINT16 ValueList[...];
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 3)]
    struct EFI_IFR_UINT8
    {
        public EFI_IFR_OP_HEADER Header;
        public UINT8 Value;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 4)]
    struct EFI_IFR_UINT16
    {
        public EFI_IFR_OP_HEADER Header;
        public UINT16 Value;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 6)]
    struct EFI_IFR_UINT32
    {
        public EFI_IFR_OP_HEADER Header;
        public UINT32 Value;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 10)]
    struct EFI_IFR_UINT64
    {
        public EFI_IFR_OP_HEADER Header;
        public UINT64 Value;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 4)]
    struct EFI_IFR_QUESTION_REF1
    {
        public EFI_IFR_OP_HEADER Header;
        public EFI_QUESTION_ID QuestionId;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 2)]
    struct EFI_IFR_QUESTION_REF3
    {
        public EFI_IFR_OP_HEADER Header;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 4)]
    struct EFI_IFR_QUESTION_REF3_2
    {
        public EFI_IFR_OP_HEADER Header;
        public EFI_STRING_ID DevicePath;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 20)]
    struct EFI_IFR_QUESTION_REF3_3
    {
        public EFI_IFR_OP_HEADER Header;
        public EFI_STRING_ID DevicePath;
        public EFI_GUID Guid;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 3)]
    struct EFI_IFR_RULE_REF
    {
        public EFI_IFR_OP_HEADER Header;
        public UINT8 RuleId;
    };
    
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 4)]
    struct EFI_IFR_STRING_REF1
    {
        public EFI_IFR_OP_HEADER Header;
        public EFI_STRING_ID StringId;
    };

    /// <summary>
    /// For EFI_IFR_TO_STRING, when converting from
    /// unsigned integers, these flags control the format:
    /// 0 = unsigned decimal.
    /// 1 = signed decimal.
    /// 2 = hexadecimal (lower-case alpha).
    /// 3 = hexadecimal (upper-case alpha).
    /// </summary>
    public enum EFI_IFR_TO_STRING_FORMAT_FROM_UINT_e
    {
        EFI_IFR_STRING_UNSIGNED_DEC = 0x00,
        EFI_IFR_STRING_SIGNED_DEC = 0x01,
        EFI_IFR_STRING_LOWERCASE_HEX = 0x02,
        EFI_IFR_STRING_UPPERCASE_HEX = 0x03,
    };

    /// <summary>
    /// When converting from a buffer, these flags control the format:
    /// 0 = ASCII.
    /// 8 = Unicode.
    /// </summary>
    public enum EFI_IFR_TO_STRING_FORMAT_FROM_BUFFER_e
    {
        EFI_IFR_STRING_ASCII = 0x00,
        EFI_IFR_STRING_UNICODE = 0x08,
    };
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 3)]
    struct EFI_IFR_TO_STRING
    {
        public EFI_IFR_OP_HEADER Header;
        /// <summary>
        /// Use Format_FromUINT or Format_FromBUFFER if possible for type safe access
        /// </summary>
        public UINT8 Format;

        public EFI_IFR_TO_STRING_FORMAT_FROM_BUFFER_e Format_FromUINT { get { return Format.GetBits<EFI_IFR_TO_STRING_FORMAT_FROM_BUFFER_e>(); } set { Format = SetBits(Format, value); } }
        public EFI_IFR_TO_STRING_FORMAT_FROM_UINT_e Format_FromBUFFER { get { return Format.GetBits<EFI_IFR_TO_STRING_FORMAT_FROM_UINT_e>(); } set { Format = SetBits(Format, value); } }
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 18)]
    struct EFI_IFR_MATCH2
    {
        public EFI_IFR_OP_HEADER Header;
        public EFI_GUID SyntaxType;
    };

    /// <summary>
    /// Flags governing the matching criteria of EFI_IFR_FIND
    /// </summary>
    public enum EFI_IFR_FIND_FORMAT_e
    {
        EFI_IFR_FF_CASE_SENSITIVE = 0x00,
        EFI_IFR_FF_CASE_INSENSITIVE = 0x01,
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 3)]
    struct EFI_IFR_FIND
    {
        public EFI_IFR_OP_HEADER Header;
        private UINT8 _Format;

        public EFI_IFR_FIND_FORMAT_e Format { get { return _Format.GetBits<EFI_IFR_FIND_FORMAT_e>(); } set { _Format = SetBits(_Format, value); } }
    };

    /// <summary>
    /// Flags specifying whether to find the first matching string
    /// or the first non-matching string.
    /// </summary>
    public enum EFI_IFR_SPAN_FLAGS_e
    {
        EFI_IFR_FLAGS_FIRST_MATCHING = 0x00,
        EFI_IFR_FLAGS_FIRST_NON_MATCHING = 0x01,
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 3)]
    struct EFI_IFR_SPAN
    {
        public EFI_IFR_OP_HEADER Header;
        private UINT8 _Flags;

        public EFI_IFR_SPAN_FLAGS_e Flags { get { return _Flags.GetBits<EFI_IFR_SPAN_FLAGS_e>(); } set { _Flags = SetBits(_Flags, value); } }
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 18)]
    struct EFI_IFR_SECURITY
    {
        /// <summary>
        /// Standard opcode header, where Header.Op = EFI_IFR_SECURITY_OP.
        /// </summary>
        public EFI_IFR_OP_HEADER Header;
        /// <summary>
        /// Security permission level.
        /// </summary>
        public EFI_GUID Permissions;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 18)]
    struct EFI_IFR_FORM_MAP_METHOD
    {
        /// <summary>
        /// The string identifier which provides the human-readable name of 
        /// the configuration method for this standards map form.
        /// </summary>
        public EFI_STRING_ID MethodTitle;
        /// <summary>
        /// Identifier which uniquely specifies the configuration methods 
        /// associated with this standards map form.
        /// </summary>
        public EFI_GUID MethodIdentifier;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 18)]
    struct EFI_IFR_FORM_MAP
    {
        /// <summary>
        /// The sequence that defines the type of opcode as well as the length 
        /// of the opcode being defined. Header.OpCode = EFI_IFR_FORM_MAP_OP. 
        /// </summary>
        public EFI_IFR_OP_HEADER Header;
        /// <summary>
        /// The unique identifier for this particular form.
        /// </summary>
        public EFI_FORM_ID FormId;
        /// <summary>
        /// One or more configuration method's name and unique identifier.
        /// </summary>
        // EFI_IFR_FORM_MAP_METHOD  Methods[...];
    };

    [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode, Pack = 1, Size = 2)]
    struct EFI_IFR_SET_HEADER_VarStoreInfo
    {
        /// <summary>
        /// A 16-bit Buffer Storage offset.
        /// </summary>
        [FieldOffset(0)] // union type 1
        public EFI_STRING_ID VarName;
        /// <summary>
        /// A Name Value or EFI Variable name (VarName).
        /// </summary>
        [FieldOffset(0)] // union type 2
        public UINT16 VarOffset;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 7)]
    struct EFI_IFR_SET
    {
        /// <summary>
        /// The sequence that defines the type of opcode as well as the length 
        /// of the opcode being defined. Header.OpCode = EFI_IFR_SET_OP. 
        /// </summary>
        public EFI_IFR_OP_HEADER Header;
        /// <summary>
        /// Specifies the identifier of a previously declared variable store to 
        /// use when storing the question's value. 
        /// </summary>
        public EFI_VARSTORE_ID VarStoreId;
        public EFI_IFR_SET_HEADER_VarStoreInfo VarStoreInfo;
        /// <summary>
        /// Specifies the type used for storage. 
        /// </summary>
        public UINT8 VarStoreType;
    };

    [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode, Pack = 1, Size = 2)]
    struct EFI_IFR_GET_HEADER_VarStoreInfo
    {
        /// <summary>
        /// A 16-bit Buffer Storage offset.
        /// </summary>
        [FieldOffset(0)] // union type 1
        public EFI_STRING_ID VarName;
        /// <summary>
        /// A Name Value or EFI Variable name (VarName).
        /// </summary>
        [FieldOffset(0)] // union type 2
        public UINT16 VarOffset;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 7)]
    struct EFI_IFR_GET
    {
        /// <summary>
        /// The sequence that defines the type of opcode as well as the length 
        /// of the opcode being defined. Header.OpCode = EFI_IFR_GET_OP. 
        /// </summary>
        public EFI_IFR_OP_HEADER Header;
        /// <summary>
        /// Specifies the identifier of a previously declared variable store to 
        /// use when retrieving the value. 
        /// </summary>
        public EFI_VARSTORE_ID VarStoreId;
        public EFI_IFR_GET_HEADER_VarStoreInfo VarStoreInfo;
        /// <summary>
        /// Specifies the type used for storage. 
        /// </summary>
        private UINT8 _VarStoreType;
        public EFI_IFR_TYPE_e VarStoreType { get { return _VarStoreType.GetBits<EFI_IFR_TYPE_e>(); } set { _VarStoreType = SetBits(_VarStoreType, value); } }
    };
    #endregion

    #region Definitions for Keyboard Package
    /*
        ///
        /// Each enumeration values maps a physical key on a keyboard.
        ///
        typedef enum {
        EfiKeyLCtrl,
        EfiKeyA0,
        EfiKeyLAlt,
        EfiKeySpaceBar,
        EfiKeyA2,
        EfiKeyA3,
        EfiKeyA4,
        EfiKeyRCtrl,
        EfiKeyLeftArrow,
        EfiKeyDownArrow,
        EfiKeyRightArrow,
        EfiKeyZero,
        EfiKeyPeriod,
        EfiKeyEnter,
        EfiKeyLShift,
        EfiKeyB0,
        EfiKeyB1,
        EfiKeyB2,
        EfiKeyB3,
        EfiKeyB4,
        EfiKeyB5,
        EfiKeyB6,
        EfiKeyB7,
        EfiKeyB8,
        EfiKeyB9,
        EfiKeyB10,
        EfiKeyRShift,
        EfiKeyUpArrow,
        EfiKeyOne,
        EfiKeyTwo,
        EfiKeyThree,
        EfiKeyCapsLock,
        EfiKeyC1,
        EfiKeyC2,
        EfiKeyC3,
        EfiKeyC4,
        EfiKeyC5,
        EfiKeyC6,
        EfiKeyC7,
        EfiKeyC8,
        EfiKeyC9,
        EfiKeyC10,
        EfiKeyC11,
        EfiKeyC12,
        EfiKeyFour,
        EfiKeyFive,
        EfiKeySix,
        EfiKeyPlus,
        EfiKeyTab,
        EfiKeyD1,
        EfiKeyD2,
        EfiKeyD3,
        EfiKeyD4,
        EfiKeyD5,
        EfiKeyD6,
        EfiKeyD7,
        EfiKeyD8,
        EfiKeyD9,
        EfiKeyD10,
        EfiKeyD11,
        EfiKeyD12,
        EfiKeyD13,
        EfiKeyDel,
        EfiKeyEnd,
        EfiKeyPgDn,
        EfiKeySeven,
        EfiKeyEight,
        EfiKeyNine,
        EfiKeyE0,
        EfiKeyE1,
        EfiKeyE2,
        EfiKeyE3,
        EfiKeyE4,
        EfiKeyE5,
        EfiKeyE6,
        EfiKeyE7,
        EfiKeyE8,
        EfiKeyE9,
        EfiKeyE10,
        EfiKeyE11,
        EfiKeyE12,
        EfiKeyBackSpace,
        EfiKeyIns,
        EfiKeyHome,
        EfiKeyPgUp,
        EfiKeyNLck,
        EfiKeySlash,
        EfiKeyAsterisk,
        EfiKeyMinus,
        EfiKeyEsc,
        EfiKeyF1,
        EfiKeyF2,
        EfiKeyF3,
        EfiKeyF4,
        EfiKeyF5,
        EfiKeyF6,
        EfiKeyF7,
        EfiKeyF8,
        EfiKeyF9,
        EfiKeyF10,
        EfiKeyF11,
        EfiKeyF12,
        EfiKeyPrint,
        EfiKeySLck,
        EfiKeyPause
    }
    EFI_KEY;

        typedef struct {
          ///
          /// Used to describe a physical key on a keyboard.
          ///
    EFI_KEY Key;
    ///
    /// Unicode character code for the Key.
    ///
    CHAR16 Unicode;
    ///
    /// Unicode character code for the key with the shift key being held down.
    ///
    CHAR16 ShiftedUnicode;
    ///
    /// Unicode character code for the key with the Alt-GR being held down.
    ///
    CHAR16 AltGrUnicode;
    ///
    /// Unicode character code for the key with the Alt-GR and shift keys being held down.
    ///
    CHAR16 ShiftedAltGrUnicode;
    ///
    /// Modifier keys are defined to allow for special functionality that is not necessarily 
    /// accomplished by a printable character. Many of these modifier keys are flags to toggle 
    /// certain state bits on and off inside of a keyboard driver.
    ///
    UINT16 Modifier;
    UINT16 AffectedAttribute;
        } EFI_KEY_DESCRIPTOR;

        ///
        /// A key which is affected by all the standard shift modifiers.  
        /// Most keys would be expected to have this bit active.
        ///
        #define EFI_AFFECTED_BY_STANDARD_SHIFT       0x0001

        ///
        /// This key is affected by the caps lock so that if a keyboard driver
        /// would need to disambiguate between a key which had a "1" defined
        /// versus an "a" character.  Having this bit turned on would tell
        /// the keyboard driver to use the appropriate shifted state or not.
        ///
        #define EFI_AFFECTED_BY_CAPS_LOCK            0x0002

        ///
        /// Similar to the case of CAPS lock, if this bit is active, the key
        /// is affected by the num lock being turned on.
        ///
        #define EFI_AFFECTED_BY_NUM_LOCK             0x0004

        typedef struct {
          UINT16 LayoutLength;
    EFI_GUID Guid;
    UINT32 LayoutDescriptorStringOffset;
    UINT8 DescriptorCount;
          // EFI_KEY_DESCRIPTOR    Descriptors[];
        } EFI_HII_KEYBOARD_LAYOUT;

        typedef struct {
          EFI_HII_PACKAGE_HEADER Header;
    UINT16 LayoutCount;
          // EFI_HII_KEYBOARD_LAYOUT Layout[];
        } EFI_HII_KEYBOARD_PACKAGE_HDR;

        //
        // Modifier values
        //
        #define EFI_NULL_MODIFIER                0x0000
        #define EFI_LEFT_CONTROL_MODIFIER        0x0001
        #define EFI_RIGHT_CONTROL_MODIFIER       0x0002
        #define EFI_LEFT_ALT_MODIFIER            0x0003
        #define EFI_RIGHT_ALT_MODIFIER           0x0004
        #define EFI_ALT_GR_MODIFIER              0x0005
        #define EFI_INSERT_MODIFIER              0x0006
        #define EFI_DELETE_MODIFIER              0x0007
        #define EFI_PAGE_DOWN_MODIFIER           0x0008
        #define EFI_PAGE_UP_MODIFIER             0x0009
        #define EFI_HOME_MODIFIER                0x000A
        #define EFI_END_MODIFIER                 0x000B
        #define EFI_LEFT_SHIFT_MODIFIER          0x000C
        #define EFI_RIGHT_SHIFT_MODIFIER         0x000D
        #define EFI_CAPS_LOCK_MODIFIER           0x000E
        #define EFI_NUM_LOCK_MODIFIER            0x000F
        #define EFI_LEFT_ARROW_MODIFIER          0x0010
        #define EFI_RIGHT_ARROW_MODIFIER         0x0011
        #define EFI_DOWN_ARROW_MODIFIER          0x0012
        #define EFI_UP_ARROW_MODIFIER            0x0013
        #define EFI_NS_KEY_MODIFIER              0x0014
        #define EFI_NS_KEY_DEPENDENCY_MODIFIER   0x0015
        #define EFI_FUNCTION_KEY_ONE_MODIFIER    0x0016
        #define EFI_FUNCTION_KEY_TWO_MODIFIER    0x0017
        #define EFI_FUNCTION_KEY_THREE_MODIFIER  0x0018
        #define EFI_FUNCTION_KEY_FOUR_MODIFIER   0x0019
        #define EFI_FUNCTION_KEY_FIVE_MODIFIER   0x001A
        #define EFI_FUNCTION_KEY_SIX_MODIFIER    0x001B
        #define EFI_FUNCTION_KEY_SEVEN_MODIFIER  0x001C
        #define EFI_FUNCTION_KEY_EIGHT_MODIFIER  0x001D
        #define EFI_FUNCTION_KEY_NINE_MODIFIER   0x001E
        #define EFI_FUNCTION_KEY_TEN_MODIFIER    0x001F
        #define EFI_FUNCTION_KEY_ELEVEN_MODIFIER 0x0020
        #define EFI_FUNCTION_KEY_TWELVE_MODIFIER 0x0021

        //
        // Keys that have multiple control functions based on modifier
        // settings are handled in the keyboard driver implementation.
        // For instance, PRINT_KEY might have a modifier held down and
        // is still a nonprinting character, but might have an alternate
        // control function like SYSREQUEST
        //
        #define EFI_PRINT_MODIFIER               0x0022
        #define EFI_SYS_REQUEST_MODIFIER         0x0023
        #define EFI_SCROLL_LOCK_MODIFIER         0x0024
        #define EFI_PAUSE_MODIFIER               0x0025
        #define EFI_BREAK_MODIFIER               0x0026

        #define EFI_LEFT_LOGO_MODIFIER           0x0027
        #define EFI_RIGHT_LOGO_MODIFIER          0x0028
        #define EFI_MENU_MODIFIER                0x0029
        */
    #endregion

    #region Animation Package

    /// <summary>
    /// Animation IFR opcode
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 4)]
    struct EFI_IFR_ANIMATION
    {
        /// <summary>
        /// Standard opcode header, where Header.OpCode is EFI_IFR_ANIMATION_OP.
        /// </summary>
        public EFI_IFR_OP_HEADER Header;
        /// <summary>
        /// Animation identifier in the HII database.
        /// </summary>
        public EFI_ANIMATION_ID Id;
    };

    /*
    ///
    /// HII animation package header.
    ///
    typedef struct _EFI_HII_ANIMATION_PACKAGE_HDR
    {
    ///
    /// Standard package header, where Header.Type = EFI_HII_PACKAGE_ANIMATIONS.
    ///
    EFI_HII_PACKAGE_HEADER Header;
    ///
    /// Offset, relative to this header, of the animation information. If 
    /// this is zero, then there are no animation sequences in the package.
    ///
    UINT32 AnimationInfoOffset;
    }
    EFI_HII_ANIMATION_PACKAGE_HDR;

    ///
    /// Animation information is encoded as a series of blocks,
    /// with each block prefixed by a single byte header EFI_HII_ANIMATION_BLOCK.
    ///
    typedef struct _EFI_HII_ANIMATION_BLOCK
    {
    UINT8 BlockType;
    //UINT8  BlockBody[];
    }
    EFI_HII_ANIMATION_BLOCK;

    ///
    /// Animation block types.
    ///
    #define EFI_HII_AIBT_END                 0x00
    #define EFI_HII_AIBT_OVERLAY_IMAGES      0x10
    #define EFI_HII_AIBT_CLEAR_IMAGES        0x11
    #define EFI_HII_AIBT_RESTORE_SCRN        0x12
    #define EFI_HII_AIBT_OVERLAY_IMAGES_LOOP 0x18
    #define EFI_HII_AIBT_CLEAR_IMAGES_LOOP   0x19
    #define EFI_HII_AIBT_RESTORE_SCRN_LOOP   0x1A
    #define EFI_HII_AIBT_DUPLICATE           0x20
    #define EFI_HII_AIBT_SKIP2               0x21
    #define EFI_HII_AIBT_SKIP1               0x22
    #define EFI_HII_AIBT_EXT1                0x30
    #define EFI_HII_AIBT_EXT2                0x31
    #define EFI_HII_AIBT_EXT4                0x32

    ///
    /// Extended block headers used for variable sized animation records
    /// which need an explicit length.
    ///

    typedef struct _EFI_HII_AIBT_EXT1_BLOCK
    {
    ///
    /// Standard animation header, where Header.BlockType = EFI_HII_AIBT_EXT1.
    ///
    EFI_HII_ANIMATION_BLOCK Header;
    ///
    /// The block type.
    ///
    UINT8 BlockType2;
    ///
    /// Size of the animation block, in bytes, including the animation block header.
    ///
    UINT8 Length;
    }
    EFI_HII_AIBT_EXT1_BLOCK;

    typedef struct _EFI_HII_AIBT_EXT2_BLOCK
    {
    ///
    /// Standard animation header, where Header.BlockType = EFI_HII_AIBT_EXT2.
    ///
    EFI_HII_ANIMATION_BLOCK Header;
    ///
    /// The block type
    ///
    UINT8 BlockType2;
    ///
    /// Size of the animation block, in bytes, including the animation block header.
    ///
    UINT16 Length;
    }
    EFI_HII_AIBT_EXT2_BLOCK;

    typedef struct _EFI_HII_AIBT_EXT4_BLOCK
    {
    ///
    /// Standard animation header, where Header.BlockType = EFI_HII_AIBT_EXT4.
    ///
    EFI_HII_ANIMATION_BLOCK Header;
    ///
    /// The block type
    ///
    UINT8 BlockType2;
    ///
    /// Size of the animation block, in bytes, including the animation block header.
    ///
    UINT32 Length;
    }
    EFI_HII_AIBT_EXT4_BLOCK;

    typedef struct _EFI_HII_ANIMATION_CELL
    {
    ///
    /// The X offset from the upper left hand corner of the logical 
    /// window to position the indexed image.
    ///
    UINT16 OffsetX;
    ///
    /// The Y offset from the upper left hand corner of the logical 
    /// window to position the indexed image.
    ///
    UINT16 OffsetY;
    ///
    /// The image to display at the specified offset from the upper left 
    /// hand corner of the logical window.
    ///
    EFI_IMAGE_ID ImageId;
    ///
    /// The number of milliseconds to delay after displaying the indexed 
    /// image and before continuing on to the next linked image.  If value 
    /// is zero, no delay.
    ///
    UINT16 Delay;
    }
    EFI_HII_ANIMATION_CELL;

    ///
    /// An animation block to describe an animation sequence that does not cycle, and
    /// where one image is simply displayed over the previous image.
    ///
    typedef struct _EFI_HII_AIBT_OVERLAY_IMAGES_BLOCK
    {
    ///
    /// This is image that is to be reference by the image protocols, if the 
    /// animation function is not supported or disabled. This image can 
    /// be one particular image from the animation sequence (if any one 
    /// of the animation frames has a complete image) or an alternate 
    /// image that can be displayed alone. If the value is zero, no image 
    /// is displayed.
    ///
    EFI_IMAGE_ID DftImageId;
    ///
    /// The overall width of the set of images (logical window width).
    ///
    UINT16 Width;
    ///
    /// The overall height of the set of images (logical window height).
    ///
    UINT16 Height;
    ///
    /// The number of EFI_HII_ANIMATION_CELL contained in the 
    /// animation sequence.
    ///
    UINT16 CellCount;
    ///
    /// An array of CellCount animation cells.
    ///
    EFI_HII_ANIMATION_CELL AnimationCell[1];
    }
    EFI_HII_AIBT_OVERLAY_IMAGES_BLOCK;

    ///
    /// An animation block to describe an animation sequence that does not cycle,
    /// and where the logical window is cleared to the specified color before 
    /// the next image is displayed.
    ///
    typedef struct _EFI_HII_AIBT_CLEAR_IMAGES_BLOCK
    {
    ///
    /// This is image that is to be reference by the image protocols, if the 
    /// animation function is not supported or disabled. This image can 
    /// be one particular image from the animation sequence (if any one 
    /// of the animation frames has a complete image) or an alternate 
    /// image that can be displayed alone. If the value is zero, no image 
    /// is displayed.
    ///
    EFI_IMAGE_ID DftImageId;
    ///
    /// The overall width of the set of images (logical window width).
    ///
    UINT16 Width;
    ///
    /// The overall height of the set of images (logical window height).
    ///
    UINT16 Height;
    ///
    /// The number of EFI_HII_ANIMATION_CELL contained in the 
    /// animation sequence.
    ///
    UINT16 CellCount;
    ///
    /// The color to clear the logical window to before displaying the 
    /// indexed image.
    ///
    EFI_HII_RGB_PIXEL BackgndColor;
    ///
    /// An array of CellCount animation cells.
    ///
    EFI_HII_ANIMATION_CELL AnimationCell[1];
    }
    EFI_HII_AIBT_CLEAR_IMAGES_BLOCK;

    ///
    /// An animation block to describe an animation sequence that does not cycle,
    /// and where the screen is restored to the original state before the next 
    /// image is displayed.
    ///
    typedef struct _EFI_HII_AIBT_RESTORE_SCRN_BLOCK
    {
    ///
    /// This is image that is to be reference by the image protocols, if the 
    /// animation function is not supported or disabled. This image can 
    /// be one particular image from the animation sequence (if any one 
    /// of the animation frames has a complete image) or an alternate 
    /// image that can be displayed alone. If the value is zero, no image 
    /// is displayed.
    ///
    EFI_IMAGE_ID DftImageId;
    ///
    /// The overall width of the set of images (logical window width).
    ///
    UINT16 Width;
    ///
    /// The overall height of the set of images (logical window height).
    ///
    UINT16 Height;
    ///
    /// The number of EFI_HII_ANIMATION_CELL contained in the 
    /// animation sequence.
    ///
    UINT16 CellCount;
    ///
    /// An array of CellCount animation cells.
    ///
    EFI_HII_ANIMATION_CELL AnimationCell[1];
    }
    EFI_HII_AIBT_RESTORE_SCRN_BLOCK;

    ///
    /// An animation block to describe an animation sequence that continuously cycles,
    /// and where one image is simply displayed over the previous image.
    ///
    typedef EFI_HII_AIBT_OVERLAY_IMAGES_BLOCK  EFI_HII_AIBT_OVERLAY_IMAGES_LOOP_BLOCK;

    ///
    /// An animation block to describe an animation sequence that continuously cycles,
    /// and where the logical window is cleared to the specified color before 
    /// the next image is displayed.
    ///
    typedef EFI_HII_AIBT_CLEAR_IMAGES_BLOCK    EFI_HII_AIBT_CLEAR_IMAGES_LOOP_BLOCK;

    ///
    /// An animation block to describe an animation sequence that continuously cycles,
    /// and where the screen is restored to the original state before 
    /// the next image is displayed.
    ///
    typedef EFI_HII_AIBT_RESTORE_SCRN_BLOCK    EFI_HII_AIBT_RESTORE_SCRN_LOOP_BLOCK;

    ///
    /// Assigns a new character value to a previously defined animation sequence.
    ///
    typedef struct _EFI_HII_AIBT_DUPLICATE_BLOCK
    {
    ///
    /// The previously defined animation ID with the exact same 
    /// animation information.
    ///
    EFI_ANIMATION_ID AnimationId;
    }
    EFI_HII_AIBT_DUPLICATE_BLOCK;

    ///
    /// Skips animation IDs.
    ///
    typedef struct _EFI_HII_AIBT_SKIP1_BLOCK
    {
    ///
    /// The unsigned 8-bit value to add to AnimationIdCurrent.
    ///
    UINT8 SkipCount;
    }
    EFI_HII_AIBT_SKIP1_BLOCK;

    ///
    /// Skips animation IDs.
    ///
    typedef struct _EFI_HII_AIBT_SKIP2_BLOCK
    {
    ///
    /// The unsigned 16-bit value to add to AnimationIdCurrent.
    ///
    UINT16 SkipCount;
    }
    EFI_HII_AIBT_SKIP2_BLOCK;
    */
    #endregion
}