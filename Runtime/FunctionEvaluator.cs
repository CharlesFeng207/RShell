using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
            returnObj = null;
            try
            {
                var parts = ParseCodePart(code.Trim());
                returnObj = FindRootType(ref parts);
                if (returnObj == null)
                    throw new Exception("Root type not found");

                while (parts.Count > 0)
                {
                    string p = parts.Dequeue();
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
                returnObj = $"Error executing code: {code}\n{ex.Message}\n{ex.StackTrace}";
                return false;
            }

            return true;
        }

        private Queue<string> ParseCodePart(string code)
        {
            var parts = new Queue<string>(SyntaxSplit('.', code));
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
            
            if (code.Contains("("))
                return SyntaxType.MethodCall;

            return SyntaxType.ValueGet;
        }

        private Type FindRootType(ref Queue<string> parts)
        {
            Assembly[] assemblies = null;
            var testParts = new Queue<string>(parts);
            var root = _FindRootType(ref testParts, ref assemblies, false);
            if (root == null)
            {
                testParts = new Queue<string>(parts);
                root = _FindRootType(ref testParts, ref assemblies, true);
            }

            parts = testParts;
            return root;
        }

        private Type _FindRootType(ref Queue<string> parts, ref Assembly[] assemblies, bool useGlobalNamespace)
        {
            string typeName = null;
            
            while (parts.Count > 0)
            {
                var p = parts.Dequeue();
                typeName = string.IsNullOrEmpty(typeName) ? p : $"{typeName}.{p}";
                if (useGlobalNamespace)
                {
                    foreach (var globalNameSpace in m_GlobalEnvironmentNameSpace)
                    {
                        string testTypeName = $"{globalNameSpace}.{typeName}";
                        var root = TestType(testTypeName, ref assemblies);
                        if (root != null)
                            return root;
                    }
                }
                else
                {
                    var root = TestType(typeName, ref assemblies);
                    if (root != null)
                        return root;
                }
            }

            return null;
        }

        private Type TestType(string typeName, ref Assembly[] assemblies)
        {
            var testTypeName = typeName;

            if (m_TypeCache.TryGetValue(testTypeName, out var value))
            {
                if (value != null)
                    return value;
            }

            if (assemblies == null) assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                var type = assembly.GetType(testTypeName);
                if (type != null)
                {
                    m_TypeCache[testTypeName] = type;
                    return type;
                }
            }

            m_TypeCache[testTypeName] = null;
            return null;
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
                if(Execute(parameterStr, out var returnObj))
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
                        
                        var t = inputObj.GetType();
                        if (t == expectedInfo.ParameterType || t.IsSubclassOf(expectedInfo.ParameterType))
                        {
                            parameters[i] = inputObj;
                        }
                        else
                        {
                            parameters[i] = Convert.ChangeType(inputObj, expectedInfo.ParameterType);    
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
