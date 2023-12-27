using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Text;
using System.Threading.Tasks;

namespace MainProgram
{
    class Circuit
    {
        private List<Gate> gates;
        private List<Wire> wires;
        private List<Pin> pins;

        public Circuit()
        {
            gates = new List<Gate>();
            wires = new List<Wire>();
            pins = new List<Pin>();
        }

        public void evaluateCircuit()
        {
            for (int i = 0; i < wires.Count; i++)
            {
                for (int j = 0; j < pins.Count; j++) // Checking if the start or end points of a wire i are at the same location as an output of a pin j
                {
                    if ((pins[j].getLocation()[0] + 20 == wires[i].getLocation()[0] && pins[j].getLocation()[1] + 20 == wires[i].getLocation()[1]) || (pins[j].getLocation()[0] + 20 == wires[i].getEnd()[0] && pins[j].getLocation()[1] + 20 == wires[i].getEnd()[1]))
                    {
                        wires[i].setState(pins[j].getState()); // Evaluate wires connected to input pins
                    }
                }

                for (int j = 0; j < gates.Count; j++) // These if statements check if the start or end points of wire i are at the same location as an input or output of a gate j
                {
                    if(gates[j].getNot() == true)
                    {
                        if ((gates[j].getLocation()[0] == wires[i].getLocation()[0] - 100 && gates[j].getLocation()[1] == wires[i].getLocation()[1] - 40) || (gates[j].getLocation()[0] == wires[i].getEnd()[0] - 100 && gates[j].getLocation()[1] == wires[i].getEnd()[1] - 40))
                        {
                            wires[i].setState(gates[j].getState()); // Evaluate wires connected to gate NOT outputs
                        }
                    }
                    else if (gates[j].getLocation()[0] == wires[i].getLocation()[0] - 80 && gates[j].getLocation()[1] == wires[i].getLocation()[1] - 40 || (gates[j].getLocation()[0] == wires[i].getEnd()[0] - 80 && gates[j].getLocation()[1] == wires[i].getEnd()[1] - 40))
                    {
                        wires[i].setState(gates[j].getState()); // Evaluate wires connected to gate outputs
                    }

                    if (gates[j].getGateType() == 3)
                    {
                        if ((gates[j].getLocation()[0] == wires[i].getEnd()[0] && gates[j].getLocation()[1] + 40 == wires[i].getEnd()[1]) || (gates[j].getLocation()[0] == wires[i].getLocation()[0] && gates[j].getLocation()[1] + 40 == wires[i].getLocation()[1]))
                        {
                            gates[j].setState(0, wires[i].getState()); // Set NOT gate state to connected wire state
                            gates[j].Output(); // Evaluate gate output
                        }
                    }
                    else if ((gates[j].getLocation()[0] == wires[i].getLocation()[0] && gates[j].getLocation()[1] + 20 == wires[i].getLocation()[1]) || (gates[j].getLocation()[0] == wires[i].getEnd()[0] && gates[j].getLocation()[1] + 20 == wires[i].getEnd()[1]))
                    {
                        gates[j].setState(0, wires[i].getState()); // Set first gate state to connected wire state
                        gates[j].Output(); // Evaluate gate output
                    }
                    else if ((gates[j].getLocation()[0] == wires[i].getEnd()[0] && gates[j].getLocation()[1] + 60 == wires[i].getEnd()[1]) || (gates[j].getLocation()[0] == wires[i].getLocation()[0] && gates[j].getLocation()[1] + 60 == wires[i].getLocation()[1]))
                    {
                        gates[j].setState(1, wires[i].getState()); // Set second gate state to connected wire state
                        gates[j].Output(); // Evaluate gate output
                    }
                }

                for (int j = 0; j < wires.Count; j++) // Wire to wire connections to the wire j
                {
                    if (checkIfOnWire(j, wires[i].getLocation()))
                    {
                        wires[i].setState(wires[j].getState());
                    }
                    else if (checkIfOnWire(j, wires[i].getEnd()))
                    {
                        wires[i].setState(wires[j].getState());
                    }
                }

                for (int j = 0; j < pins.Count; j++) // Checking if the start or end points of a wire i are at the same location as an input of a pin j
                {
                    if ((pins[j].getLocation()[0] == wires[i].getEnd()[0] && pins[j].getLocation()[1] == wires[i].getEnd()[1] - 20) || (pins[j].getLocation()[0] == wires[i].getLocation()[0] && pins[j].getLocation()[1] == wires[i].getLocation()[1] - 20))
                    {
                        if (pins[j].getIfOutput() && pins[j].getState() != wires[i].getState()) // If pin is an output pin and its state is not the same as the wire connecting to it
                        {
                            pins[j].flipState(); // Flip the state of the pin
                        }
                    }
                }
            }
        }

        public bool checkIfOnWire(int j, int[] checkLocation) // J is index of wire to be checked
        {
            if (wires[j].getTurnPoints().Count > 0) // If the wire has turnpoints
            {
                // FOR FIRST SEGMENT
                if (checkLocation[1] == wires[j].getLocation()[1]) // If the checkLocation is in the same y plane as the start of wire j
                {
                    if (wires[j].getTurnPoints()[0][0] > wires[j].getLocation()[0]) // If wire j heads to the right initially
                    {
                        if (checkLocation[0] >= wires[j].getLocation()[0]) // If the checkLocation is to the right of the start of wire j
                        {
                            if (checkLocation[0] <= wires[j].getTurnPoints()[0][0]) // If the checkLocation is to the left of the first turnpoint
                            {
                                return true;
                            }
                        }
                    }
                    else if (wires[j].getTurnPoints()[0][0] < wires[j].getLocation()[0]) // If wire j heads to the left initially
                    {
                        if (checkLocation[0] <= wires[j].getLocation()[0]) // If the checkLocation is to the left of the start of wire j
                        {
                            if (checkLocation[0] >= wires[j].getTurnPoints()[0][0]) // If the checkLocation is to the right of the first turnpoint
                            {
                                return true;
                            }
                        }
                    }
                }
                else if (checkLocation[0] == wires[j].getTurnPoints()[0][0]) // If the checkLocation is in the same x plane as the first turnpoint
                {
                    if (wires[j].getTurnPoints()[0][1] < wires[j].getLocation()[1]) // If wire j heads upwards initially
                    {
                        if (checkLocation[1] <= wires[j].getLocation()[1]) // If the checkLocation is above the start of wire j
                        {
                            if (checkLocation[1] >= wires[j].getTurnPoints()[0][1]) // If the checkLocation is below the first turnpoint
                            {
                                return true;
                            }
                        }
                    }
                    else if (wires[j].getTurnPoints()[0][1] > wires[j].getLocation()[1]) // If wire j heads downwards initially
                    {
                        if (checkLocation[1] >= wires[j].getLocation()[1]) // If the checkLocation is below the start of wire j
                        {
                            if (checkLocation[1] <= wires[j].getTurnPoints()[0][1]) // If the checkLocation is above the first turnpoint
                            {
                                return true;
                            }
                        }
                    }
                }

                // FOR MIDDLE SEGMENTS
                for (int k = 0; k < wires[j].getTurnPoints().Count - 1; k++)
                {
                    if (checkLocation[1] == wires[j].getTurnPoints()[k][1]) // If the checkLocation is in the same y plane as the kth turnpoint
                    {
                        if (wires[j].getTurnPoints()[k + 1][0] > wires[j].getTurnPoints()[k][0]) // If wire j heads to the right after the kth turnpoint
                        {
                            if (checkLocation[0] >= wires[j].getTurnPoints()[k][0]) // If the checkLocation is to the right of the kth turnpoint
                            {
                                if (checkLocation[0] <= wires[j].getTurnPoints()[k + 1][0]) // If the checkLocation is to the left of the k+1th turnpoint
                                {
                                    return true;
                                }
                            }
                        }
                        else if (wires[j].getTurnPoints()[k + 1][0] < wires[j].getTurnPoints()[k][0]) // If wire j heads to the left after the kth turnpoint
                        {
                            if (checkLocation[0] <= wires[j].getTurnPoints()[k][0]) // If the checkLocation is to the left of the kth turnpoint
                            {
                                if (checkLocation[0] >= wires[j].getTurnPoints()[k + 1][0]) // If the checkLocation is to the right of the k+1th turnpoint
                                {
                                    return true;
                                }
                            }
                        }
                    }
                    else if (checkLocation[0] == wires[j].getTurnPoints()[k + 1][0]) // If the checkLocation is in the same x plane as the kth turnpoint
                    {
                        if (wires[j].getTurnPoints()[k + 1][1] < wires[j].getTurnPoints()[k][1]) // If wire j heads upwards after the kth turnpoint
                        {
                            if (checkLocation[1] <= wires[j].getTurnPoints()[k][1]) // If the checkLocation is above the start of the kth turnpoint
                            {
                                if (checkLocation[1] >= wires[j].getTurnPoints()[k + 1][1]) // If the checkLocation is below the k+1th turnpoint
                                {
                                    return true;
                                }
                            }
                        }
                        else if (wires[j].getTurnPoints()[k + 1][1] > wires[j].getTurnPoints()[k][1]) // If wire j heads downwards after the kth turnpoint
                        {
                            if (checkLocation[1] >= wires[j].getTurnPoints()[k][1]) // If the checkLocation is below the kth turnpoint
                            {
                                if (checkLocation[1] <= wires[j].getTurnPoints()[k + 1][1]) // If the checkLocation is above the k+1th turnpoint
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }

                // FOR FINAL SEGMENT
                if (checkLocation[1] == wires[j].getTurnPoints()[wires[j].getTurnPoints().Count - 1][1]) // If the checkLocation is in the same y plane as the kth turnpoint
                {
                    if (wires[j].getEnd()[0] > wires[j].getTurnPoints()[wires[j].getTurnPoints().Count - 1][0]) // If wire j heads to the right after the kth turnpoint
                    {
                        if (checkLocation[0] >= wires[j].getTurnPoints()[wires[j].getTurnPoints().Count - 1][0]) // If the checkLocation is to the right of the kth turnpoint
                        {
                            if (checkLocation[0] <= wires[j].getEnd()[0]) // If the checkLocation is to the left of the k+1th turnpoint
                            {
                                return true;
                            }
                        }
                    }
                    else if (wires[j].getEnd()[0] < wires[j].getTurnPoints()[wires[j].getTurnPoints().Count - 1][0]) // If wire j heads to the left after the kth turnpoint
                    {
                        if (checkLocation[0] <= wires[j].getTurnPoints()[wires[j].getTurnPoints().Count - 1][0]) // If the checkLocation is to the left of the kth turnpoint
                        {
                            if (checkLocation[0] >= wires[j].getEnd()[0]) // If the checkLocation is to the right of the k+1th turnpoint
                            {
                                return true;
                            }
                        }
                    }
                }
                else if (checkLocation[0] == wires[j].getEnd()[0]) // If the checkLocation is in the same x plane as the kth turnpoint
                {
                    if (wires[j].getEnd()[1] < wires[j].getTurnPoints()[wires[j].getTurnPoints().Count - 1][1]) // If wire j heads upwards after the kth turnpoint
                    {
                        if (checkLocation[1] <= wires[j].getTurnPoints()[wires[j].getTurnPoints().Count - 1][1]) // If the checkLocation is above the start of the kth turnpoint
                        {
                            if (checkLocation[1] >= wires[j].getEnd()[1]) // If the checkLocation is below the k+1th turnpoint
                            {
                                return true;
                            }
                        }
                    }
                    else if (wires[j].getEnd()[1] > wires[j].getTurnPoints()[wires[j].getTurnPoints().Count - 1][1]) // If wire j heads downwards after the kth turnpoint
                    {
                        if (checkLocation[1] >= wires[j].getTurnPoints()[wires[j].getTurnPoints().Count - 1][1]) // If the checkLocation is below the kth turnpoint
                        {
                            if (checkLocation[1] <= wires[j].getEnd()[1]) // If the checkLocation is above the k+1th turnpoint
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            else // If there are no turnpoints in the wire, it is a single segment
            {
                if (checkLocation[1] == wires[j].getLocation()[1]) // If the checkLocation is in the same y plane as the start of wire j
                {
                    if (wires[j].getEnd()[0] > wires[j].getLocation()[0]) // If wire j heads to the right initially
                    {
                        if (checkLocation[0] >= wires[j].getLocation()[0]) // If the checkLocation is to the right of the start of wire j
                        {
                            if (checkLocation[0] <= wires[j].getEnd()[0]) // If the checkLocation is to the left of the first turnpoint
                            {
                                return true;
                            }
                        }
                    }
                    else if (wires[j].getEnd()[0] < wires[j].getLocation()[0]) // If wire j heads to the left initially
                    {
                        if (checkLocation[0] <= wires[j].getLocation()[0]) // If the checkLocation is to the left of the start of wire j
                        {
                            if (checkLocation[0] >= wires[j].getEnd()[0]) // If the checkLocation is to the right of the first turnpoint
                            {
                                return true;
                            }
                        }
                    }
                }
                else if (checkLocation[0] == wires[j].getEnd()[0]) // If the checkLocation is in the same x plane as the first turnpoint
                {
                    if (wires[j].getEnd()[1] < wires[j].getLocation()[1]) // If wire j heads upwards initially
                    {
                        if (checkLocation[1] <= wires[j].getLocation()[1]) // If the checkLocation is above the start of wire j
                        {
                            if (checkLocation[1] >= wires[j].getEnd()[1]) // If the checkLocation is below the first turnpoint
                            {
                                return true;
                            }
                        }
                    }
                    else if (wires[j].getEnd()[1] > wires[j].getLocation()[1]) // If wire j heads downwards initially
                    {
                        if (checkLocation[1] >= wires[j].getLocation()[1]) // If the checkLocation is below the start of wire j
                        {
                            if (checkLocation[1] <= wires[j].getEnd()[1]) // If checkLocation is above the first turnpoint
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        public void reduceGateIndexes(int startPoint)
        {
            for(int i = startPoint; i < gates.Count; i++)
            {
                gates[i].reduceIndex(); // Reduce the indexes of all the gates above a certain threshold index
            }
        }

        public void reducePinIndexes(int startPoint)
        {
            for(int i = startPoint; i < pins.Count; i++)
            {
                pins[i].reduceIndex(); // Reduce the indexes of all the pins above a certain threshold index
            }
        }

        public Gate getGate(int index)
        {
            return gates[index];
        }

        public Wire getWire(int index)
        {
            return wires[index];
        }

        public Pin getPin(int index)
        {
            return pins[index];
        }

        public int getNum(int type) // 0 Gates, 1 Wires, 2 Pins
        {
            switch (type)
            {
                case 0:
                    return gates.Count;
                case 1:
                    return wires.Count;
                case 2:
                    return pins.Count;
            }

            return -1;
        }

        public int getNum(bool isInput) // Returns the number of input or output pins
        {
            int counter = 0;
            if (isInput)
            {
                for(int i = 0; i < pins.Count; i++) // Loop through pins
                {
                    if (pins[i].getIfOutput()) // If the pin is an output pin
                    {
                        counter++; // Increase the counter
                    }
                }
            }
            else
            {
                for (int i = 0; i < pins.Count; i++) // Loop through pins
                {
                    if (!pins[i].getIfOutput()) // If the pin is not an output pin
                    {
                        counter++; // Increase the counter
                    }
                }
            }
            return counter;
        }

        public void removeComponent(int type, int index) // 0 Gates, 1 Wires, 2 Pins
        {
            switch (type)
            {
                case 0:
                    gates.RemoveAt(index);
                    break;
                case 1:
                    wires.RemoveAt(index);
                    break;
                case 2:
                    pins.RemoveAt(index);
                    break;
            }
        }

        public void addGate(Gate newGate)
        {
            gates.Add(newGate);
        }

        public void addWire(Wire newWire)
        {
            wires.Add(newWire);
        }

        public void addPin(Pin newPin)
        {
            pins.Add(newPin);
        }
    }
}
