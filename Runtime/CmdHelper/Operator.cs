using System.Collections;

namespace RShell
{
    public static class Operator
    {
        public static double Add(double a, double b)
        {
            return a + b;
        }

        public static double Sub(double a, double b)
        {
            return a - b;
        }

        public static double Mul(double a, double b)
        {
            return a * b;
        }

        public static object Index(IList obj, int index)
        {
            return obj[index];
        }

        public static object Index(IDictionary obj, object key)
        {
            return obj[key];
        }
    }
}