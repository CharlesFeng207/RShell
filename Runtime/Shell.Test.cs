using System;
using System.Text;
using UnityEngine.Assertions;

namespace RShell
{
    public static partial class Shell
    {
        public static string TestSelf()
        {
            try
            {
                object result;
                float num;

                result = Execute("UnityEngine.Application.targetFrameRate;f");
                Assert.IsTrue(float.TryParse(result.ToString(), out _));

                result = Execute("Time.time;");
                Assert.IsTrue(float.TryParse(result.ToString(), out _));

                result = Execute("RShell.TestEvaluator.StaticAdd(1.0, 2.9)");
                num = float.Parse(result.ToString());
                Assert.IsTrue(num == 3.9f);

                result = Execute("TestEvaluator.GetInstance().Add(1.0, 2.9);");
                num = float.Parse(result.ToString());
                Assert.IsTrue(num == 3.9f);

                result = Execute("TestEvaluator.StaticPrivateValue = 1");
                Assert.IsTrue(result.ToString() == "1");
                
                result = Execute("TestEvaluator.StaticPublicValue = 1");
                Assert.IsTrue(result.ToString() == "1");
                
                result = Execute("TestEvaluator.StaticGetSetValue = 1; ");
                Assert.IsTrue(result.ToString() == "1");
                
                result = Execute("TestEvaluator.GetInstance().PublicValue = 1");
                Assert.IsTrue(result.ToString() == "1");
                
                result = Execute("TestEvaluator.GetInstance().m_PrivateValue = 1");
                Assert.IsTrue(result.ToString() == "1");
                
                result = Execute("TestEvaluator.GetInstance().GetSetValue = 1");
                Assert.IsTrue(result.ToString() == "1");

                result = Execute("TestEvaluator.StaticPrivateValue;");
                Assert.IsTrue(result.ToString() == "1");

                result = Execute("TestEvaluator.StaticPublicValue");
                Assert.IsTrue(result.ToString() == "1");

                result = Execute("TestEvaluator.StaticGetSetValue");
                Assert.IsTrue(result.ToString() == "1");

                result = Execute("TestEvaluator.GetInstance().PublicValue");
                Assert.IsTrue(result.ToString() == "1");

                result = Execute("TestEvaluator.GetInstance().m_PrivateValue");
                Assert.IsTrue(result.ToString() == "1");

                result = Execute("TestEvaluator.GetInstance().GetSetValue");
                Assert.IsTrue(result.ToString() == "1");
                
                result = Execute("TestEvaluator.GetInstance().SuperValue");
                Assert.IsTrue(result.ToString() == "3");
                
                result = Execute("TestEvaluator.StaticAdd(RShell.TestEvaluator.StaticPublicValue, TestEvaluator.StaticAdd(1, 1))");
                Assert.IsTrue(result.ToString() == "3");

                Execute("TestEvaluator.StaticPublicValue = TestEvaluator.StaticAdd(1,1)");
                result = Execute("TestEvaluator.StaticPublicValue");
                Assert.IsTrue(result.ToString() == "2");
                
                Execute("TestEvaluator.StaticPublicValue = TestEvaluator.TestOverload(TestEvaluator.StaticAdd(10,10))");
                result = Execute("TestEvaluator.StaticPublicValue");
                Assert.IsTrue(result.ToString() == "21");
                
                result = Execute("TestEvaluator.TestOverload(\"TestEvaluator.StaticAdd(10,10)\")");
                Assert.IsTrue(result.ToString() == "TestEvaluator.StaticAdd(10,10)");
                
                result = Execute("TestEvaluator.TestObj(TestEvaluator.GetInstance(), \"hello world\", TestEvaluator.GetInstance(), 10, 10);");
                Assert.IsTrue(result.ToString() == "hello world 10 10");
                
                result = Execute("TestEvaluator.TestDefaultValue(1)");
                Assert.IsTrue(result.ToString() == "3");
                
                result = Execute("TestEvaluator.GetInstance()");
                Assert.IsNotNull(result);
                
                Execute("TestEvaluator.m_Evaluator = null");
                result = Execute("TestEvaluator.m_Evaluator");
                Assert.IsNull(result);
                
                return "Test Complete!";
            }
            catch (Exception e)
            {
                return $"{e.Message}\n{e.StackTrace}";
            }
        }

        public static string TestLargeStr(int count)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                sb.AppendLine($"TestLargeStr: {i}");
            }

            return sb.ToString();
        }
    }
}
