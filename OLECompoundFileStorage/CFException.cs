using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

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

namespace OleCompoundFileStorage
{
    /// <summary>
    /// OpenMCDF base exception.
    /// </summary>
    [Serializable]
    public class CFException : Exception
    {
        public CFException()
            : base()
        {
        }

        protected CFException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public CFException(string message)
            : base(message, null)
        {

        }

        public CFException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

    }

    /// <summary>
    /// Exception raised when a data setter/getter method is invoked
    /// on a stream or storage object after the disposal of the owner
    /// compound file object.
    /// </summary>
    [Serializable]
    public class CFDisposedException : InvalidOperationException
    {
        public CFDisposedException()
            : base()
        {
        }

        protected CFDisposedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public CFDisposedException(string message)
            : base(message, null)
        {

        }

        public CFDisposedException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

    }

    /// <summary>
    /// Exception raised when opening a file with invalid header
    /// or not supported COM/OLE Structured storage version.
    /// </summary>
    [Serializable]
    public class CFFileFormatException : CFException
    {
        public CFFileFormatException()
            : base()
        {
        }

        protected CFFileFormatException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public CFFileFormatException(string message)
            : base(message, null)
        {

        }

        public CFFileFormatException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

    }

    /// <summary>
    /// Exception raised when a named stream or a storage object
    /// are not found in a parent storage.
    /// </summary>
    [Serializable]
    public class CFItemNotFound : CFException
    {
        protected CFItemNotFound(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public CFItemNotFound()
            : base("Entry not found")
        {
        }

        public CFItemNotFound(string message)
            : base(message, null)
        {

        }

        public CFItemNotFound(string message, Exception innerException)
            : base(message, innerException)
        {

        }

    }



}
