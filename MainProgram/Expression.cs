using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainProgram
{
    class Expression
    {
        private string literalString;

        public Expression()
        {
            literalString = "";
        }

        public void addChar(int character) // Adding a variable char to the string
        {
            switch (character)
            {
                case 0:
                    literalString += "A";
                    break;
                case 1:
                    literalString += "B";
                    break;
                case 2:
                    literalString += "C";
                    break;
                case 3:
                    literalString += "D";
                    break;
            }
        }

        public int getNumGates()
        {
            int numGates = 0;

            for(int i = 0; i < literalString.Length; i++)
            {
                if(literalString[i] == '.' || literalString[i] == '+' || literalString[i] == '^' || literalString[i] == '¬') // If the character is a gate
                {
                    numGates++; // Increase the counter
                }
            }

            return numGates;
        }

        public int getNumNOTGates()
        {
            int numGates = 0;

            for (int i = 0; i < literalString.Length; i++)
            {
                if (literalString[i] == '¬') // If the character is a NOT gate
                {
                    numGates++; // Increase the counter
                }
            }

            return numGates;
        }

        public List<string> convertRPN()
        {
            Queue<string> input = new Queue<string>();
            Stack<string> operatorStack = new Stack<string>();

            for (int i = 0; i < literalString.Length; i++) // Add the characters in the literal string as tokens in the input queue
            {
                input.Enqueue(Convert.ToString(literalString[i]));
            }

            string variables = "ABCD";
            string ops = "+.^¬";
            List<string> output = new List<string>();

            while (input.Count != 0) // Shunting Yard Algorithm
            {
                if (variables.Contains(input.Peek())) // If a variable is at the front of the input queue
                {
                    output.Add(input.Dequeue()); // Add the variable to the output
                }
                else if (ops.Contains(input.Peek())) // If an operator is at the front of the input queue
                {
                    if (input.Peek() == "+" || input.Peek() == "^") // If an OR or XOR gate is at the front of the input queue
                    {
                        while (operatorStack.Count != 0 && operatorStack.Peek() == "." || operatorStack.Count != 0 && operatorStack.Peek() == "¬") // AND and NOT gates take higher precedence over OR and XOR
                        {
                            output.Add(operatorStack.Pop()); // Add the operators to the output
                        }
                    }
                    else if (input.Peek() == ".") // If an AND gate is at the front of the input queue
                    {
                        while (operatorStack.Count != 0 && operatorStack.Peek() == "¬") // NOT gates take higher priority over AND gates
                        {
                            output.Add(operatorStack.Pop()); // Add the operators to the output
                        }
                    }
                    operatorStack.Push(input.Dequeue()); // Add the front of the input queue to the operator stack
                }
                else if (input.Peek() == "(") // If an open bracket is at the front of the input queue
                {
                    operatorStack.Push(input.Dequeue()); // Add the bracke to the operator stack
                }
                else if (input.Peek() == ")") // If a close bracket is at the front of the input queue
                {
                    while (operatorStack.Count != 0 && operatorStack.Peek() != "(") // If there are operators on the stack and not an openbracket
                    {
                        output.Add(operatorStack.Pop()); // Add the operators to the output
                    }
                    if (operatorStack.Count > 0) // Remove any remaining operators or open brackets
                    {
                        operatorStack.Pop();
                    }
                    else if (operatorStack.Count == 0) // If there are no more operators or open brackets
                    {
                        break;
                    }
                    input.Dequeue(); // Remove the input
                }
            }

            while (operatorStack.Count != 0) // While there are operators remaining on the stack
            {
                output.Add(operatorStack.Pop()); // Add the operators to the output
            }

            return output;
        }

        public bool checkStringValidity()
        {
            for(int i = 0; i < literalString.Length - 1; i++)
            {
                if ("ABCD".Contains(literalString[i]) && "ABCD".Contains(literalString[i + 1]))
                {
                    return false;
                }
                else if ("ABCD".Contains(literalString[i]) && literalString[i + 1] == '¬')
                {
                    return false;
                }
                else if ("ABCD".Contains(literalString[i]) && literalString[i + 1] == '(')
                {
                    return false;
                }
                else if (literalString[i] == ')' && "ABCD".Contains(literalString[i + 1]))
                {
                    return false;
                }
            }

            return true;
        }

        public bool checkBracketValidity()
        {
            int openBrackets = 0; // The number of unclosed brackets

            for(int i = 0; i < literalString.Length; i++)
            {
                if(literalString[i] == '(') // If a bracket is opened
                {
                    openBrackets++; // Increase the number of unclosed brackets
                }
                else if(literalString[i] == ')') // If a bracket is closed
                {
                    openBrackets--; // Decrease the number of unclosed brackets
                }

                if(openBrackets < 0)
                {
                    return false;
                }
            }

            if(openBrackets == 0) // If all brackets are closed
            {
                return true;
            }
            else // If some brackets are open of too many are closed
            {
                return false;
            }
        }

        public void addAND()
        {
            literalString += ".";
        }

        public void addOR()
        {
            literalString += "+";
        }

        public void addXOR()
        {
            literalString += "^";
        }

        public void openBracket()
        {
            literalString += "(";
        }

        public void closeBracket()
        {
            literalString += ")";
        }

        public void startNOT()
        {
            literalString += "¬(";
        }

        public void clear()
        {
            literalString = "";
        }

        public string getExpression()
        {
            return literalString;
        }
    }
}
