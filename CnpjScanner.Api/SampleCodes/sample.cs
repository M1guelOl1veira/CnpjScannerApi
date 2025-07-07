using System;
using System.Collections.Generic;

namespace TestApp
{
    public class TestDeclarations
    {
        // Field (optional future enhancement)
        private int fieldNumber = 42;
        public long propertyNumber { get; set; }

        private int GetNumber()
        {
            return 123;
        }
        public void RunTests()
        {
            var result = GetNumber();
            // 1. Explicit declarations
            long cnpjLong = 98765432000190;

            // 2. var with inferred types
            var cnpjFromString = "12.345.678/0001-90";
            var inferredInt = 100;

            // 3. Declaration without initialization
            int notInitialized;
            notInitialized = 42;

            // 4. Multiple declarations in one line
            int x = 1, y = 2, z = 3;

            // 5. Const declaration

            // 6. For loop
            for (int i = 0; i < 5; i++)
            {
                Console.WriteLine(i);
            }

            // 7. Foreach loop with explicit int
            var numbers = new List<int> { 1, 2, 3 };
            foreach (int number in numbers)
            {
                Console.WriteLine(number);
            }

            // 8. Foreach with inferred type
            foreach (var num in numbers)
            {
                Console.WriteLine(num);
            }

            // 9. Tuple declaration
            (int a, int b) = (12345678, 900012);

            // 10. Variable with non-CNPJ string
            var description = "This is a normal string";

            // 11. Variable that looks like a CNPJ as string
            string cnpjLikeString = "12345678000190";
        }
    }
}
