using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Text;
using System.Threading.Tasks;

namespace MainProgram
{
    class Component
    {
        protected bool state;
        protected int[] location;
        protected int index;

        public Component(int[] position)
        {
            state = false;
            location = position;
        }

        public virtual void reduceNextIndex() // Method to be overwritten by children
        {}

        public virtual void resetNextIndex() // Method to be overwritten by children
        { }

        public virtual void setState(bool newState)
        {
            state = newState;
        }

        public void reduceIndex()
        {
            index--;
        }

        public void flipState()
        {
            state = !state;
        }

        public int getIndex()
        {
            return index;
        }

        public int[] getLocation()
        {
            return location;
        }

        public bool getState()
        {
            return state;
        }
    }

    class Pin : Component
    {
        private bool outputPin; // Is the pin an output pin
        private static char nextChar =  'A';
        private char pinChar; // The character of the pin
        private static int nextIndex = 1;
        private static List<char> spareChars = new List<char>();

        public Pin(int[] position) : base(position) // This constructor is used to create pin with the next available char
        {
            index = nextIndex;
            nextIndex++;
            outputPin = false;
            if(spareChars.Count > 0) // If a spare char exists use that
            {
                pinChar = spareChars.Last();
                spareChars.Remove(pinChar);
            }
            else // Otherwise use the next char
            {
                pinChar = nextChar;
                nextChar++;
            }
        }

        public Pin(int[] position, char specificChar) : base(position) // This constructor is used to create a pin with a specific char
        {
            index = nextIndex;
            nextIndex++;
            outputPin = false;
            pinChar = specificChar;
        }

        public void addSpareChar()
        {
            spareChars.Add(pinChar); // Add the pin's char to the list of spare chars
            spareChars.Sort(); // Keep the spare chars in alphabetical order
            spareChars.Reverse();
        }

        public override void resetNextIndex()
        {
            nextChar = 'A'; // Reset the indexes to their default values
            nextIndex = 1;
        }

        public override void reduceNextIndex()
        {
            nextIndex--;
        }

        public bool getIfOutput()
        {
            return outputPin;
        }

        public void flipOutput()
        {
            outputPin = !outputPin;
        }

        public char getChar()
        {
            return pinChar;
        }
    }

    class Wire : Component
    {
        private int[] endPoint; // Coordinate of the wire's end
        private List<int[]> turnPoints; // Coordinates of every point the wire turns at

        public Wire(int[] start) : base(start)
        {
            endPoint = new int[2];
            turnPoints = new List<int[]>();
        }

        public void finishWire()
        {
            if(turnPoints.Count > 0)
            {
                endPoint = turnPoints.Last(); // Set the final turnpoint as the endpoint
                turnPoints.RemoveAt(turnPoints.Count - 1); // Remove the final turnpoint from the list
            }
        }

        public void addTurnPoint(int[] newPoint)
        {
            turnPoints.Add(newPoint);
        }

        public List<int[]> getTurnPoints()
        {
            return turnPoints;
        }

        public int[] getEnd()
        {
            return endPoint;
        }

    }

    class Gate : Component
    {
        private bool[] inputs;
        private bool not;
        private int type; // 0 - And, 1 - Or, 2 - Xor, 3 - Not
        private static int nextIndex = 1;

        public Gate(int[] place, int gateType, bool isNot) : base(place)
        {
            inputs = new bool[2] { false, false };
            type = gateType;
            not = isNot;

            Output();

            index = nextIndex;
            nextIndex++;
        }

        public override void reduceNextIndex()
        {
            nextIndex--;
        }

        public override void resetNextIndex()
        {
            nextIndex = 1;
        }

        public void setState(int i, bool newState)
        {
            inputs[i] = newState;
        }

        public bool getInput(int num)
        {
            return inputs[num];
        }

        public bool getNot()
        {
            return not;
        }

        public int getGateType()
        {
            return type;
        }

        public void setLocation(int[] newPlace)
        {
            location = newPlace;
        }

        public void Output()
        {
            switch (type)
            {
                case 0: // AND gate
                    state = inputs[0] & inputs[1];
                    break;
                case 1: // OR gate
                    state = inputs[0] | inputs[1];
                    break;
                case 2: // XOR gate
                    state = inputs[0] ^ inputs[1];
                    break;
                case 3: // NOT gate
                    state = !inputs[0];
                    break;
            }
            if (not && type != 3) // If a NOT version of a gate, flip the state
            {
                state = !state;
            }
        }
    }
}
