using System;
using System.Collections.Generic;
using System.Text;

/*
     The contents of this file are subject to the Mozilla Public License
     Version 1.1 (the "License"); you may not use this file except in
     compliance with the License. You may obtain a copy of the License at
     http://www.mozilla.org/MPL/

     Software distributed under the License is distributed on an "AS IS"
     basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
     License for the specific language governing rights and limitations
     under the License.

     The Original Code is OpenMCDF - Compound Document Format library.

     The Initial Developer of the Original Code is Federico Blaseotto.
*/

namespace OLECompoundFileStorage
{
    /// <summary>
    /// OpenMCDF base exception.
    /// </summary>
    public class CFSException : Exception
    {
        public CFSException(string message)
            : base(message, null)
        {

        }

        public CFSException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

    }

    public class CFSFileFormatException : CFSException
    {
        
        public CFSFileFormatException(string message)
            : base(message, null)
        {
            
        }

        public CFSFileFormatException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

    }
}
