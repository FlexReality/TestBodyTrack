using System.Collections.Generic;
using UnityEngine;

namespace FlexReality.BodyTracking
{
    public readonly struct MathQuestion
    {
        public readonly int A, B;
        public readonly bool IsAddition;

        public int Answer => IsAddition ? A + B : A - B;
        public string Display => IsAddition ? $"{A} + {B} = ?" : $"{A} - {B} = ?";

        public MathQuestion(int a, int b, bool add) { A = a; B = b; IsAddition = add; }

        public static MathQuestion GenerateRandom()
        {
            bool add = Random.value > 0.4f;
            int a, b;
            if (add)
            {
                a = Random.Range(1, 10);
                b = Random.Range(1, 11 - a); // a + b ≤ 10, both ≥ 1
            }
            else
            {
                a = Random.Range(2, 11);
                b = Random.Range(1, a); // result > 0
            }
            return new MathQuestion(a, b, add);
        }

        // Returns `count` wrong answers in range 0-10, picking nearby values first
        // so they serve as genuine distractors.
        public int[] WrongAnswers(int count)
        {
            int ans = Answer;
            var used = new HashSet<int> { ans };
            var result = new int[count];
            int filled = 0;

            for (int delta = 1; delta <= 10 && filled < count; delta++)
            {
                foreach (int v in new[] { ans + delta, ans - delta })
                {
                    if (v >= 0 && v <= 10 && used.Add(v))
                    {
                        result[filled++] = v;
                        if (filled >= count) return result;
                    }
                }
            }
            return result;
        }
    }
}
