//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Text;
//using OpenMcdf.Extensions.OLEProperties.Interfaces;
//using System.Collections;

//namespace OpenMcdf.Extensions.OLEProperties
//{

//    public class PropertyReader
//    {
//        public PropertyContext Context { get { return ctx; } }
//        private PropertyContext ctx = new PropertyContext();


//        public PropertyReader(int codePageOffset, BinaryReader br)
//        {
//            br.BaseStream.Seek(codePageOffset, SeekOrigin.Begin);

//            VTPropertyType vType = (VTPropertyType)br.ReadUInt16();
//            br.ReadUInt16(); // Ushort Padding

//            ITypedPropertyValue pr = PropertyFactory.Instance.NewProperty(vType, ctx);
//            pr.Read(br);

//            this.ctx.CodePage = (int)(ushort)(short)pr.Value;
//        }

      

       
//    }
//}
