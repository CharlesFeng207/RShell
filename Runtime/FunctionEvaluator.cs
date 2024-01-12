using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace RShell
{
    public class FunctionEvaluator
    {
        private enum SyntaxType
        {
            MethodCall,
            ValueSet,
            ValueGet
        }

        private Assembly[] m_Assemblies = null;
        private StringBuilder m_StringBuilder = null;
        private readonly Dictionary<string, Type> m_TypeCache = new Dictionary<string, Type>();

        private readonly List<string> m_GlobalEnvironmentNameSpace = new List<string>()
        {
            "RShell",
            "UnityEngine"
        };

        public void AddGlobalEnvironmentNameSpace(string nameSpace)
        {
            m_GlobalEnvironmentNameSpace.Add(nameSpace);
        }

        public bool Execute(string code, out object returnObj)
        {
            m_Assemblies = AppDomain.CurrentDomain.GetAssemblies();
            if (m_StringBuilder == null) m_StringBuilder = new StringBuilder();
            return ExecuteInnernal(code, out returnObj);
        }

        private bool ExecuteInnernal(string code, out object returnObj)
        {
            returnObj = null;
            try
            {
                var parts = ParseCodePart(code.Trim());
                returnObj = FindRootType(ref parts);
                if (returnObj == null)
                    throw new Exception("Root type not found");

                while (parts.Count > 0)
                {
                    string p = parts[0];
                    parts.RemoveAt(0);

                    Type targetType;
                    object targetInstance;

                    if (returnObj is Type type)
                    {
                        targetType = type;
                        targetInstance = null;
                    }
                    else
                    {
                        if (returnObj == null)
                            throw new Exception($"Target instance is null when executing {p}");

                        targetType = returnObj.GetType();
                        targetInstance = returnObj;
                    }

                    switch (CheckSyntaxType(p))
                    {
                        case SyntaxType.MethodCall:
                            returnObj = ExecuteMethodCall(targetType, targetInstance, p);
                            break;
                        case SyntaxType.ValueGet:
                            returnObj = ExecuteValueGet(targetType, targetInstance, p);
                            break;
                        case SyntaxType.ValueSet:
                            returnObj = ExecuteValueSet(targetType, targetInstance, p);
                            break;
                    }
                }

            }
            catch (Exception ex)
            {
                string innterEx = "";
                if(ex.InnerException != null)
                {
                    innterEx = $"InnerException: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}";
                }

                returnObj = $"Error executing code: {code}\n{ex.Message}\n{ex.StackTrace}\n{innterEx}";
                return false;
            }

            return true;
        }


        private List<string> ParseCodePart(string code)
        {
            var parts = new List<string>(SyntaxSplit('.', code));
            return parts;
        }

        private List<string> SyntaxSplit(char splitChar, string code)
        {
            var parts = new List<string>();
            int j = 0;
            int inBracket = 0;
            bool inString = false;
            bool afterAssign = false;
            for (int i = 0; i < code.Length; i++)
            {
                if (code[i] == '"')
                {
                    inString = !inString;
                }

                if (!inString)
                {
                    if (code[i] == ';')
                    {
                        parts.Add(code.Substring(j, i - j));
                        return parts;
                    }
                    else if (code[i] == '(')
                    {
                        inBracket ++;
                    }
                    else if (code[i] == ')')
                    {
                        inBracket --;
                    }
                    else if(code[i] == '=')
                    {
                        afterAssign = true;
                    }
                }

                if (i == code.Length - 1)
                {
                    parts.Add(code.Substring(j, code.Length - j));
                }
                else if (code[i] == splitChar && inBracket == 0 && !afterAssign && !inString)
                {
                    parts.Add(code.Substring(j, i - j));
                    j = i + 1;
                }
            }

            return parts;
        }

        private SyntaxType CheckSyntaxType(string code)
        {
            if (code.Contains("="))
                return SyntaxType.ValueSet;
            
            if (code.Contains("(") && code.Contains(")"))
                return SyntaxType.MethodCall;

            return SyntaxType.ValueGet;
        }

        private Type FindRootType(ref List<string> parts)
        {
            var root = _FindRootType(ref parts, false);
            if (root == null)
            {
                root = _FindRootType(ref parts, true);
            }

            return root;
        }

        private Type _FindRootType(ref List<string> parts, bool useGlobalNamespace)
        {
            for (int i = 0; i < parts.Count; i++)
            {
                int testCount = parts.Count - i;
                if (useGlobalNamespace)
                {
                    foreach (var globalNameSpace in m_GlobalEnvironmentNameSpace)
                    {
                        var root = TestType(parts, testCount, globalNameSpace);
                        if (root != null)
                        {
                            parts.RemoveRange(0, testCount);
                            return root;
                        }
                    }
                }
                else
                {
                    var root = TestType(parts, testCount);
                    if (root != null)
                    {
                        parts.RemoveRange(0, testCount);
                        return root;
                    }
                }
            }

            return null;
        }

        private Type TestType(List<string> parts, int count, string globalNameSpace = null)
        {
            int maxInnerClassCount = count - 1;
            string originTypeName = null;
            for (int innerClassCount = 0; innerClassCount <= maxInnerClassCount; innerClassCount++)
            {
                var testTypeName = GetTypeName(parts, count, innerClassCount, globalNameSpace);
                if (originTypeName == null)
                {
                    originTypeName = testTypeName;
                    if (m_TypeCache.TryGetValue(originTypeName, out var value))
                    {
                        if (value != null)
                            return value;
                    }
                }

                foreach (Assembly assembly in m_Assemblies)
                {
                    var type = assembly.GetType(testTypeName);
                    if (type != null)
                    {
                        m_TypeCache[originTypeName] = type;
                        return type;
                    }
                }
            }

            m_TypeCache[originTypeName] = null;
            return null;
        }

        private string GetTypeName(List<string> parts, int count, int innerClassCount, string globalNameSpace)
        {
            m_StringBuilder.Clear();

            if (!string.IsNullOrEmpty(globalNameSpace))
            {
                m_StringBuilder.Append(globalNameSpace);
                m_StringBuilder.Append(".");
            }
            
            int innerClassIndex = count - 1 - innerClassCount;
            for (int i = 0; i < count; i++)
            {
                m_StringBuilder.Append(parts[i]);
                if (i != count - 1)
                {
                    if (i >= innerClassIndex)
                        m_StringBuilder.Append("+");
                    else
                        m_StringBuilder.Append(".");
                }
                    
            }
            return m_StringBuilder.ToString();
        }

        private object ProcessParameter(string parameterStr)
        {
            if (parameterStr == "null") return null;

            if (parameterStr.Length > 1 && parameterStr.StartsWith("\"") && parameterStr.EndsWith("\""))
            {
                var str = parameterStr.Substring(1, parameterStr.Length - 2);
                return str;
            }
                
            
            if (!double.TryParse(parameterStr, out _) && parameterStr.Contains("."))
            {
                if(ExecuteInnernal(parameterStr, out var returnObj))
                    return returnObj;
                throw new Exception($"ProcessParameter failed:\n{returnObj}");
            }
            
            return parameterStr;
        }

        #region ValueGetSet

        private void ExtractSetParameter(string code, out string leftStr, out object rightObj)
        {
            int i = code.IndexOf('=');
            leftStr = code.Substring(0, i).Trim();
            
            var rightStr = code.Substring(i + 1).Trim();
            rightObj = ProcessParameter(rightStr);
        }

        private object ExecuteValueSet(Type targetType, object targetInstance, string code)
        {
            ExtractSetParameter(code, out var leftStr, out var rightObj);

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                                 BindingFlags.NonPublic | BindingFlags.SetField | BindingFlags.SetProperty;
            
            PropertyInfo propertyInfo = targetType.GetProperty(leftStr, flags);
            if (propertyInfo != null)
            {
                propertyInfo.SetValue(targetInstance, Convert.ChangeType(rightObj, propertyInfo.PropertyType));
                return propertyInfo.GetValue(targetInstance);
            }

            FieldInfo fieldInfo = targetType.GetField(leftStr, flags);
            if (fieldInfo != null)
            {
                fieldInfo.SetValue(targetInstance, Convert.ChangeType(rightObj, fieldInfo.FieldType));
                return fieldInfo.GetValue(targetInstance);
            }

            throw new Exception($"Can't find property or field {leftStr} in type {targetType}");
        }

        private object ExecuteValueGet(Type targetType, object targetInstance, string code)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                                 BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.GetProperty | BindingFlags.FlattenHierarchy;
            PropertyInfo propertyInfo = targetType.GetProperty(code, flags);
            if (propertyInfo != null)
            {
                return propertyInfo.GetValue(targetInstance);
            }

            FieldInfo fieldInfo = targetType.GetField(code, flags);
            if (fieldInfo != null)
            {
                return fieldInfo.GetValue(targetInstance);
            }

            throw new Exception($"Can't find property or field {code} in type {targetType}");
        }

        #endregion

        #region MethodCall

        private object ExecuteMethodCall(Type targetType, object targetInstance, string code)
        {
            ExtractFunctionCall(code, out var methodName, out var inputObjs);

            var methods = targetType.GetMethods();
            foreach (var method in methods)
            {
                if (method.Name != methodName)
                    continue;

                ParameterInfo[] methodParameterInfos = method.GetParameters();
                if (methodParameterInfos.Length < inputObjs.Length)
                    continue;

                object[] parameters = new object[methodParameterInfos.Length];

                try
                {
                    for (int i = 0; i < methodParameterInfos.Length; i++)
                    {
						ParameterInfo expectedInfo = methodParameterInfos[i];
                        if(i >= inputObjs.Length)
                        {
                            parameters[i] = expectedInfo.HasDefaultValue ? expectedInfo.DefaultValue : null;
                            continue;
                        }

                        object inputObj = inputObjs[i];
                        if (inputObjs[i] == null)
                        {
                            parameters[i] = null;
                            continue;
                        }
                        
                        if (expectedInfo.ParameterType.IsInstanceOfType(inputObj))
                        {
                            parameters[i] = inputObj;
                        }
                        else
                        {
                            if(expectedInfo.ParameterType.IsEnum && int.TryParse(inputObj.ToString(), out var enumValue))
                            {
                                parameters[i] = Enum.ToObject(expectedInfo.ParameterType, enumValue);
                            }
                            else
                            {
                                parameters[i] = Convert.ChangeType(inputObj, expectedInfo.ParameterType);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    continue;
                }

                return method.Invoke(targetInstance, parameters);
            }

            throw new Exception($"Can't find method {targetType} {methodName} {Dumper.Do(inputObjs)}");
        }

        private void ExtractFunctionCall(string code, out string methodName, out object[] parameters)
        {
            var startIndex = code.IndexOf('(');
            var endIndex = code.LastIndexOf(')');
            methodName = code.Substring(0, startIndex).Trim();
            var parameterStr = code.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();
            if (string.IsNullOrEmpty(parameterStr))
            {
                parameters = Array.Empty<object>();
            }
            else
            {                
                var list = new List<object>();

                var parameterStrArr = SyntaxSplit(',', parameterStr).Select(x => x.Trim());
                foreach (var str in parameterStrArr)
                {
                    list.Add(ProcessParameter(str));
                }

                parameters = list.ToArray();
            }
        }

        #endregion
    }
}
