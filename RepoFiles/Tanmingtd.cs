using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TanmingTest
{
    public class Tanmingtd
    {
        public static int test()
        {
            int sum = 0;
            if (TanmingClass1.a == 1 && TanmingClass2.a == 1 && TanmingClass3.a == 1)
            {
                int a1 = TanmingClass1.a;
                int a2 = TanmingClass2.a;
                int a3 = TanmingClass3.a;
                Console.Write("Right\n");
                sum = (a1 + a2 + a3)/3 +1;
            }
            else Console.Write("else\n");
            return sum;
        }
    }
}
