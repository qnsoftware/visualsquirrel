/* see LICENSE notice in solution root */

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Debugger.Interop;
using VisualSquirrel.SquirrelDebuggerEngine;

namespace VisualSquirrel.Debugger.Engine
{
    // An implementation of IDebugProperty2
    // This interface represents a stack frame property, a program document property, or some other property. 
    // The property is usually the result of an expression evaluation. 
    //
    // The sample engine only supports locals and parameters for functions that have symbols loaded.
    class AD7Property : IDebugProperty2
    {
        
        private SquirrelDebugObject sqdbgobj;
        
        public AD7Property(SquirrelDebugObject dobj)
        {
        
            sqdbgobj = dobj;
        }

        // Construct a DEBUG_PROPERTY_INFO representing this local or parameter.
        public DEBUG_PROPERTY_INFO ConstructDebugPropertyInfo(enum_DEBUGPROP_INFO_FLAGS dwFields, bool hex)
        {
            
            DEBUG_PROPERTY_INFO propertyInfo = new DEBUG_PROPERTY_INFO();

            if ((dwFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_FULLNAME) != 0)
            {
                StringBuilder sb = new StringBuilder(sqdbgobj.Name);
                propertyInfo.bstrFullName = sb.ToString();
                propertyInfo.dwFields = (enum_DEBUGPROP_INFO_FLAGS)((uint)propertyInfo.dwFields | (uint)(DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_FULLNAME));
            }

            if ((dwFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_NAME) != 0)
            {
                StringBuilder sb = new StringBuilder(sqdbgobj.Name);
                propertyInfo.bstrName = sb.ToString();
                propertyInfo.dwFields = (enum_DEBUGPROP_INFO_FLAGS)((uint)propertyInfo.dwFields | (uint)(DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_NAME));
            }

            if ((dwFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_TYPE) != 0)
            {
                StringBuilder sb = new StringBuilder(sqdbgobj.Value.Type);
                propertyInfo.bstrType = sb.ToString();
                propertyInfo.dwFields = (enum_DEBUGPROP_INFO_FLAGS)((uint)propertyInfo.dwFields | (uint)(DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_TYPE));
            }

            if ((dwFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE) != 0)
            {
                string value = sqdbgobj.Value.Value;
                if (hex && sqdbgobj.Value.Type == "int")
                {
                    long n = Convert.ToInt64(value);
                    if (Math.Abs(n) > 0xFFFFFFFF)
                    {
                        
                        value = "0x" + n.ToString("X16");
                        
                    }
                    else
                    {
                        value = "0x" + ((int)n).ToString("X8");
                    }
                }
                else if (value == "{instance}")
                {
                    value = sqdbgobj.Value.Type;
                }
                propertyInfo.bstrValue = value;
                propertyInfo.dwFields = (enum_DEBUGPROP_INFO_FLAGS)((uint)propertyInfo.dwFields | (uint)(DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE));
            }

            if ((dwFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_ATTRIB) != 0)
            {
                // The sample does not support writing of values displayed in the debugger, so mark them all as read-only.
                propertyInfo.dwAttrib = (enum_DBG_ATTRIB_FLAGS)DBG_ATTRIB_FLAGS.DBG_ATTRIB_VALUE_READONLY;

                if (sqdbgobj.Value.Children != null)
                {
                    propertyInfo.dwAttrib |= (enum_DBG_ATTRIB_FLAGS)DBG_ATTRIB_FLAGS.DBG_ATTRIB_OBJ_IS_EXPANDABLE;
                }
            }

            // If the debugger has asked for the property, or the property has children (meaning it is a pointer in the sample)
            // then set the pProperty field so the debugger can call back when the chilren are enumerated.
            if (((dwFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_PROP) != 0) ||
                (sqdbgobj.Value.Children != null))
            {
                propertyInfo.pProperty = (IDebugProperty2)this;
                propertyInfo.dwFields = (enum_DEBUGPROP_INFO_FLAGS)((uint)propertyInfo.dwFields | (uint)(DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_PROP));
            }

            return propertyInfo;
        }

        #region IDebugProperty2 Members

        // Enumerates the children of a property. This provides support for dereferencing pointers, displaying members of an array, or fields of a class or struct.
        // The sample debugger only supports pointer dereferencing as children. This means there is only ever one child.
        public int EnumChildren(enum_DEBUGPROP_INFO_FLAGS dwFields, uint dwRadix, ref System.Guid guidFilter, enum_DBG_ATTRIB_FLAGS dwAttribFilter, string pszNameFilter, uint dwTimeout, out IEnumDebugPropertyInfo2 ppEnum)
        {
            ppEnum = null;
            bool hex = dwRadix == 16;
            if (sqdbgobj.Value.Children != null)
            {
                List<SquirrelDebugObject> children = sqdbgobj.Value.Children;
                DEBUG_PROPERTY_INFO[] properties = new DEBUG_PROPERTY_INFO[children.Count];
                int n = 0;
                foreach (SquirrelDebugObject sdo in children)
                {
                    properties[n++] = (new AD7Property(sdo)).ConstructDebugPropertyInfo(dwFields, hex);
                    ppEnum = new AD7PropertyEnum(properties);
                }
                return EngineConstants.S_OK;
            }

            return EngineConstants.S_FALSE;
        }

        // Returns the property that describes the most-derived property of a property
        // This is called to support object oriented languages. It allows the debug engine to return an IDebugProperty2 for the most-derived 
        // object in a hierarchy. This engine does not support this.
        public int GetDerivedMostProperty(out IDebugProperty2 ppDerivedMost)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        // This method exists for the purpose of retrieving information that does not lend itself to being retrieved by calling the IDebugProperty2::GetPropertyInfo 
        // method. This includes information about custom viewers, managed type slots and other information.
        // The sample engine does not support this.
        public int GetExtendedInfo(ref System.Guid guidExtendedInfo, out object pExtendedInfo)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        // Returns the memory bytes for a property value.
        public int GetMemoryBytes(out IDebugMemoryBytes2 ppMemoryBytes)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        // Returns the memory context for a property value.
        public int GetMemoryContext(out IDebugMemoryContext2 ppMemory)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        // Returns the parent of a property.
        // The sample engine does not support obtaining the parent of properties.
        public int GetParent(out IDebugProperty2 ppParent)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        // Fills in a DEBUG_PROPERTY_INFO structure that describes a property.
        public int GetPropertyInfo(enum_DEBUGPROP_INFO_FLAGS dwFields, uint dwRadix, uint dwTimeout, IDebugReference2[] rgpArgs, uint dwArgCount, DEBUG_PROPERTY_INFO[] pPropertyInfo)
        {
            bool hex = dwRadix == 16;
            pPropertyInfo[0] = new DEBUG_PROPERTY_INFO();
            rgpArgs = null;
            pPropertyInfo[0] = ConstructDebugPropertyInfo(dwFields, hex);
            return EngineConstants.S_OK;
        }

        //  Return an IDebugReference2 for this property. An IDebugReference2 can be thought of as a type and an address.
        public int GetReference(out IDebugReference2 ppReference)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        // Returns the size, in bytes, of the property value.
        public int GetSize(out uint pdwSize)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        // The debugger will call this when the user tries to edit the property's values
        // the sample has set the read-only flag on its properties, so this should not be called.
        public int SetValueAsReference(IDebugReference2[] rgpArgs, uint dwArgCount, IDebugReference2 pValue, uint dwTimeout)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        // The debugger will call this when the user tries to edit the property's values in one of the debugger windows.
        // the sample has set the read-only flag on its properties, so this should not be called.
        public int SetValueAsString(string pszValue, uint dwRadix, uint dwTimeout)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion

    }
}
