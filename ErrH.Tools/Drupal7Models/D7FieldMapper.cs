﻿using System;
using System.Reflection;
using ErrH.Tools.Drupal7Models.Entities;
using ErrH.Tools.Drupal7Models.FieldAttributes;
using ErrH.Tools.Drupal7Models.Fields;
using ErrH.Tools.Drupal7Models.FieldValues;
using ErrH.Tools.ErrorConstructors;
using ErrH.Tools.Extensions;

namespace ErrH.Tools.Drupal7Models
{
    public class D7FieldMapper
    {
        public static D7NodeBase Map<T>(T source, out string errorMsg)
        {
            errorMsg = null;
            D7NodeBase nod = null;

            try {
                nod = Map<T>(source);
            }
            catch (Exception ex)
            {
                errorMsg = ex.Details();
            }

            return nod;
        }


        public static D7NodeBase Map<T>(T source)
        {
            var typIn = source.GetType();
            var dtoAtt = typIn.GetAttribute<D7NodeDtoAttribute>
                                          (errofIfMissing: true);
            var typOut = dtoAtt.DtoType;
            var nodeOut = Activator.CreateInstance(typOut) as D7NodeBase;

            nodeOut.type = dtoAtt.MachineName;

            foreach (var inProp in typIn.PublicInstanceProps())
            {
                var att = inProp.GetAttribute<D7FieldAttribute>(true);
                if (att != null)
                {
                    var val = inProp.GetValue(source, null);
                    var outProp = typOut.GetProperty(att.FieldName);
                    SetFieldValue(nodeOut, inProp, outProp, att, val, source);
                }
            }
            return nodeOut.As<D7NodeBase>();
        }

        private static void SetFieldValue<T>(D7NodeBase d7Node,
                                             PropertyInfo inProp,
                                             PropertyInfo outProp,
                                             D7FieldAttribute fieldAttr,
                                             object value,
                                             T itemIn)
        {
            if (outProp == null) return;

            if (fieldAttr.FieldType == D7FieldTypes.DirectValue)
            {
                outProp.SetValue(d7Node, value, null);
                return;
            }

            object fieldVal = null;
            switch (fieldAttr.FieldType)
            {
                case D7FieldTypes.CckField:
                    fieldVal = MapCckField(fieldAttr, value, itemIn, inProp);
                    break;

                case D7FieldTypes.NodeReference:
                    //Throw.IfNull(value, $"‹ID7Node› for “{outProp.Name}”");
                    if (value != null)
                        fieldVal = und.TargetIds((value.As<ID7Node>()).nid);
                    break;

                case D7FieldTypes.TermReference:
                    fieldVal = MapTermRefField(outProp, value);
                    break;

                case D7FieldTypes.FileReference:
                    fieldVal = und.Fids(value.ToString().ToInt());
                    break;

                case D7FieldTypes.UserReference:
                    if (value != null)
                        fieldVal = und.TargetIds((value.As<D7User>()).uid);
                    break;

                default:
                    throw Error.Unsupported(fieldAttr.FieldType);
            }
            outProp.SetValue(d7Node, fieldVal, null);
        }



        private static object MapTermRefField(PropertyInfo outProp, object value)
        {
            Throw.IfNull(value, $"Term Ref value for “{outProp.Name}”");

            if (value is D7Term)
                return und.TermIds(value.As<D7Term>().tid);

            return und.TermIds((int)value);
        }



        private static object MapCckField<T>(D7FieldAttribute fieldAttr, object value, T itemIn, PropertyInfo inProp)
        {
            if (fieldAttr.Has2Values)
                return WrapBothValues(value, fieldAttr, itemIn);

            if (inProp.PropertyType == typeof(bool))
                return und.Values((bool)value ? 1 : 0);

            if (inProp.PropertyType == typeof(bool?))
            {
                var nulB = (bool?)value;
                if (nulB.HasValue)
                    return und.Values(nulB.Value ? 1 : 0);
                else
                    return und.Values(null);
            }

            if (inProp.PropertyType == typeof(DateTime))
                return und.Values(((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss"));

            if (inProp.PropertyType == typeof(DateTime?))
            {
                var nD8 = (DateTime?)value;
                if (nD8.HasValue)
                    return und.Values(nD8?.ToString("yyyy-MM-dd HH:mm:ss"));
                else
                    return und.Values(null);
            }

            return und.Values(value);
        }

        private static FieldUnd<Und2Values> WrapBothValues<T>
            (object value2, D7FieldAttribute att, T itemIn)
        {
            var value1 = att.GetValue1(itemIn);
            //return und.Value1_2(value1, value2);

            var d8Val1 = ((DateTime)value1).ToString("yyyy-MM-dd HH:mm:ss");
            var d8Val2 = ((DateTime)value2).ToString("yyyy-MM-dd HH:mm:ss");
            return und.Value1_2(d8Val1, d8Val2);
        }

    }
}
