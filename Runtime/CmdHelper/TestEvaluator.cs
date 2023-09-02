namespace RShell
{
    /// <summary>
    /// For FunctionEvaluator UnitTest 
    /// </summary>
    public class TestEvaluator : TestEvaluatorSuper
    {
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