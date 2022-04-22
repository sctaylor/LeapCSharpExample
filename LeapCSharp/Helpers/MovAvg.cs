using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers
{
    public class MovAvg
    {
    /** Initialize your data structure here. */
        double runningTotal;
        int windowSize;
        Queue<double> buffer;

        public MovAvg(int inputSize)
        {
            /*initialize values*/
            runningTotal = 0.0;
            windowSize = inputSize;
            buffer = new Queue<double>(2000);
        }

        public double AddToAvg(double inputValue)
        {
            /*check if buffer is full*/
            if (buffer.Count() == windowSize)
            {
                /*subtract front value from running total*/
                runningTotal -= buffer.Peek();
                /*delete value from front of std::queue*/
                buffer.Dequeue();
            }
            /*add new value*/
            buffer.Enqueue(inputValue);
            /*update running total*/
            runningTotal += inputValue;
            /*calculate average*/
            return (double)(runningTotal / ((double)buffer.Count()));
        }

        public double GetAverage()
        {
            if ( buffer.Count() == 0 )
            {
                return 0;
            }
            return (double)(runningTotal / ((double)buffer.Count()));
        }

        public void Clear()
        {
            buffer.Clear();
            runningTotal = 0.0;
        }

        public void ChangeSize( int size)
        {
            windowSize = size;
            runningTotal = 0.0;
            buffer.Clear();
        }

        public int GetCount()
        {
            return buffer.Count();
        }
        
    }
}
