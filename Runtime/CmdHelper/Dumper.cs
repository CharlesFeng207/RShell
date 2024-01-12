using System;
using System.Collections;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace RShell
{
    public static class Dumper
    {
        private static StringBuilder m_TextBuilder;

        private static StringBuilder TextBuilder
        {
            get
            {
                if (m_TextBuilder == null)
                {
                    m_TextBuilder = new StringBuilder("", 1024);
                }

                return m_TextBuilder;
            }
        }

        public static string Do(object obj)
        {
            TextBuilder.Clear();

            try
            {
                DoDump(obj);
            }
            catch (Exception e)
            {
                TextBuilder.Append($"Dump Failed: {e.Message}");
            }

            return TextBuilder.ToString();
        }

        private static void DoDump(object obj)
        {
            if (obj == null)
            {
                TextBuilder.Append("null");
                return;
            }

            if (obj is Type)
            {
                TextBuilder.Append(((Type)obj).FullName);
                return;
            }

            Type t = obj.GetType();

            // Repeat field
            if (obj is IList)
            {
                var list = obj as IList;
                TextBuilder.Append("[");
                foreach (object v in list)
                {
                    DoDump(v);
                    TextBuilder.Append(", ");
                }

                TextBuilder.Append("]");
            }
            else if (t.IsValueType)
            {
                TextBuilder.Append(obj);
            }
            else if (obj is string)
            {
                TextBuilder.Append("\"");
                TextBuilder.Append(obj);
                TextBuilder.Append("\"");
            }
            else if (obj is IDictionary)
            {
                var dic = obj as IDictionary;
                TextBuilder.Append("{");
                foreach (DictionaryEntry item in dic)
                {
                    DoDump(item.Key);
                    TextBuilder.Append(":");
                    DoDump(item.Value);
                    TextBuilder.Append(", ");
                }

                TextBuilder.Append("}");
            }
            else if (t.IsClass)
            {
                TextBuilder.Append(t.Name);
                TextBuilder.Append("{");
                DumpClassObject(t, obj);
                TextBuilder.Append("}");
            }
            else
            {
                Debug.LogWarning($"unsupported type: {t.FullName}");
                TextBuilder.Append(obj);
            }
        }

        private static void DumpClassObject(Type objectType, object target)
        {
            PropertyInfo[] properties = objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo property in properties)
            {
                try
                {
                    object value = property.GetValue(target);
                    TextBuilder.Append($"{property.Name}={value} ");
                }
                catch (Exception e)
                {
                    TextBuilder.Append($"{property.Name}=? ");
                }
            }

            FieldInfo[] fieldInfos = objectType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo fieldInfo in fieldInfos)
            {
                try
                {
                    object value = fieldInfo.GetValue(target);
                    TextBuilder.Append($"{fieldInfo.Name}={value} ");
                }
                catch (Exception e)
                {
                    TextBuilder.Append($"{fieldInfo.Name}=? ");
                }
            }
        }
    }
}