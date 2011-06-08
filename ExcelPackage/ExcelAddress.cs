﻿/*******************************************************************************
 * You may amend and distribute as you like, but don't remove this header!
 * 
 * All rights reserved.
 * 
 * EPPlus is an Open Source project provided under the 
 * GNU General Public License (GPL) as published by the 
 * Free Software Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
 * 
 * See http://epplus.codeplex.com/ for details
 * 
 * The GNU General Public License can be viewed at http://www.opensource.org/licenses/gpl-license.php
 * If you unfamiliar with this license or have questions about it, here is an http://www.gnu.org/licenses/gpl-faq.html
 * 
 * The code for this project may be used and redistributed by any means PROVIDING it is 
 * not sold for profit without the author's written consent, and providing that this notice 
 * and the author's name and all copyright notices remain intact.
 * 
 * All code and executables are provided "as is" with no warranty either express or implied. 
 * The author accepts no liability for any damage or loss of business that this product may cause.
 *
 * Code change notes:
 * 
 * Author							Change						Date
 *******************************************************************************
 * Jan Källman		Added		18-MAR-2010
 *******************************************************************************/
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace OfficeOpenXml
{
    /// <summary>
    /// A range address
    /// </summary>
    /// <remarks>Examples of addresses are "A1" "B1:C2" "A:A" "1:1" "A1:E2,G3:G5" </remarks>
    public class ExcelAddressBase : ExcelCellBase
    {
        internal protected int _fromRow=-1, _toRow, _fromCol, _toCol;
        internal protected string _wb;
        internal protected string _ws;
        internal protected string _address;
        internal enum eAddressCollition
        {
            No,
            Partly,
            Inside,
            Equal
        }
        #region "Constructors"
        internal ExcelAddressBase()
        {
        }
        /// <summary>
        /// Creates an Address object
        /// </summary>
        /// <param name="fromRow">start row</param>
        /// <param name="fromCol">start column</param>
        /// <param name="toRow">End row</param>
        /// <param name="toColumn">End column</param>
        public ExcelAddressBase(int fromRow, int fromCol, int toRow, int toColumn)
        {
            _fromRow = fromRow;
            _toRow = toRow;
            _fromCol = fromCol;
            _toCol = toColumn;
            Validate();

            _address = GetAddress(_fromRow, _fromCol, _toRow, _toCol);
        }
        /// <summary>
        /// Creates an Address object
        /// </summary>
        /// <remarks>Examples of addresses are "A1" "B1:C2" "A:A" "1:1" "A1:E2,G3:G5" </remarks>
        /// <param name="address">The Excel Address</param>
        public ExcelAddressBase(string address)
        {
            SetAddress(address);
        }

        /// <summary>
        /// Address is an defined name
        /// </summary>
        /// <param name="address">the name</param>
        /// <param name="isName">Should always be true</param>
        internal ExcelAddressBase(string address, bool isName)
        {
            if (isName)
            {
                _address = address;
                _fromRow = -1;
                _fromCol = -1;
                _toRow = -1;
                _toCol = -1;
                _start = null;
                _end = null;
            }
            else
            {
                SetAddress(address);
            }
        }

        protected internal void SetAddress(string address)
        {
            if(address.StartsWith("'"))
            {
                int pos = address.LastIndexOf("'");
                SetWbWs(address.Substring(1,pos-1).Replace("''","'"));
                _address = address.Substring(pos + 2);
            }
            else if (address.StartsWith("[")) //Remove any external reference
            {
                SetWbWs(address);
            }
            else
            {
                _address = address;
            }
            if(_address.IndexOfAny(new char[] {',','!'}) > -1)
            {
                //Advanced address. Including Sheet or multi
                ExtractAddress(_address);
            }
            else
            {
                //Simple address
                GetRowColFromAddress(_address, out _fromRow, out _fromCol, out _toRow, out  _toCol);
                _addresses = null;
                _start = null;
                _end = null;
            }
            _address = address;
            Validate();
        }

        private void SetWbWs(string address)
        {
            int pos;
            if (address[0] == '[')
            {
                pos = address.LastIndexOf("]");
                _wb = address.Substring(1, pos - 1);                
                _ws = address.Substring(pos + 1);
            }
            else
            {
                _wb = "";
                _ws = address;
            }
            pos = _ws.IndexOf("!");
            if (pos > -1)
            {
                _address = _ws.Substring(pos + 1);
                _ws = _ws.Substring(0, pos);
            }
        }
        ExcelCellAddress _start = null;
        #endregion
        /// <summary>
        /// Gets the row and column of the top left cell.
        /// </summary>
        /// <value>The start row column.</value>
        public ExcelCellAddress Start
        {
            get
            {
                if (_start == null)
                {
                    _start = new ExcelCellAddress(_fromRow, _fromCol);
                }
                return _start;
            }
        }
        ExcelCellAddress _end = null;
        /// <summary>
        /// Gets the row and column of the bottom right cell.
        /// </summary>
        /// <value>The end row column.</value>
        public ExcelCellAddress End
        {
            get
            {
                if (_end == null)
                {
                    _end = new ExcelCellAddress(_toRow, _toCol);
                }
                return _end;
            }
        }
        /// <summary>
        /// The address for the range
        /// </summary>
        public virtual string Address
        {
            get
            {
                return _address;
            }
        }
        /// <summary>
        /// If the address is a defined name
        /// </summary>
        public bool IsName
        {
            get
            {
                return _fromRow < 0;
            }
        }
        string _firstAddress;
        /// <summary>
        /// returns the first address if the address is a multi address.
        /// A1:A2,B1:B2 returns A1:A2
        /// </summary>
        internal string FirstAddress
        {
            get
            {
                if (string.IsNullOrEmpty(_firstAddress))
                {
                    return _address;
                }
                else
                {
                    return _firstAddress;
                }
            }
        }
        /// <summary>
        /// Validate the address
        /// </summary>
        protected void Validate()
        {
            if (_fromRow > _toRow || _fromCol > _toCol)
            {
                throw new ArgumentOutOfRangeException("Start cell Address must be less or equal to End cell address");
            }
        }
        internal string WorkSheet
        {
            get
            {
                return _ws;
            }
        }
        List<ExcelAddress> _addresses = null;
        internal List<ExcelAddress> Addresses
        {
            get
            {
                return _addresses;
            }
        }

        private void ExtractAddress(string fullAddress)
        {
            string first="", second="";
            bool isText=false, hasSheet=false;
            if (fullAddress == "#REF!")
            {
                SetAddress(ref fullAddress, ref second, ref hasSheet );
                return;
            }
            foreach (char c in fullAddress)
            {
                if(c=='\'')
                {
                    isText=!isText;
                }
                else
                {
                    if(c=='!' && !isText && !first.EndsWith("#REF") && !second.EndsWith("#REF"))
                    {
                        hasSheet=true;
                    }
                    else if (c == ',' && !isText)
                    {
                        SetAddress(ref first, ref second, ref hasSheet);
                    }
                    else
                    {
                        if (hasSheet)
                        {
                            second += c;
                        }
                        else
                        {
                            first += c;
                        }
                    }
                }
            }
            SetAddress(ref first, ref second, ref hasSheet);
        }
        #region Address manipulation methods
        internal eAddressCollition Collide(ExcelAddressBase address)
        {
            if (address.WorkSheet != WorkSheet)
            {
                return eAddressCollition.No;
            }

            if (address._fromRow > _toRow || address._fromCol > _toCol
                ||
                _fromRow > address._toRow || _fromCol > address._toCol)
            {
                return eAddressCollition.No;
            }
            else if (address._fromRow == _fromRow && address._fromCol == _fromCol &&
                    address._toRow == _toRow && address._toCol == _toCol)
            {
                return eAddressCollition.Equal;
            }
            else if (address._fromRow >= _fromRow && address._toRow <= _toRow &&
                     address._fromCol >= _fromCol && address._toCol <= _toCol)
            {
                return eAddressCollition.Inside;
            }
            else
                return eAddressCollition.Partly;
        }        
        internal ExcelAddressBase AddRow(int row, int rows)
        {
            if (row > _toRow)
            {
                return this;
            }
            else if (row <= _fromRow)
            {
                return new ExcelAddressBase(_fromRow + rows, _fromCol, _toRow + rows, _toCol);
            }
            else
            {
                return new ExcelAddressBase(_fromRow, _fromCol, _toRow + rows, _toCol);
            }
        }
        internal ExcelAddressBase DeleteRow(int row, int rows)
        {
            if (row > _toRow) //After
            {
                return this;
            }            
            else if (row+rows <= _fromRow) //Before
            {
                return new ExcelAddressBase(_fromRow - rows, _fromCol, _toRow - rows, _toCol);
            }
            else if (row <= _fromRow && row + rows > _toRow) //Inside
            {
                return null;
            }
            else  //Partly
            {
                if (row <= _fromRow)
                {
                    return new ExcelAddressBase(row, _fromCol, _toRow - rows, _toCol);
                }
                else
                {
                    return new ExcelAddressBase(_fromRow, _fromCol, _toRow - rows < row ? row - 1 : _toRow - rows, _toCol);
                }
            }
        }
        internal ExcelAddressBase AddColumn(int col, int cols)
        {
            if (col > _toCol)
            {
                return this;
            }
            else if (col <= _fromCol)
            {
                return new ExcelAddressBase(_fromRow, _fromCol + cols, _toRow, _toCol + cols);
            }
            else
            {
                return new ExcelAddressBase(_fromRow, _fromCol, _toRow, _toCol + cols);
            }
        }
        internal ExcelAddressBase DeleteColumn(int col, int cols)
        {
            if (col > _toCol) //After
            {
                return this;
            }
            else if (col + cols <= _fromRow) //Before
            {
                return new ExcelAddressBase(_fromRow, _fromCol - cols, _toRow, _toCol - cols);
            }
            else if (col <= _fromCol && col + cols > _toCol) //Inside
            {
                return null;
            }
            else  //Partly
            {
                if (col <= _fromCol)
                {
                    return new ExcelAddressBase(_fromRow, col, _toRow, _toCol - cols);
                }
                else
                {
                    return new ExcelAddressBase(_fromRow, _fromCol, _toRow, _toCol - cols < col ? col - 1 : _toCol - cols);
                }
            }
        }
        #endregion
        private void SetAddress(ref string first, ref string second, ref bool hasSheet)
        {
            string ws, address;
            if (hasSheet)
            {
                ws = first;
                address = second;
                first = "";
                second = "";
            }
            else
            {
                address = first;
                ws = "";
                first = "";
            }
            hasSheet = false;
            if (string.IsNullOrEmpty(_firstAddress))
            {
                if(string.IsNullOrEmpty(_ws) || !string.IsNullOrEmpty(ws))_ws = ws;
                _firstAddress = address;
                GetRowColFromAddress(address, out _fromRow, out _fromCol, out _toRow, out  _toCol);
            }
            else
            {
                if (_addresses == null) _addresses = new List<ExcelAddress>();
                _addresses.Add(new ExcelAddress(_ws, address));
            }
        }
        internal enum AddressType
        {
            Invalid,
            InternalAddress,
            ExternalAddress,
            InternalName,
            ExternalName
        }

        internal static AddressType IsValid(string Address)
        {
            string ws="";
            if (Address.IndexOfAny(new char[] { '(', ')', '+', '-', '*', '/', '.', '=','^','&','%','\"' })>-1)
            {
                return AddressType.Invalid;
            }
            if (Address.IndexOf('!') > 0)
            {
                string[] split = Address.Split('!');
                if (split.Length == 2)
                {
                    ws = split[0];
                    Address = split[1];
                }
                else if (split.Length == 3 && split[1] == "#REF" && split[2] == "")
                {
                    ws = split[0];
                    Address = "#REF!";
                    if (ws.StartsWith("[") && ws.IndexOf("]") > 1)
                    {
                        return AddressType.ExternalAddress;
                    }
                    else
                    {
                        return AddressType.InternalAddress;
                    }
                }
                else
                {
                    return AddressType.Invalid;
                }
            }
            int row, col;
            if(ExcelAddressBase.GetRowCol(Address,out row, out col, false))
            {
                if (row > 0 && col > 0 && row <= ExcelPackage.MaxRows && col <= ExcelPackage.MaxColumns)
                {
                    if (ws.StartsWith("[") && ws.IndexOf("]") > 1)
                    {
                        return AddressType.ExternalAddress;
                    }
                    else
                    {
                        return AddressType.InternalAddress;
                    }
                }
                else
                {
                    return AddressType.Invalid;
                }
            }
            else
            {
                if(IsValidName(Address))
                {
                    if (ws.StartsWith("[") && ws.IndexOf("]") > 1)
                    {
                        return AddressType.ExternalName;
                    }
                    else
                    {
                        return AddressType.InternalName;
                    }
                }
                else
                {
                    return AddressType.Invalid;
                }
            }

        }

        private static bool IsValidName(string address)
        {
            if (Regex.IsMatch(address, "[^0-9./*-+,½!\"@#£%&/{}()\\[\\]=?`^~':;<>|][^/*-+,½!\"@#£%&/{}()\\[\\]=?`^~':;<>|]*"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
    /// <summary>
    /// Range address with the address property readonly
    /// </summary>
    public class ExcelAddress : ExcelAddressBase
    {
        internal ExcelAddress()
            : base()
        {

        }

        public ExcelAddress(int fromRow, int fromCol, int toRow, int toColumn)
            : base(fromRow, fromCol, toRow, toColumn)
        {
            _ws = "";
        }
        public ExcelAddress(string address)
            : base(address)
        {
        }
        
        internal ExcelAddress(string ws, string address)
            : base(address)
        {
            if (string.IsNullOrEmpty(_ws)) _ws = ws;
        }
        internal ExcelAddress(string ws, string address, bool isName)
            : base(address, isName)
        {
            if (string.IsNullOrEmpty(_ws)) _ws = ws;
        }
        /// <summary>
        /// The address for the range
        /// </summary>
        /// <remarks>Examples of addresses are "A1" "B1:C2" "A:A" "1:1" "A1:E2,G3:G5" </remarks>
        public new string Address
        {
            get
            {
                return _address;
            }
            set
            {                
                SetAddress(value);
            }
        }
    }
}
