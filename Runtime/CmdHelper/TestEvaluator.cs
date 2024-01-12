using System.Collections.Generic;

namespace RShell
{
    /// <summary>
    /// For FunctionEvaluator UnitTest 
    /// </summary>
    public class TestEvaluator : TestEvaluatorSuper
    {
        public class InnerClass
        {
            public class InnerInnerClass
            {
                public static int Value = 999;
            }

            public static int Value = 999;
        }

        public enum TestEnum
        {
            A = 0,
            B = 1,
            C = 2
        }

        private static int StaticPrivateValue;
        public static int StaticPublicValue;
        
        public static int StaticGetSetValue
        {
            get => StaticPrivateValue;
            set => StaticPrivateValue = value;
        }


        private static TestEvaluator m_Evaluator; 
        public static TestEvaluator GetInstance()
        {
            if (m_Evaluator == null) m_Evaluator = new TestEvaluator();
            return m_Evaluator;
        }
        
        public static float StaticAdd(float a, float b)
        {
            return a + b;
        }

        public static int TestOverload(int i)
        {
            return i + 1;
        }
        
        public static int TestDefaultValue(int i, int j = 1, int k = 1)
        {
            return i + j + k;
        }

        public static string TestObj(TestEvaluator a, string str, TestEvaluator b, int i, int j)
        {
            return string.Format("{0} {1} {2}", str, i, j);
        }

        public static string TestOverload(string str)
        {
            return str;
        }

        public static TestEnum TestEnumFunc(TestEnum e)
        {
            return e;
        }

        public static List<int> TestList()
        {
            return new List<int>() {5, 6, 7};
        }

        public static Dictionary<string, int> TestDic()
        {
            return new Dictionary<string, int>() {{"a", 1}, {"b", 2}, {"c", 3}};
        }
        
        
        public int PublicValue;
        public float PublicValue2;
        private int m_PrivateValue;
        
        public int GetSetValue
        {
            get => m_PrivateValue;
            set => m_PrivateValue = value;
        }
        
        public float Add(float a, float b)
        {
            return a + b;
        }
    }

    public class TestEvaluatorSuper
    {
        public int SuperValue = 3;
    }
    
}