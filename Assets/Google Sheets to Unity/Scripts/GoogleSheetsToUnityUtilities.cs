using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

namespace GoogleSheetsToUnity.Utils
{
    class GoogleSheetsToUnityUtilities
    {
        public static int GetIndexInAlphabet(string value)
        {
            if (value.Length > 1)
            {
                int rollingIndex = 0;
                for (int i = 0; i < value.Length; i++)
                {
                    rollingIndex += (IndexInAlphabet(value[0]) + 1) * 26; //first number times letter in alphabet
                    rollingIndex += IndexInAlphabet(value[1]);
                    return rollingIndex + 1;
                }
            }
            else
            {
                return IndexInAlphabet(value[0]) + 1;
            }

            //ERROR
            return 0;
        }

        private static int IndexInAlphabet(Char c)
        {
            // Uses the uppercase character unicode code point. 'A' = U+0042 = 65, 'Z' = U+005A = 90
            char upper = char.ToUpper(c);
            if (upper < 'A' || upper > 'Z')
            {
                throw new ArgumentOutOfRangeException("value", "This method only accepts standard Latin characters.");
            }

            return ((int)upper - (int)'A');
        }
    }
}
