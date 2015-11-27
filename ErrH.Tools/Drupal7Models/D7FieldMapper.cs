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
                    Throw.IfNull(value, $"‹ID7Node› for “{outProp.Name}”");
                    fieldVal = und.TargetIds(((ID7Node)value).nid);
                    break;

                case D7FieldTypes.TermReference:
                    Throw.IfNull(value, $"‹D7Term› for “{outProp.Name}”");
                    fieldVal = und.TermIds(value.As<D7Term>().tid);
                    break;

                case D7FieldTypes.FileReference:
                    fieldVal = und.Fids(value.ToString().ToInt());
                    break;

                default:
                    throw Error.Unsupported(fieldAttr.FieldType);
            }
            outProp.SetValue(d7Node, fieldVal, null);
        }


        private static object MapCckField<T>(D7FieldAttribute fieldAttr, object value, T itemIn, PropertyInfo inProp)
        {
            if (fieldAttr.Has2Values)
                return WrapBothValues(value, fieldAttr, itemIn);

            if (inProp.PropertyType == typeof(bool))
                return und.Values((bool)value ? 1 : 0);

            return und.Values(value);
        }

        private static FieldUnd<Und2Values> WrapBothValues<T>
            (object value2, D7FieldAttribute att, T itemIn)
        {
            var value1 = att.GetValue1(itemIn);
            return und.Value1_2(value1, value2);
        }

    }
}
