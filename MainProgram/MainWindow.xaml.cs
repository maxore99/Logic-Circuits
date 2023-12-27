using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.IO;
using Microsoft.Win32;
using Path = System.Windows.Shapes.Path; // There is an ambiguous reference between System.IO paths and System.Windows.Shapes paths

namespace MainProgram
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int selection;
        private Path[] currentComponent;
        private bool currentNot;
        private Circuit mainCircuit;
        private bool drawingWire;
        private PathGeometry entireWire;
        private int[] savedCoords;
        private Wire currentWire;
        private Path[] wireHead;
        private List<Path> gatePaths;
        private List<Path> wirePaths;
        private List<Path> pinPaths;
        private List<TextBox> labels;
        private int selectedIndex;
        private Button[] infoButtons;
        private Button[] inputButtons;
        private int selectedComponentType;
        private int numInputs;
        private Expression mainExpression;
        private bool GCSESelection;
        private bool deleteMode;
        private int highlightedIndex;
        private int[] totalOffset;
        private bool failedConstruction;

        public MainWindow()
        {
            InitializeComponent();
            drawCanvas();

            gatePaths = new List<Path>();
            wirePaths = new List<Path>();
            pinPaths = new List<Path>();
            labels = new List<TextBox>();
            mainCircuit = new Circuit();
            inputButtons = new Button[4];
            infoButtons = new Button[2];
            savedCoords = new int[2];
            totalOffset = new int[2];

            selectedComponentType = -1; // 0 Gate, 1 Wire, 2 Pin

            DispatcherTimer timer = new DispatcherTimer();
            timer.Tick += timerTick;
            timer.Interval = new TimeSpan(0, 0, 0, 0, 1);
            timer.Start();

            selectedIndex = -1;
            wireHead = new Path[2];
            entireWire = new PathGeometry();
            selection = -1;
            currentComponent = new Path[2];

            deleteMode = false;
            highlightedIndex = -1;

            failedConstruction = false;
            mainExpression = new Expression();
            addNewInputButton(1); // Add the 'A' input button for the boolean
        }

        #region Canvas Interactions
        private void timerTick(object sender, EventArgs e) // The timer handles visual updates that need to happen as the user moves their mouse over the canvas
        {
            if(Mouse.GetPosition(cnvMain).X < cnvMain.Width && Mouse.GetPosition(cnvMain).Y < cnvMain.Height) // If the mouse is over the canvas, draw a ghost
            {
                int[] mouseCoords = new int[] { (((int)Mouse.GetPosition(cnvMain).X / 20) * 20), (((int)Mouse.GetPosition(cnvMain).Y / 20) * 20) }; // The coordinates of the mouse snapped to the grid
                cnvMain.Children.Remove(currentComponent[0]); // Remove the previous ghost
                currentComponent[0] = currentComponent[1]; // Delete the previous ghost from the array

                for (int i = 0; i < gatePaths.Count; i++) // Components sometimes get stuck red when moused over during deletion, these loops correct this if it arises
                {
                    if (gatePaths[i].Stroke == Brushes.Red) // If a gate is coloured red still, colour it black
                    {
                        gatePaths[i].Stroke = Brushes.Black;
                    }
                }
                for(int i = 0; i < pinPaths.Count; i++)
                {
                    if(pinPaths[i].Stroke == Brushes.Red) // If a pin is coloured red still, colour it black
                    {
                        pinPaths[i].Stroke = Brushes.Black;
                    }
                }
                for(int i = 0; i < wirePaths.Count; i++)
                {
                    if(wirePaths[i].Stroke == Brushes.Red) // If a wire is coloured red still, parse the circuit to reset its colour to blue or aqua
                    {
                        parseCircuit();
                    }
                }

                if (deleteMode)
                {
                    for (int i = 0; i < mainCircuit.getNum(0); i++) // For loop runs for the number of gates in the circuit
                    {
                        if (mainCircuit.getGate(i).getLocation()[0] <= mouseCoords[0] && mainCircuit.getGate(i).getLocation()[0] + 80 >= mouseCoords[0] && mainCircuit.getGate(i).getLocation()[1] + 60 >= mouseCoords[1] && mainCircuit.getGate(i).getLocation()[1] <= mouseCoords[1])// If this gate is moused over
                        {
                            gatePaths[i].Stroke = Brushes.Red; // Colour the gate red and record which gate is highlighted
                            highlightedIndex = i;
                            selectedComponentType = 0;
                            break;
                        }
                        else if(highlightedIndex > -1 && selectedComponentType == 0) // If a different gate is highlighted
                        {
                            gatePaths[highlightedIndex].Stroke = Brushes.Black; // Unhighlight the gate and remove the highlighted gate's index
                            selectedComponentType = -1;
                            highlightedIndex = -1;
                        }
                    }

                    for (int i = 0; i < mainCircuit.getNum(2); i++) // Loop through the pins
                    {
                        if (mouseCoords[0] == mainCircuit.getPin(i).getLocation()[0] && (mouseCoords[1] == mainCircuit.getPin(i).getLocation()[1] || mouseCoords[1] == mainCircuit.getPin(i).getLocation()[1] + 20)) // If this pin is moused over
                        {
                            pinPaths[i].Stroke = Brushes.Red; // Colour the pin red and record which pin is highlighted
                            highlightedIndex = i;
                            selectedComponentType = 2;
                            break;
                        }
                        else if(highlightedIndex > -1 && selectedComponentType == 2) // If a different pin is highlighted
                        {
                            pinPaths[highlightedIndex].Stroke = Brushes.Black; // Unhighlight the pin and remove the pin's index
                            selectedComponentType = -1;
                            highlightedIndex = -1;
                        }
                    }

                    for(int i = 0; i < mainCircuit.getNum(1); i++) // Loop through the wires
                    {
                        if(mainCircuit.checkIfOnWire(i, mouseCoords)) // If this wire is moused over
                        {
                            wirePaths[i].Stroke = Brushes.Red; // Colour the wire red and record the wire's index
                            highlightedIndex = i;
                            selectedComponentType = 1;
                            break;
                        }
                        else if(highlightedIndex > -1 && selectedComponentType == 1) // If a different wire is highlighted
                        {
                            parseCircuit(); // Parse the circuit to set the wire colour back to blue or aqua and remove the wire's index
                            selectedComponentType = -1;
                            highlightedIndex = -1;
                        }
                    }

                    for(int i = 0; i < labels.Count; i++) // Loop through the labels
                    {
                        if (Canvas.GetLeft(labels[i]) <= mouseCoords[0] && Canvas.GetLeft(labels[i]) + labels[i].ActualWidth >= mouseCoords[0] && Canvas.GetTop(labels[i]) + 5 == mouseCoords[1]) // If a label is moused over
                        {
                            labels[i].Background = Brushes.Red; // Set the label's background to red and record the label's index
                            highlightedIndex = i;
                            selectedComponentType = 3;
                            break;
                        }
                        else if (highlightedIndex > -1 && selectedComponentType == 3) // If a different label is highlighted
                        {
                            labels[highlightedIndex].Background = Brushes.Transparent; // Clear the background of the label and remove the label's index
                            selectedComponentType = -1;
                            highlightedIndex = -1;
                        }
                    }
                }
                else if (drawingWire) // Create a wire head ghost and a wire ghost
                {
                    cnvMain.Children.Remove(wireHead[0]); // Remove the previous wire head ghost
                    wireHead[0] = wireHead[1]; // Delete the previous wire head ghost from the array

                    wireHead[1] = new Path(); // Create a ghost of the head of the wire
                    wireHead[1].Data = new RectangleGeometry(new Rect(new Point(mouseCoords[0] - 1, mouseCoords[1] - 1), new Size(2, 2)));
                    wireHead[1].Stroke = Brushes.Gray;
                    wireHead[1].StrokeThickness = 3;
                    cnvMain.Children.Add(wireHead[1]); // Add the ghost to the canvas

                    currentComponent[1] = new Path(); // Create a wire ghost
                    currentComponent[1].Data = entireWire;
                    currentComponent[1].Stroke = Brushes.Gray;
                    cnvMain.Children.Add(currentComponent[1]); // Add the ghost to the array
                }
                else if (gateList.SelectedIndex > -1) // Create gate ghost
                {
                    currentComponent[1] = drawGate(mouseCoords, selection, currentNot); // Draw a new ghost
                    currentComponent[1].Stroke = Brushes.Gray;
                    currentComponent[1].StrokeThickness = 3;
                    cnvMain.Children.Add(currentComponent[1]);
                }
                else if (selection == 0) // Create wire start point ghost
                {
                    currentComponent[1] = new Path();
                    currentComponent[1].Data = new RectangleGeometry(new Rect(new Point(mouseCoords[0] - 1, mouseCoords[1] - 1), new Size(2, 2)));
                    currentComponent[1].Stroke = Brushes.Gray;
                    currentComponent[1].StrokeThickness = 3;
                    cnvMain.Children.Add(currentComponent[1]);
                }
                else if (selection == 1) // Create pin ghost
                {
                    currentComponent[1] = new Path();
                    currentComponent[1].Data = new RectangleGeometry(new Rect(new Point(mouseCoords[0], mouseCoords[1] + 10), new Size(20, 20)));
                    currentComponent[1].Stroke = Brushes.Gray;
                    currentComponent[1].StrokeThickness = 3;
                    cnvMain.Children.Add(currentComponent[1]);
                }


            }
        }

        private void Window_MouseRightbuttonDown(object sender, MouseButtonEventArgs e)
        {
            if (drawingWire) // If the user is drawing a wire
            {
                Path wirePath = new Path(); // Create a new path to hold the finished wire
                wirePath.Data = entireWire; // Set the path data
                wirePath.Stroke = Brushes.Blue;
                cnvMain.Children.Add(wirePath); // Add the whole wire to the canvas

                if (!(currentWire.getTurnPoints().Count == 0 && currentWire.getEnd()[0] == 0 && currentWire.getEnd()[1] == 0))
                {
                    currentWire.finishWire(); // Set the endpoint of the wire
                    mainCircuit.addWire(currentWire); // Add the wire to the circuit
                    wirePaths.Add(wirePath);
                }
                cnvMain.Children.Remove(wireHead[0]); // Remove the previous ghost of the wire head
                cnvMain.Children.Remove(wireHead[1]); // Remove the current ghost of the wire head

                entireWire = new PathGeometry(); // Reset entire wire
            }
            drawingWire = false; // Stop drawing the wire

            compList.SelectedIndex = -1; // Reset the selection of each list
            gateList.SelectedIndex = -1;
            selection = -1; // Reset selection
            parseCircuit(); // Parse any changes to the circuit
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.GetPosition(cnvMain).X < cnvMain.Width && Mouse.GetPosition(cnvMain).Y < cnvMain.Height) // If the mouse is over the canvas when the user clicks
            {
                int[] mouseCoords = new int[] { (((int)Mouse.GetPosition(cnvMain).X / 20) * 20), (((int)Mouse.GetPosition(cnvMain).Y / 20) * 20) }; // Snap the mouse coords to the grid

                if (deleteMode) // If currently in delete mode
                {
                    if (highlightedIndex > -1 && selectedComponentType == 0)// If a gate is clicked on, delete it
                    {
                        deselectComponent(); // Deselect any component
                        cnvMain.Children.Remove(gatePaths[highlightedIndex]); // Remove the gate from the canvas
                        gatePaths.RemoveAt(highlightedIndex); // Remove the gate's path from the list
                        mainCircuit.getGate(highlightedIndex).reduceNextIndex(); // Reduce the next index of the gates
                        mainCircuit.removeComponent(0, highlightedIndex); // Remove the gate from the circuit
                        mainCircuit.reduceGateIndexes(highlightedIndex); // Reduce the gate indexes above the removed gate
                        parseCircuit(); // Parse any changes to the circuit
                        highlightedIndex = -1; // Reset the highlighted index
                    }
                    else if (highlightedIndex > -1 && selectedComponentType == 2)// If a pin is clicked on, delete it
                    {
                        if (mainCircuit.getPin(highlightedIndex).getIfOutput())// If the pin is an ouput pin, remove it from the lists of output pins
                        {
                            listOutputPinsTruth.Items.Remove(mainCircuit.getPin(highlightedIndex).getChar());
                            listOutputPinsBoolean.Items.Remove(mainCircuit.getPin(highlightedIndex).getChar());
                        }
                        deselectComponent(); // Deselect any component
                        cnvMain.Children.Remove(pinPaths[highlightedIndex]); // Remove the pin from the canvas
                        pinPaths.RemoveAt(highlightedIndex); // Remove the pin's path from the list
                        mainCircuit.getPin(highlightedIndex).addSpareChar(); // Add the pin's char to the list of spare chars
                        mainCircuit.getPin(highlightedIndex).reduceNextIndex(); // Reduce the next index of the pins
                        mainCircuit.removeComponent(2, highlightedIndex); // Remove the pin from the circuit
                        mainCircuit.reducePinIndexes(highlightedIndex); // Reduce the pin indexes above the removed pin
                        parseCircuit(); // Parse any changes to the circuit
                        highlightedIndex = -1; // Reset the highlighted index
                    }
                    else if (highlightedIndex > -1 && selectedComponentType == 1)// If a wire is clicked on, delete it
                    {
                        deselectComponent(); // Deselect any component
                        cnvMain.Children.Remove(wirePaths[highlightedIndex]); // Remove the wire from the canvas
                        wirePaths.RemoveAt(highlightedIndex); // Remove the wire's path from the list
                        mainCircuit.getWire(highlightedIndex).reduceNextIndex(); // Reduce the next index of the wires
                        mainCircuit.removeComponent(1, highlightedIndex); // Remove the component from the list
                        parseCircuit(); // Parse any changes to the circuit
                        highlightedIndex = -1; // Reset the highlighted index
                    }
                    else if (highlightedIndex > -1 && selectedComponentType == 3)// If a label is clicked on, delete it
                    {
                        cnvMain.Children.Remove(labels[highlightedIndex]); // the label from the canvas
                        labels.RemoveAt(highlightedIndex); // Remove the label from the list
                        highlightedIndex = -1; // Reset the highlighted index
                    }
                }
                else if (drawingWire) // If a wire is currently being drawn
                {
                    PathFigureCollection thisStep = drawWireLine(savedCoords, mouseCoords); // Get the latest segment of the wire

                    for (int i = 0; i < 2; i++)
                    {
                        entireWire.Figures.Add(thisStep[i]); // Add the segments
                    }

                    savedCoords = mouseCoords; // Save the coords of the turnpoint
                    currentWire.addTurnPoint(mouseCoords); // Add the turnpoint at the mouse location

                }
                else if (gateList.SelectedIndex > -1) // If a gate is selected
                {
                    createNewGate(mouseCoords, selection, currentNot);

                    selection = -1; // Reset the selection
                    currentNot = false;
                }
                else if (selection == 0) // If wire is selected
                {
                    drawingWire = true; // A wire is now being drawn
                    currentWire = new Wire(mouseCoords); // Create a new wire to add to the circuit

                    savedCoords = mouseCoords; // Save the coords
                }
                else if (selection == 1) // If pin is selected
                {
                    Path thisPin = drawPin(mouseCoords, true); // Draw the new pin

                    mainCircuit.addPin(new Pin(mouseCoords));
                    pinPaths.Add(thisPin); // Add the pin to the list of paths
                    cnvMain.Children.Add(thisPin); // Add the pin to the grid
                    Canvas.SetLeft(thisPin, mouseCoords[0]);
                    Canvas.SetTop(thisPin, mouseCoords[1] + 10); // Offset the pin

                    selection = -1; // Reset  the selection
                }
                else if(selection == 2) // If a label is selected
                {
                    TextBox thisLabel = new TextBox(); // Create a new textbox
                    thisLabel.FontWeight = FontWeights.ExtraBold;
                    thisLabel.FontSize = 20;
                    thisLabel.Background = Brushes.Transparent;
                    thisLabel.BorderBrush = Brushes.Transparent;

                    thisLabel.Text = " "; // Set the content of the label
                    thisLabel.SelectAll();

                    labels.Add(thisLabel); // Add the label to the canvas
                    cnvMain.Children.Add(thisLabel);
                    Canvas.SetLeft(thisLabel, mouseCoords[0]);
                    Canvas.SetTop(thisLabel, mouseCoords[1] - 5);

                    selection = -1;
                }
                else if (selection == -1) // If nothing is selected
                {
                    deselectComponent(); // Deselect the previous component
                    selectedIndex = selectComponent(mouseCoords); // Select the new component
                }
                else // To catch any unforseen actions
                {
                    deselectComponent(); // Deselect the previous component
                    selectedIndex = -1; // Reset the selection
                }

                parseCircuit(); // Evaluate any changes to the circuit
                compList.SelectedIndex = -1; // Reset the selection of each list
                gateList.SelectedIndex = -1;
            }
        }

        private void createNewGate(int[] location, int selection, bool isNot)
        {
            Path objectToDraw = drawGate(location, selection, isNot); // Get the gate to draw
            gatePaths.Add(objectToDraw); // The gate's index is determined by the order it was drawn in so it will have the same index in this list
            cnvMain.Children.Add(objectToDraw); // Draw the gate

            mainCircuit.addGate(new Gate(location, selection, isNot)); // Add the new gate to the circuit
        }

        private int selectComponent(int[] mouseCoords)
        {
            for (int i = 0; i < mainCircuit.getNum(0); i++) // For loop runs for the number of gates in the circuit
            {
                if (mainCircuit.getGate(i).getLocation()[0] <= mouseCoords[0] && mainCircuit.getGate(i).getLocation()[0] + 80 >= mouseCoords[0] && mainCircuit.getGate(i).getLocation()[1] + 60 >= mouseCoords[1] && mainCircuit.getGate(i).getLocation()[1] <= mouseCoords[1])// If a gate is clicked on
                {
                    gatePaths[i].Stroke = Brushes.Blue; // Outline the selected gate in blue

                    selectedComponentType = 0;
                    labComponentIndex.Content = mainCircuit.getGate(i).getIndex(); // Display the index of the gate

                    switch (mainCircuit.getGate(i).getGateType()) // Display the type of gate in the correct label
                    {
                        case 0:
                            if (mainCircuit.getGate(i).getNot())
                            {
                                labComponentName.Content = "NAND";
                            }
                            else
                            {
                                labComponentName.Content = "AND";
                            }
                            break;
                        case 1:
                            if (mainCircuit.getGate(i).getNot())
                            {
                                labComponentName.Content = "NOR";
                            }
                            else
                            {
                                labComponentName.Content = "OR";
                            }
                            break;
                        case 2:
                            if (mainCircuit.getGate(i).getNot())
                            {
                                labComponentName.Content = "XNOR";
                            }
                            else
                            {
                                labComponentName.Content = "XOR";
                            }
                            break;
                        case 3:
                            labComponentName.Content = "NOT";
                            break;
                    }

                    if (mainCircuit.getGate(i).getInput(0)) // Set the first state label to the gate's state
                    {
                        labComponentState1.Content = "TRUE";
                    }
                    else
                    {
                        labComponentState1.Content = "FALSE";
                    }

                    if (mainCircuit.getGate(i).getInput(1) && mainCircuit.getGate(i).getGateType() != 3) // Set the second state label to the gate's state. NOT gates only have 1 state
                    {
                        labComponentState2.Content = "TRUE";
                    }
                    else if(mainCircuit.getGate(i).getGateType() != 3)
                    {
                        labComponentState2.Content = "FALSE";
                    }

                    if (mainCircuit.getGate(i).getState()) // Set the output state label to the gate's output state
                    {
                        labComponentOutput.Content = "TRUE";
                    }
                    else
                    {
                        labComponentOutput.Content = "FALSE";
                    }

                    return mainCircuit.getGate(i).getIndex(); // Return the index of the gate
                }
            }
            for(int i = 0; i < mainCircuit.getNum(2); i++)
            {
                if (mouseCoords[0] == mainCircuit.getPin(i).getLocation()[0] && (mouseCoords[1] == mainCircuit.getPin(i).getLocation()[1] || mouseCoords[1] == mainCircuit.getPin(i).getLocation()[1] + 20)) // Check if a pin exists where the mouse has clicked
                {
                    pinPaths[i].Stroke = Brushes.Blue; // Outline the selected pin in blue
                    labComponentName.Content = "Pin " + mainCircuit.getPin(i).getChar(); // Set the name label to the char of the pin
                    labComponentIndex.Content = mainCircuit.getPin(i).getIndex(); // Display the index of the gate
                    selectedComponentType = 2;

                    if (mainCircuit.getPin(i).getState())
                    {
                        labComponentState1.Content = "TRUE";
                    }
                    else
                    {
                        labComponentState1.Content = "FALSE";
                    }

                    if (mainCircuit.getPin(i).getIfOutput())
                    {
                        labComponentOutput.Content = "FUNCTION: OUTPUT";
                    }
                    else
                    {
                        labComponentOutput.Content = "FUNCTION: INPUT";
                    }

                    labComponentState2.Content = "";

                    Button thisButton = new Button(); // Create a new button

                    if (!mainCircuit.getPin(i).getIfOutput()) // If the pin is not an output pin
                    {
                        thisButton.Width = 88; // Add the state button
                        thisButton.Height = 33;
                        grdMain.Children.Add(thisButton);
                        thisButton.Margin = new Thickness(713, 230, 0, 0);
                        thisButton.Click += State_Button_Click;
                        thisButton.Content = "STATE";

                        infoButtons[0] = thisButton; // Add the button to the array
                        thisButton = new Button(); // Reset thisButton
                    }

                    thisButton.Width = 88; // Add the function button
                    thisButton.Height = 33;
                    grdMain.Children.Add(thisButton);
                    thisButton.Margin = new Thickness(895, 230, 0, 0);
                    thisButton.Click += Function_Button_Click;
                    thisButton.Content = "FUNCTION";

                    infoButtons[1] = thisButton; // Add the button to the array

                    return mainCircuit.getPin(i).getIndex(); // Return the pin index
                }
            }

            return -1; // If nothing was selected, return -1
        }

        private void ButDelete_Click(object sender, RoutedEventArgs e)
        {
            if (deleteMode) // If currently in delete mode
            {
                deleteMode = false; // Turn off delete mode
                butDelete.Content = "Delete"; // Reset the button
                butDelete.Background = Brushes.Pink;
                deselectComponent(); // Deselect any selected component
                for(int i = 0; i < labels.Count; i++) // Allow labels to be interacted with
                {
                    labels[i].IsEnabled = true;
                }
            }
            else // If not in delete mode
            {
                deleteMode = true; // Turn on delete mode
                butDelete.Content = "Cancel"; // Set the button to its cancel state
                butDelete.Background = Brushes.Gray;
                deselectComponent(); // Deselect any selected component
                for (int i = 0; i < labels.Count; i++) // Prevent labels from being interacted with
                {
                    labels[i].IsEnabled = false;
                }
            }
        }

        private void State_Button_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < mainCircuit.getNum(2); i++) // For loop runs for the number of gates in the circuit
            {
                if (mainCircuit.getPin(i).getIndex() == selectedIndex)
                {
                    mainCircuit.getPin(i).flipState(); // Flip the pin's state
                    if (mainCircuit.getPin(i).getState()) // Set the state label to the new state of the pin
                    {
                        labComponentState1.Content = "TRUE";
                    }
                    else
                    {
                        labComponentState1.Content = "FALSE";
                    }
                    parseCircuit(); // Parse the circuit for any changes made
                }
            }
        }

        private void Function_Button_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < mainCircuit.getNum(2); i++) // For loop runs for the number of gates in the circuit
            {
                if (mainCircuit.getPin(i).getIndex() == selectedIndex)
                {
                    if (!mainCircuit.getPin(i).getIfOutput())
                    {
                        labComponentOutput.Content = "FUNCTION: OUTPUT";

                        Path newPin = drawPin(mainCircuit.getPin(i).getLocation(), false); // Create a new pin path with the opposite connector
                        cnvMain.Children.Remove(pinPaths[i]); // Remove the previous pin path
                        pinPaths[i] = newPin; // Replace the previous path with the new path
                        cnvMain.Children.Add(newPin); // Add the new path to the grid
                        mainCircuit.getPin(i).flipOutput(); // Flip the output of the pin in the circuit class

                        if (mainCircuit.getPin(i).getState()) // If the pin is on, turn it off
                        {
                            mainCircuit.getPin(i).flipState();
                        }

                        deselectComponent(); // Deselect the 'previous component'
                        selectedIndex = selectComponent(mainCircuit.getPin(i).getLocation()); // Select the 'new component'
                        grdMain.Children.Remove(infoButtons[0]);

                        infoButtons[0] = new Button();

                        Canvas.SetLeft(newPin, mainCircuit.getPin(i).getLocation()[0]);
                        Canvas.SetTop(newPin, mainCircuit.getPin(i).getLocation()[1] + 10);

                        listOutputPinsTruth.Items.Add(mainCircuit.getPin(i).getChar());
                        listOutputPinsBoolean.Items.Add(mainCircuit.getPin(i).getChar());
                    }
                    else
                    {
                        labComponentOutput.Content = "FUNCTION: INPUT";

                        Path newPin = drawPin(mainCircuit.getPin(i).getLocation(), true); // Create a new pin path with the opposite connector
                        cnvMain.Children.Remove(pinPaths[i]); // Remove the previous pin path
                        pinPaths[i] = newPin; // Replace the previous path with the new path
                        cnvMain.Children.Add(newPin); // Add the new path to the grid
                        mainCircuit.getPin(i).flipOutput(); // Flip the output of the pin in the circuit class

                        deselectComponent(); // Deselect the 'previous component'
                        selectedIndex = selectComponent(mainCircuit.getPin(i).getLocation()); // Select the 'new component'

                        Canvas.SetLeft(newPin, mainCircuit.getPin(i).getLocation()[0]);
                        Canvas.SetTop(newPin, mainCircuit.getPin(i).getLocation()[1] + 10);

                        listOutputPinsTruth.Items.Remove(mainCircuit.getPin(i).getChar());
                        listOutputPinsBoolean.Items.Remove(mainCircuit.getPin(i).getChar());
                    }
                    parseCircuit(); // Parse the circuit for any changes made
                }
            }
        }

        private void deselectComponent()
        {
            if(selectedComponentType == 0) // If a gate is selected
            {
                for (int i = 0; i < mainCircuit.getNum(0); i++) // For loop runs for the number of gates in the circuit
                {
                    if (mainCircuit.getGate(i).getIndex() == selectedIndex)
                    {
                        gatePaths[i].Stroke = Brushes.Black; // Set the stroke of the deselected gate back to black

                        labComponentName.Content = ""; // Set the contents of the labels to their default state
                        labComponentState1.Content = "INPUT 1";
                        labComponentState2.Content = "INPUT 2";
                        labComponentIndex.Content = "";
                        labComponentOutput.Content = "OUTPUT";
                    }
                }
            }
            else if(selectedComponentType == 2) // If a pin is selected
            {
                for (int i = 0; i < mainCircuit.getNum(2); i++) // For loop runs for the number of pins in the circuit
                {
                    if (mainCircuit.getPin(i).getIndex() == selectedIndex)
                    {
                        pinPaths[i].Stroke = Brushes.Black; // Set the stroke of the deselected pin back to black

                        labComponentName.Content = ""; // Set the contents of the labels to their default state
                        labComponentState1.Content = "INPUT 1";
                        labComponentState2.Content = "INPUT 2";
                        labComponentIndex.Content = "";
                        labComponentOutput.Content = "OUTPUT";

                        grdMain.Children.Remove(infoButtons[0]); // Remove the buttons used to interact with the pins
                        grdMain.Children.Remove(infoButtons[1]);
                    }
                }
            }
            selectedIndex = -1; // Reset selection indexes
            selectedComponentType = -1;
        }

        #endregion

        #region Component Drawing
        public void drawCanvas()// Draws the border of the canvas and gridlines on it
        {
            for (int i = 0; i < 24; i++)//Horizontal Gridlines
            {
                Rectangle horiLine = new Rectangle();//Define parameters for rectangles making up gridlines
                horiLine.Height = 20;
                horiLine.Width = cnvMain.Width;
                horiLine.Stroke = Brushes.LightGray;//Colour of lines

                cnvMain.Children.Add(horiLine);//Add gridlines to canvas
                Canvas.SetTop(horiLine, i * 20);//Place each rectangle 1 rectangle width apart
                Canvas.SetLeft(horiLine, 0);
            }

            for(int i = 0; i < 40; i++)//Vertical Gridlines
            {
                Rectangle vertiLine = new Rectangle();//Define parameters for rectangles making up gridlines
                vertiLine.Height = cnvMain.Height;
                vertiLine.Width = 20;
                vertiLine.Stroke = Brushes.LightGray;//Colour of lines

                cnvMain.Children.Add(vertiLine);//Add gridlines to canvas
                Canvas.SetLeft(vertiLine, i * 20);//Place each rectangle 1 rectangle width apart
                Canvas.SetTop(vertiLine, 0);
            }

            Rectangle canvasBorder = new Rectangle();//Black border for canvas
            canvasBorder.Height = cnvMain.Height;
            canvasBorder.Width = cnvMain.Width;
            canvasBorder.Stroke = Brushes.Black;
            canvasBorder.StrokeThickness = 2;

            cnvMain.Children.Add(canvasBorder);//Add border to canvas
            Canvas.SetTop(canvasBorder, 0);
            Canvas.SetLeft(canvasBorder, 0);
        }

        public PathFigureCollection drawWireLine(int[] startPoint, int[] endPoint)
        {
            PathFigureCollection wholeWire = new PathFigureCollection();
            PathFigure thisPathFigure = new PathFigure(); // Draw the horizontal portion of the wire segment
            thisPathFigure.StartPoint = new Point(startPoint[0], startPoint[1]); // Starting at the startpoint
            LineSegment thisLine = new LineSegment();
            thisLine.Point = new Point(endPoint[0], startPoint[1]); // Ending in the same column as the endpoint but the same row as the startpoint
            thisPathFigure.Segments.Add(thisLine);
            wholeWire.Add(thisPathFigure);

            thisPathFigure = new PathFigure(); // Draw the vertical portion of the wire segment
            thisPathFigure.StartPoint = new Point(endPoint[0], startPoint[1]); // Starting where the horizontal portion finished
            thisLine = new LineSegment();
            thisLine.Point = new Point(endPoint[0], endPoint[1]); // Finishing at the endpoint
            thisPathFigure.Segments.Add(thisLine);
            wholeWire.Add(thisPathFigure);

            return wholeWire;
        }

        public Path drawGate(int[] coords, int type, bool not) // Provides a Path for the specified gate
        {
            GeometryGroup finalGate = new GeometryGroup(); // This group will contain the path geometry for the gate and an ellipse geometry for the circle to represent a NOT form of a gate
            PathGeometry gateGeometry = new PathGeometry(); // Define store for line segments
            PathFigure thisPathFigure = new PathFigure(); // This will be instantiated multiple times for each line
            EllipseGeometry NOTCircle = new EllipseGeometry(new Point(coords[0] + 90, coords[1] + 40), 10, 10); // Creating an ellipse geometry that will be added as a NOT circle if one is needed
            switch (type)
            {
                case 0: // AND Gate
                    thisPathFigure.StartPoint = new Point(coords[0], coords[1]); // Top left point of figure
                    LineSegment backLineAND = new LineSegment();
                    backLineAND.Point = new Point(coords[0], coords[1] + 80); // Bottom left point of figure
                    thisPathFigure.Segments.Add(backLineAND); // Add line to the current pathfigure
                    gateGeometry.Figures.Add(thisPathFigure); // Add pathfigure to the overall store

                    thisPathFigure = new PathFigure();
                    thisPathFigure.StartPoint = new Point(coords[0], coords[1] + 80); // Bottom left point of figure
                    LineSegment bottomLine = new LineSegment();
                    bottomLine.Point = new Point(coords[0] + 50, coords[1] + 80); // Bottom right corner of figure
                    thisPathFigure.Segments.Add(bottomLine); // Add line to the current pathfigure
                    gateGeometry.Figures.Add(thisPathFigure); // Add pathfigure to the overall store

                    thisPathFigure = new PathFigure();
                    thisPathFigure.StartPoint = new Point(coords[0], coords[1]); // Top left point of figure
                    LineSegment topLine = new LineSegment();
                    topLine.Point = new Point(coords[0] + 50, coords[1]); // Top right corner of figure
                    thisPathFigure.Segments.Add(topLine); // Add line to the current pathfigure
                    gateGeometry.Figures.Add(thisPathFigure); // Add pathfigure to the overall store

                    thisPathFigure = new PathFigure();
                    thisPathFigure.StartPoint = new Point(coords[0] + 50, coords[1] + 80); // Bottom right point of figure
                    ArcSegment rightCurve = new ArcSegment();
                    rightCurve.Point = new Point(coords[0] + 50, coords[1]); // Top right corner of figure
                    rightCurve.Size = new Size(30, 40); // Radius of arc
                    thisPathFigure.Segments.Add(rightCurve); // Add line to the current pathfigure
                    gateGeometry.Figures.Add(thisPathFigure); // Add pathfigure to the overall store

                    break;
                case 1: // OR Gate
                    thisPathFigure.StartPoint = new Point(coords[0], coords[1] + 80); // Bottom left point of figure
                    ArcSegment backCurveOR = new ArcSegment();
                    backCurveOR.Point = new Point(coords[0], coords[1]); // Top left point of figure
                    backCurveOR.Size = new Size(15, 40); // Radius of arc
                    thisPathFigure.Segments.Add(backCurveOR); // Add line to the current pathfigure
                    gateGeometry.Figures.Add(thisPathFigure); // Add pathfigure to the overall store

                    thisPathFigure = new PathFigure();
                    thisPathFigure.StartPoint = new Point(coords[0], coords[1]); // Top left point of figure
                    BezierSegment topCurveOR = new BezierSegment();
                    topCurveOR.Point3 = new Point(coords[0] + 80, coords[1] + 40); // Right edge point of figure
                    topCurveOR.Point1 = new Point(coords[0] + 30, coords[1] + 6); // First Control Point
                    topCurveOR.Point2 = new Point(coords[0] + 50, coords[1]); // Second Control Point
                    thisPathFigure.Segments.Add(topCurveOR); // Add line to the current pathfigure
                    gateGeometry.Figures.Add(thisPathFigure); // Add pathfigure to the overall store

                    thisPathFigure = new PathFigure();
                    thisPathFigure.StartPoint = new Point(coords[0], coords[1] + 80); // Right edge point of figure
                    BezierSegment bottomCurveOR = new BezierSegment();
                    bottomCurveOR.Point3 = new Point(coords[0] + 80, coords[1] + 40); // Bottom left point of figure
                    bottomCurveOR.Point1 = new Point(coords[0] + 30, coords[1] + 74); // First Control Point
                    bottomCurveOR.Point2 = new Point(coords[0] + 50, coords[1] + 80); // Second Control Point
                    thisPathFigure.Segments.Add(bottomCurveOR); // Add line to the current pathfigure
                    gateGeometry.Figures.Add(thisPathFigure); // Add pathfigure to the overall store

                    break;
                case 2: // XOR Gate
                    thisPathFigure.StartPoint = new Point(coords[0], coords[1] + 80); // Bottom left point of figure
                    ArcSegment backCurveXOR = new ArcSegment();
                    backCurveXOR.Point = new Point(coords[0], coords[1]); // Top left point of figure
                    backCurveXOR.Size = new Size(15, 40); // Radius of arc
                    thisPathFigure.Segments.Add(backCurveXOR); // Add line to the current pathfigure
                    gateGeometry.Figures.Add(thisPathFigure); // Add pathfigure to the overall store

                    thisPathFigure = new PathFigure();
                    ArcSegment backDoubleCurveXOR = new ArcSegment();
                    thisPathFigure.StartPoint = new Point(coords[0] - 10, coords[1] + 80); // Top left point of figure
                    backDoubleCurveXOR.Point = new Point(coords[0] - 10, coords[1]); // Behind the top left point of figure
                    backDoubleCurveXOR.Size = new Size(15, 40); // Radius of arc
                    thisPathFigure.Segments.Add(backDoubleCurveXOR); // Add line to the current pathfigure
                    gateGeometry.Figures.Add(thisPathFigure); // Add pathfigure to the overall store

                    thisPathFigure = new PathFigure();
                    thisPathFigure.StartPoint = new Point(coords[0], coords[1]); // Top left point of figure
                    BezierSegment topCurveXOR = new BezierSegment();
                    topCurveXOR.Point3 = new Point(coords[0] + 80, coords[1] + 40); // Right edge point of figure
                    topCurveXOR.Point1 = new Point(coords[0] + 30, coords[1] + 6); // First Control Point
                    topCurveXOR.Point2 = new Point(coords[0] + 50, coords[1]); // Second Control Point
                    thisPathFigure.Segments.Add(topCurveXOR); // Add line to the current pathfigure
                    gateGeometry.Figures.Add(thisPathFigure); // Add pathfigure to the overall store

                    thisPathFigure = new PathFigure();
                    thisPathFigure.StartPoint = new Point(coords[0], coords[1] + 80); // Right edge point of figure
                    BezierSegment bottomCurveXOR = new BezierSegment();
                    bottomCurveXOR.Point3 = new Point(coords[0] + 80, coords[1] + 40); // Bottom left point of figure
                    bottomCurveXOR.Point1 = new Point(coords[0] + 30, coords[1] + 74); // First Control Point
                    bottomCurveXOR.Point2 = new Point(coords[0] + 50, coords[1] + 80); // Second Control Point
                    thisPathFigure.Segments.Add(bottomCurveXOR); // Add line to the current pathfigure
                    gateGeometry.Figures.Add(thisPathFigure); // Add pathfigure to the overall store

                    break;
                case 3: // NOT Gate
                    thisPathFigure.StartPoint = new Point(coords[0], coords[1]); // Top left point of figure
                    LineSegment backLineNOT = new LineSegment();
                    backLineNOT.Point = new Point(coords[0], coords[1] + 80); // Bottom left point of figure
                    thisPathFigure.Segments.Add(backLineNOT); // Add line to the current pathfigure
                    gateGeometry.Figures.Add(thisPathFigure); // Add pathfigure to the overall store

                    thisPathFigure = new PathFigure();
                    thisPathFigure.StartPoint = new Point(coords[0], coords[1]); // Top left point of figure
                    LineSegment topDiagonal = new LineSegment();
                    topDiagonal.Point = new Point(coords[0] + 80, coords[1] + 40); // Rightmost point of figure
                    thisPathFigure.Segments.Add(topDiagonal); // Add line to the current pathfigure
                    gateGeometry.Figures.Add(thisPathFigure); // Add pathfigure to the overall store

                    thisPathFigure = new PathFigure();
                    thisPathFigure.StartPoint = new Point(coords[0], coords[1] + 80); // Bottom left point of figure
                    LineSegment bottomDiagonal = new LineSegment();
                    bottomDiagonal.Point = new Point(coords[0] + 80, coords[1] + 40); // Rightmost point of figure
                    thisPathFigure.Segments.Add(bottomDiagonal); // Add line to the current pathfigure
                    gateGeometry.Figures.Add(thisPathFigure); // Add pathfigure to the overall store
                    break;
                case -1: // No selection

                    break;
            }

            if (not) // If gate is a NOT version, add a circle to represent this
            {
                finalGate.Children.Add(NOTCircle);
            }

            finalGate.Children.Add(gateGeometry);
            finalGate.Children.Add(drawGateConnectors(coords, not, type));

            Path drawGate = new Path(); // Create the path to return
            drawGate.Stroke = Brushes.Black;
            drawGate.StrokeThickness = 2;
            drawGate.Data = finalGate; // Add the drawn gate to the Path

            return drawGate; // Return the path of the drawn gate
        }

        private Path drawPin(int[] mouseCoords, bool isInput)
        {
            GeometryGroup finalPin = new GeometryGroup();

            RectangleGeometry thisPin = new RectangleGeometry(); // Create the pin
            thisPin.Rect = new Rect(0, 0, 20, 20);
            finalPin.Children.Add(thisPin);
            finalPin.Children.Add(drawPinConnector(mouseCoords, isInput));

            Path drawPin = new Path(); // Draw the new pin
            drawPin.Fill = Brushes.LightBlue;
            drawPin.Stroke = Brushes.Black;
            drawPin.StrokeThickness = 4;
            drawPin.Data = finalPin;

            return drawPin; // Return the new pin
        }

        private PathGeometry drawPinConnector(int[] mouseCoords, bool isInput)
        {
            PathGeometry pinConnectorGeometry = new PathGeometry();
            PathFigure thisPathFigure;

            if (isInput)
            {
                thisPathFigure = new PathFigure();
                thisPathFigure.StartPoint = new Point(20, 10); // input connector, coords are relative to position of mouse click
                LineSegment inputConnector = new LineSegment(new Point(24, 10), true);
                thisPathFigure.Segments.Add(inputConnector);
                pinConnectorGeometry.Figures.Add(thisPathFigure);
            }
            else
            {
                thisPathFigure = new PathFigure();
                thisPathFigure.StartPoint = new Point(0, 10); // output connector, coords are relative to position of mouse click
                LineSegment inputConnector = new LineSegment(new Point(-4, 10), true);
                thisPathFigure.Segments.Add(inputConnector);
                pinConnectorGeometry.Figures.Add(thisPathFigure);
            }

            return pinConnectorGeometry; // Return the connector geometry
        }

        private PathGeometry drawGateConnectors(int[] mouseCoords, bool not, int selectedType) // Draw dots for connection points on gates
        {
            PathGeometry gateConnectorGeometry = new PathGeometry();
            PathFigure thisPathFigure = new PathFigure();

            if (selectedType == 3) // If gate is a NOT gate, only one connector is needed for input
            {
                thisPathFigure = new PathFigure();
                thisPathFigure.StartPoint = new Point(mouseCoords[0] - 4, mouseCoords[1] + 40); // NOT connector
                LineSegment bottomConnector = new LineSegment(new Point(mouseCoords[0], mouseCoords[1] + 40), true);
                thisPathFigure.Segments.Add(bottomConnector);
                gateConnectorGeometry.Figures.Add(thisPathFigure);
            }
            else if(selectedType == 1)// OR gates use 2 longer connectors as inputs
            {
                thisPathFigure = new PathFigure();
                thisPathFigure.StartPoint = new Point(mouseCoords[0] - 4, mouseCoords[1] + 20); // Top connector
                LineSegment topConnector = new LineSegment(new Point(mouseCoords[0] + 12, mouseCoords[1] + 20), true);
                thisPathFigure.Segments.Add(topConnector);
                gateConnectorGeometry.Figures.Add(thisPathFigure);

                thisPathFigure = new PathFigure();
                thisPathFigure.StartPoint = new Point(mouseCoords[0] - 4, mouseCoords[1] + 60); // Bottom connector
                LineSegment bottomConnector = new LineSegment(new Point(mouseCoords[0] + 12, mouseCoords[1] + 60), true);
                thisPathFigure.Segments.Add(bottomConnector);
                gateConnectorGeometry.Figures.Add(thisPathFigure);
            }
            else if(selectedType == 2) // XOR gates use 2 slightly longer connectors as inputs
            {
                thisPathFigure = new PathFigure();
                thisPathFigure.StartPoint = new Point(mouseCoords[0] - 4, mouseCoords[1] + 20); // Top connector
                LineSegment topConnector = new LineSegment(new Point(mouseCoords[0] + 4, mouseCoords[1] + 20), true);
                thisPathFigure.Segments.Add(topConnector);
                gateConnectorGeometry.Figures.Add(thisPathFigure);

                thisPathFigure = new PathFigure();
                thisPathFigure.StartPoint = new Point(mouseCoords[0] - 4, mouseCoords[1] + 60); // Bottom connector
                LineSegment bottomConnector = new LineSegment(new Point(mouseCoords[0] + 4, mouseCoords[1] + 60), true);
                thisPathFigure.Segments.Add(bottomConnector);
                gateConnectorGeometry.Figures.Add(thisPathFigure);
            }
            else // All other gates have the same connectors as inputs
            {
                thisPathFigure = new PathFigure();
                thisPathFigure.StartPoint = new Point(mouseCoords[0] - 4, mouseCoords[1] + 20); // Top connector
                LineSegment topConnector = new LineSegment(new Point(mouseCoords[0], mouseCoords[1] + 20), true);
                thisPathFigure.Segments.Add(topConnector);
                gateConnectorGeometry.Figures.Add(thisPathFigure);

                thisPathFigure = new PathFigure();
                thisPathFigure.StartPoint = new Point(mouseCoords[0] - 4, mouseCoords[1] + 60); // Bottom connector
                LineSegment bottomConnector = new LineSegment(new Point(mouseCoords[0], mouseCoords[1] + 60), true);
                thisPathFigure.Segments.Add(bottomConnector);
                gateConnectorGeometry.Figures.Add(thisPathFigure);
            }


            thisPathFigure = new PathFigure();
            LineSegment tipConnector;
            if (not) // If the gate is a NOT version, draw the tip connector at the edge of the NOT circle
            {
                thisPathFigure.StartPoint = new Point(mouseCoords[0] + 104, mouseCoords[1] + 40);
                tipConnector = new LineSegment(new Point(mouseCoords[0] + 100, mouseCoords[1] + 40), true);
            }
            else // If the gate is not a NOT version, draw the tip connector at the tip of the gate
            {
                thisPathFigure.StartPoint = new Point(mouseCoords[0] + 84, mouseCoords[1] + 40);
                tipConnector = new LineSegment(new Point(mouseCoords[0] + 80, mouseCoords[1] + 40), true);
            }
            thisPathFigure.Segments.Add(tipConnector);
            gateConnectorGeometry.Figures.Add(thisPathFigure);

            return gateConnectorGeometry; // Return the connector geometry
        }

        private void parseCircuit()
        {
            for(int i = 0; i < mainCircuit.getNum(1) * 2; i++) // For the number of wires in the circuit, evaluate the circuit
            {
                mainCircuit.evaluateCircuit(); // Evaluating this many times reduces the likelihood of an incorrect state occurring due to a loop
            }

            for (int j = 0; j < mainCircuit.getNum(1); j++) // Loop through all the wires and set their colour to match their state
            {
                if (mainCircuit.getWire(j).getState())
                {
                    wirePaths[j].Stroke = Brushes.Aqua; // If the wire is ON, set the colour to aqua
                }
                else
                {
                    wirePaths[j].Stroke = Brushes.Blue; // If the wire is OFF, set the colour to blue
                }
            }

            for (int i = 0; i < mainCircuit.getNum(2); i++) // Loop through all the pins and set their colour to match their state
            {
                if (mainCircuit.getPin(i).getState())
                {
                    pinPaths[i].Fill = Brushes.DarkCyan; // If the pin is on, set the colour to dark cyan
                }
                else
                {
                    pinPaths[i].Fill = Brushes.LightBlue; // If the pin is off, set the colour to light blue
                }
            }
        }

        #endregion

        #region ListBox Selection
        // These events handle what happens when an item in a listbox is selected, namely setting the selection index and currentNot to the correct values. They also set the other list's selection index to -1 so that nothing is selected.

        private void ListBoxItem_Selected_AND(object sender, RoutedEventArgs e)
        {
            selection = 0;
            currentNot = false;
            compList.SelectedIndex = -1;
        }

        private void ListBoxItem_Selected_OR(object sender, RoutedEventArgs e)
        {
            selection = 1;
            currentNot = false;
            compList.SelectedIndex = -1;
        }

        private void ListBoxItem_Selected_XOR(object sender, RoutedEventArgs e)
        {
            selection = 2;
            currentNot = false;
            compList.SelectedIndex = -1;
        }

        private void ListBoxItem_Selected_NOT(object sender, RoutedEventArgs e)
        {
            selection = 3;
            currentNot = true;
            compList.SelectedIndex = -1;
        }

        private void ListBoxItem_Selected_XNOR(object sender, RoutedEventArgs e)
        {
            selection = 2;
            currentNot = true;
            compList.SelectedIndex = -1;
        }

        private void ListBoxItem_Selected_NAND(object sender, RoutedEventArgs e)
        {
            selection = 0;
            currentNot = true;
            compList.SelectedIndex = -1;
        }

        private void ListBoxItem_Selected_NOR(object sender, RoutedEventArgs e)
        {
            selection = 1;
            currentNot = true;
            compList.SelectedIndex = -1;
        }

        private void ListBoxItem_Selected_WIRE(object sender, RoutedEventArgs e)
        {
            selection = 0;
            gateList.SelectedIndex = -1;
        }

        private void ListBoxItem_Selected_PIN(object sender, RoutedEventArgs e)
        {
            selection = 1;
            gateList.SelectedIndex = -1;
        }

        private void ListBoxItem_Selected_LABEL(object sender, RoutedEventArgs e)
        {
            selection = 2;
            gateList.SelectedIndex = -1;
        }

        #endregion

        #region Truth Tables
        private void drawTable(ref Canvas targetCanvas)
        {
            TextBlock heading;
            List<char> inputs = new List<char>();

            for (int i = 0; i < mainCircuit.getNum(2); i++) // Loop through the pins
            {
                if (!mainCircuit.getPin(i).getIfOutput()) // If the pin is an input pin
                {
                    inputs.Add(mainCircuit.getPin(i).getChar()); // Add the pin's char to the list
                }
            }

            if (mainCircuit.getNum(false) < 4 && mainCircuit.getNum(false) > 0 && mainCircuit.getNum(true) > 0 && listOutputPinsTruth.Text != "") // If a table can be created
            {
                Rectangle truthBorder = new Rectangle(); // Outer border for table
                truthBorder.Width = 120 + (120 * mainCircuit.getNum(false)); // The width is the number of input pins + 1 output pin
                truthBorder.Height = 40 + 40 * (Math.Pow(2, mainCircuit.getNum(false))); // The number of rows is 2^(Number of inputs) + the top row for labels
                truthBorder.Fill = Brushes.White;
                truthBorder.Stroke = Brushes.Black;
                truthBorder.StrokeThickness = 2; // The outer edge should be thicker than the gridlines

                targetCanvas.Children.Add(truthBorder);
                Canvas.SetLeft(truthBorder, 0);
                Canvas.SetTop(truthBorder, 0);

                for (int i = 0; i < 1 + (Math.Pow(2, mainCircuit.getNum(false))); i++)//Horizontal Gridlines
                {
                    Rectangle horiLine = new Rectangle();//Define parameters for rectangles making up gridlines
                    horiLine.Height = 40;
                    horiLine.Width = 120 + (120 * mainCircuit.getNum(false));
                    horiLine.Stroke = Brushes.Black;//Colour of lines
                    if (i == 0)
                    {
                        horiLine.Fill = Brushes.LightGray; // Make the top row grey
                        horiLine.StrokeThickness = 2;
                    }

                    targetCanvas.Children.Add(horiLine);//Add gridlines to canvas
                    Canvas.SetTop(horiLine, i * 40);//Place each rectangle 1 rectangle width apart
                    Canvas.SetLeft(horiLine, 0);
                }

                for (int i = 0; i < mainCircuit.getNum(false) && i < 2; i++)//Vertical Gridlines, Loop runs to less than 2 to prevent drawing too many rectangles as each rectangle count for 2 lines
                {
                    Rectangle vertiLine = new Rectangle();//Define parameters for rectangles making up gridlines
                    vertiLine.Height = 40 + 40 * (Math.Pow(2, mainCircuit.getNum(false)));
                    vertiLine.Width = 120;
                    vertiLine.Stroke = Brushes.Black;//Colour of lines

                    targetCanvas.Children.Add(vertiLine);//Add gridlines to canvas
                    Canvas.SetLeft(vertiLine, i * 240);//Place each rectangle 1 rectangle width apart
                    Canvas.SetTop(vertiLine, 0);
                }

                for (int i = 0; i < mainCircuit.getNum(false) + 1; i++) // Loops through the number of input pins + 1 more for the output column
                {
                    heading = new TextBlock(); // Create the heading textblock
                    heading.FontWeight = FontWeights.ExtraBold;
                    heading.FontSize = 25;
                    targetCanvas.Children.Add(heading); // Add the heading to the canvas

                    Canvas.SetLeft(heading, 50 + i * 120); // Arrange the heading
                    Canvas.SetTop(heading, 2);

                    if (i == mainCircuit.getNum(false)) // If this is the output column
                    {
                        heading.Text = Convert.ToString(listOutputPinsTruth.SelectedItem); // Set the text to the output pin's char
                    }
                    else // If this is an input column
                    {
                        heading.Text = Convert.ToString(inputs[i]); // Set the text to the relevant input char
                    }
                }

                // Counting up in binary for the input columns
                for (int i = 0; i < mainCircuit.getNum(false); i++) // i is for the column
                {
                    for (int j = 1; j < Math.Pow(2, mainCircuit.getNum(false)) + 1; j++) // j is for the row
                    {
                        heading = new TextBlock(); // Reusing the heading variable
                        heading.FontWeight = FontWeights.ExtraBold;
                        heading.FontSize = 25;
                        targetCanvas.Children.Add(heading);
                        Canvas.SetLeft(heading, 50 + i * 120);
                        Canvas.SetTop(heading, 2 + j * 40);

                        heading.Text = Convert.ToString(Convert.ToString(j - 1, 2).PadLeft(mainCircuit.getNum(false), '0')[i]); // Get the relevant 1 or 0 for this part
                    }
                }

                // Calculating the output
                for (int i = 1; i < Math.Pow(2, mainCircuit.getNum(false)) + 1; i++)
                {
                    heading = new TextBlock(); // Reusing the heading variable
                    heading.FontWeight = FontWeights.ExtraBold;
                    heading.FontSize = 25;
                    targetCanvas.Children.Add(heading);
                    Canvas.SetLeft(heading, 50 + mainCircuit.getNum(false) * 120);
                    Canvas.SetTop(heading, 2 + i * 40);

                    if (evaluateForTable(Convert.ToString(i - 1, 2).PadLeft(mainCircuit.getNum(false), '0').ToCharArray())) // Use evaluateForTable to get the digit
                    {
                        heading.Text = "1";
                    }
                    else
                    {
                        heading.Text = "0";
                    }
                }
            }
            else
            {
                if(mainCircuit.getNum(false) > 0)
                {
                    MessageBox.Show("Cannot create truth table with more than 3 inputs", "Error", MessageBoxButton.OK);
                }
                else
                {
                    MessageBox.Show("Cannot create truth table", "Error", MessageBoxButton.OK);
                }
            }
        }

        private void ButTruth_Click(object sender, RoutedEventArgs e)
        {
            cnvTruth.Children.Clear(); // Clear the truth table canvas
            drawTable(ref cnvTruth); // Draw the new truth table
            parseCircuit(); // Parse the ciruit to reset the state of each wire
        }

        public bool evaluateForTable(char[] inputs)
        {
            bool[] boolInputs = new bool[inputs.Length]; // The input states required for this row
            bool output = false;

            for(int i = 0; i < boolInputs.Length; i++) // For each input
            {
                if(inputs[i] == '1') // Set the inputs to the relevant values
                {
                    boolInputs[i] = true;
                }
                else
                {
                    boolInputs[i] = false;
                }
            }

            int offset = 0;
            for(int i = 0; i < mainCircuit.getNum(2); i++) // Set the pin states to the values for this row
            {
                if (!mainCircuit.getPin(i).getIfOutput())
                {
                    mainCircuit.getPin(i).setState(boolInputs[i-offset]);
                }
                else
                {
                    offset++;
                }
            }

            parseCircuit(); // Parse the circuit

            for(int i = 0; i < mainCircuit.getNum(2); i++)
            {
                if(Convert.ToString(mainCircuit.getPin(i).getChar()) == listOutputPinsTruth.Text) // Find the correct output pin
                {
                    output = mainCircuit.getPin(i).getState(); // Get the state of that pin as the output
                }
            }

            for(int i = 0; i < mainCircuit.getNum(false); i++) // Reset the state of each pin to false
            {
                mainCircuit.getPin(i).setState(false);
            }

            parseCircuit(); // Parse the circuit

            return output; // Return the output
        }
        #endregion

        #region Boolean
        private void SliNumInputs_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            labNumInputs.Content = Math.Floor(sliNumInputs.Value) + 1; // Set the label content to the integer value of the slider
            if(numInputs < Math.Floor(sliNumInputs.Value) + 1) // If there are too few input buttons
            {
                addNewInputButton((int)Math.Floor(sliNumInputs.Value) + 1); // Add a new input buttong
            }
            else if(numInputs > Math.Floor(sliNumInputs.Value) + 1) // If there are too many input buttons
            {
                removeInputButton((int)Math.Floor(sliNumInputs.Value) + 2); // Remove an input button
            }

            numInputs = (int)Math.Floor(sliNumInputs.Value) + 1; // Set the new number of inputs

            mainExpression.clear(); // Clear the main expression and the boolean canvas
            cnvBoolInput.Children.Clear();

            for (int i = 0; i < Convert.ToInt32(labNumInputs.Content); i++) // Re-enable all the input buttons
            {
                inputButtons[i].IsEnabled = true;
            }
        }

        private void butExpression_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                labNumGatesCurrent.Content = "Number of Gates: " + mainCircuit.getNum(0); // Set the numbers of components
                labNumWiresCurrent.Content = "Number of Wires: " + mainCircuit.getNum(1);
                labNumPinsCurrent.Content = "Number of Pins: " + mainCircuit.getNum(2);

                if (listOutputPinsBoolean.Text == "") // If no pin is selected
                {
                    MessageBox.Show("Select an ouput pin.", "Error", MessageBoxButton.OK);
                    return;
                }
                for (int i = 0; i < mainCircuit.getNum(1); i++) // For the circuit to be represented as a binary tree, no loops can exist in the circuit wiring
                {
                    for (int j = 0; j < mainCircuit.getNum(1); j++)
                    {
                        if (j != i && (mainCircuit.checkIfOnWire(j, mainCircuit.getWire(i).getLocation()) || mainCircuit.checkIfOnWire(j, mainCircuit.getWire(i).getEnd()))) // If any wire i connects to any wire j
                        {
                            MessageBox.Show("Cannot create binary tree, try removing loops in circuit", "Error", MessageBoxButton.OK); // If any wire to wire connection exists, the tree cannot be created
                            return;
                        }
                    }
                }

                int[] startPoint = new int[2];
                for (int i = 0; i < mainCircuit.getNum(2); i++)
                {
                    if (Convert.ToString(mainCircuit.getPin(i).getChar()) == listOutputPinsBoolean.Text) // Find the pin that corresponds to the selected character
                    {
                        mainCircuit.getPin(i).getLocation().CopyTo(startPoint, 0); // Copy the startpoint
                        startPoint[1] += 20; // Move the startpoint so it matches the y value of a connected wire
                        break;
                    }
                }

                TreeNode circuitTree = generateTreeFromCircuit(startPoint); // Generate a tree representing the circuit

                List<string> infixForm = convertToChars(circuitTree.inOrderTraverse()); // Traverse the circuit to produce an infix expression, then convert it to a readable form
                infixForm.Reverse();

                string infixExpression = "";
                foreach (string token in infixForm) // Turn the list into one string
                {
                    infixExpression += token;
                }
                drawExpression(infixExpression, cnvOutput); // Draw the expression on the output canvas

                infixExpression = "";
                foreach (string token in simplifyExpression(infixForm)) // Turn the simplified expression list into one string
                {
                    infixExpression += token;
                }
                drawExpression(infixExpression, cnvSimplified); // Draw the expression on the simplified canvas
            }
            catch
            {
                MessageBox.Show("Failed to generate expression", "Error", MessageBoxButton.OK);
            }
        }
    
        private List<string> convertToChars(List<string> inputs)
        {
            List<string> output = new List<string>();
            foreach(string thisIndex in inputs) // Swap each index for the symbol associated with it
            {
                if(thisIndex == ")") // If the index is a bracket, add a bracket
                {
                    output.Add(")");
                }
                else if(thisIndex == "¬") // If the index indicates a NOT gate, add a NOT gate
                {
                    output.Add("¬(");
                }
                else
                {
                    for (int i = 0; i < mainCircuit.getNum(0); i++) // Check if the index represents a gate
                    {
                        if (mainCircuit.getGate(i).getIndex() == Convert.ToInt32(thisIndex)) // If the index matches a gate
                        {
                            switch (mainCircuit.getGate(i).getGateType()) // Add a string that represents the gate type
                            {
                                case 0:
                                    output.Add(".");
                                    break;
                                case 1:
                                    output.Add("+");
                                    break;
                                case 2:
                                    output.Add("^");
                                    break;
                                case 3:
                                    output.Add("¬(");
                                    break;
                            }
                        }
                    }
                    for (int i = 0; i < mainCircuit.getNum(2); i++) // Check if the index represents a pin
                    {
                        if (mainCircuit.getPin(i).getIndex() == -Convert.ToInt32(thisIndex)) // Gate indexes are negative
                        {
                            output.Add(Convert.ToString(mainCircuit.getPin(i).getChar())); // Add the char that represents that pin
                        }
                    }
                }
            }
            return output;
        }

        private List<string> simplifyExpression(List<string> infixInput)
        {
            int numNOT;
            string[] copiedInput = new string[infixInput.Count]; // Duplicate the input to an array
            infixInput.CopyTo(copiedInput);

            for(int i = 0; i < infixInput.Count() - 1; i++)
            {
                if(infixInput[i] == "¬(" && infixInput[i+1] == "¬(") // If two NOT gates follow each other
                {
                    numNOT = 2;
                    for(int j = i + 2; j < infixInput.Count() - 1; j++)
                    {
                        if (infixInput[j] == ")" && infixInput[j + 1] == ")" && numNOT == 2) // If there is nothing between the NOT gates
                        {
                            infixInput.RemoveAt(i); // Remove the NOT gates
                            infixInput.RemoveAt(i);
                            infixInput.RemoveAt(j - 2);
                            infixInput.RemoveAt(j - 2);
                            break;
                        }
                        else if(infixInput[j] == ")") // If one of the NOTs finishes before the second one
                        {
                            numNOT--;
                        }
                    }
                }
            }

            if (infixInput.SequenceEqual(copiedInput)) // If nothing was simplified
            {
                return new List<string>(); // Return a blank list
            }
            else // If something changed
            {
                return infixInput; // Return the new list
            }
        }

        private TreeNode generateTreeFromCircuit(int[] startPoint)
        {
            for(int i = 0; i < mainCircuit.getNum(1); i++)
            {
                if (mainCircuit.getWire(i).getEnd().SequenceEqual(startPoint)) // Find the wire that connects to the startpoint
                {
                    for (int j = 0; j < mainCircuit.getNum(0); j++) // Find the gate that connects to the other end of the wire
                    {
                        if (mainCircuit.getGate(j).getGateType() == 3) // If the gate is a NOT gate
                        {
                            if (mainCircuit.getGate(j).getLocation()[0] == mainCircuit.getWire(i).getLocation()[0] - 100 && mainCircuit.getGate(j).getLocation()[1] == mainCircuit.getWire(i).getLocation()[1] - 40)
                            {
                                return new TreeNode(Convert.ToString(mainCircuit.getGate(j).getIndex()), false, generateTreeFromCircuit(new int[2] { mainCircuit.getGate(j).getLocation()[0], mainCircuit.getGate(j).getLocation()[1] + 40 }), null); // Call the subroutine again to generate the left branch from this node
                            }
                        }
                        else if (mainCircuit.getGate(j).getNot()) // If the gate is a NOT version of another gate
                        {
                            if (mainCircuit.getGate(j).getLocation()[0] == mainCircuit.getWire(i).getLocation()[0] - 100 && mainCircuit.getGate(j).getLocation()[1] == mainCircuit.getWire(i).getLocation()[1] - 40)
                            {
                                return new TreeNode(Convert.ToString(mainCircuit.getGate(j).getIndex()), true, generateTreeFromCircuit(new int[2] { mainCircuit.getGate(j).getLocation()[0], mainCircuit.getGate(j).getLocation()[1] + 20 }), generateTreeFromCircuit(new int[2] { mainCircuit.getGate(j).getLocation()[0], mainCircuit.getGate(j).getLocation()[1] + 60 })); // Call the subroutine again to generate the right and left branches from this node
                            }
                        }
                        else if (mainCircuit.getGate(j).getLocation()[0] == mainCircuit.getWire(i).getLocation()[0] - 80 && mainCircuit.getGate(j).getLocation()[1] == mainCircuit.getWire(i).getLocation()[1] - 40)
                        {
                            return new TreeNode(Convert.ToString(mainCircuit.getGate(j).getIndex()), false, generateTreeFromCircuit(new int[2] { mainCircuit.getGate(j).getLocation()[0], mainCircuit.getGate(j).getLocation()[1] + 20 }), generateTreeFromCircuit(new int[2] { mainCircuit.getGate(j).getLocation()[0], mainCircuit.getGate(j).getLocation()[1] + 60 })); // Call the subroutine again to generate the right and left branches from this node
                        }
                    }
                    for (int j = 0; j < mainCircuit.getNum(2); j++) // If a gate doesn't connect to the wire, check for a pin instead
                    {
                        if (mainCircuit.getPin(j).getLocation()[0] == mainCircuit.getWire(i).getLocation()[0] - 20 && mainCircuit.getPin(j).getLocation()[1] == mainCircuit.getWire(i).getLocation()[1] - 20)
                        {
                            return new TreeNode(Convert.ToString(-mainCircuit.getPin(j).getIndex()));
                        }
                    }
                }
                else if(mainCircuit.getWire(i).getLocation().SequenceEqual(startPoint)) // Wires are bidirectional so the startpoint may match the start or the end of the wire
                {
                    for (int j = 0; j < mainCircuit.getNum(0); j++) // Repeat the previous process, checking the wire ends instead of starts
                    {
                        if (mainCircuit.getGate(j).getGateType() == 3) // If the gate is a NOT gate
                        {
                            if (mainCircuit.getGate(j).getLocation()[0] == mainCircuit.getWire(i).getEnd()[0] - 100 && mainCircuit.getGate(j).getLocation()[1] == mainCircuit.getWire(i).getEnd()[1] - 40)
                            {
                                return new TreeNode(Convert.ToString(mainCircuit.getGate(j).getIndex()), false, generateTreeFromCircuit(new int[2] { mainCircuit.getGate(j).getLocation()[0], mainCircuit.getGate(j).getLocation()[1] + 40 }), null); // Call the subroutine again to generate the left branch from this node
                            }
                        }
                        else if (mainCircuit.getGate(j).getNot()) // If the gate is a NOT version of another gate
                        {
                            if (mainCircuit.getGate(j).getLocation()[0] == mainCircuit.getWire(i).getEnd()[0] - 100 && mainCircuit.getGate(j).getLocation()[1] == mainCircuit.getWire(i).getEnd()[1] - 40)
                            {
                                return new TreeNode(Convert.ToString(mainCircuit.getGate(j).getIndex()), true, generateTreeFromCircuit(new int[2] { mainCircuit.getGate(j).getLocation()[0], mainCircuit.getGate(j).getLocation()[1] + 20 }), generateTreeFromCircuit(new int[2] { mainCircuit.getGate(j).getLocation()[0], mainCircuit.getGate(j).getLocation()[1] + 60 })); // Call the subroutine again to generate the right and left branches from this node
                            }
                        }
                        else if (mainCircuit.getGate(j).getLocation()[0] == mainCircuit.getWire(i).getEnd()[0] - 80 && mainCircuit.getGate(j).getLocation()[1] == mainCircuit.getWire(i).getEnd()[1] - 40)
                        {
                            return new TreeNode(Convert.ToString(mainCircuit.getGate(j).getIndex()), false, generateTreeFromCircuit(new int[2] { mainCircuit.getGate(j).getLocation()[0], mainCircuit.getGate(j).getLocation()[1] + 20 }), generateTreeFromCircuit(new int[2] { mainCircuit.getGate(j).getLocation()[0], mainCircuit.getGate(j).getLocation()[1] + 60 })); // Call the subroutine again to generate the right and left branches from this node
                        }
                    }
                    for (int j = 0; j < mainCircuit.getNum(2); j++)
                    {
                        if (mainCircuit.getPin(j).getLocation()[0] == mainCircuit.getWire(i).getEnd()[0] - 20 && mainCircuit.getPin(j).getLocation()[1] == mainCircuit.getWire(i).getEnd()[1] - 20)
                        {
                            return new TreeNode(Convert.ToString(-mainCircuit.getPin(j).getIndex()));
                        }
                    }
                    break;
                }
            }

            return null; // If nothing was found, return null
        }

        private TreeNode generateTreeFromString(List<string> expressionRPN)
        {
            try
            {
                if ("+.^".Contains(expressionRPN[0])) // If this token is a gate, add a gate and generate the branches
                {
                    string token = expressionRPN[0];
                    expressionRPN.RemoveAt(0);
                    return new TreeNode(token, generateTreeFromString(expressionRPN), generateTreeFromString(expressionRPN));
                }
                else if (expressionRPN[0] == "¬") // If this token is a NOT gate, add a NOT gate and generate the left branch
                {
                    string token = expressionRPN[0];
                    expressionRPN.RemoveAt(0);
                    return new TreeNode(token, null, generateTreeFromString(expressionRPN));
                }
                else // If this token is a pin, add it as an end node
                {
                    string token = expressionRPN[0];
                    expressionRPN.RemoveAt(0);
                    return new TreeNode(token);
                }
            }
            catch // If the expression was invalid
            {
                failedConstruction = true;
                return null;
            }
        }

        private void ButCircuit_Click(object sender, RoutedEventArgs e)
        {
            labNumGatesNew.Content = "Number of Gates: " + mainExpression.getNumGates(); // Set the number of gates label

            for(int i = 0; i < Convert.ToInt32(labNumInputs.Content); i++) // Loop through the input buttons
            {
                if(inputButtons[i].IsEnabled == true) // If the button is enabled and has not been pressed
                {
                    MessageBox.Show("Cannot create circuit, unused inputs", "Error", MessageBoxButton.OK);
                    return; // Cancel the circuit creation
                }
            }

            if (mainExpression.checkBracketValidity() && mainExpression.convertRPN().Count > 0 && mainExpression.checkStringValidity()) // If the expression is valid for brackets and not empty
            {
                resetCircuit(); // Reset the main canvas
                List<string> expressionRPN = mainExpression.convertRPN();
                int numNOTs = mainExpression.getNumNOTGates();

                expressionRPN.Reverse(); // Reverse the RPN
                TreeNode rootNode = generateTreeFromString(expressionRPN); // Generate a binary tree from the expression
                if (failedConstruction) // If the tree failed to be constructed
                {
                    MessageBox.Show("Failed to generate tree", "Error", MessageBoxButton.OK);
                    failedConstruction = false;
                    return; // Cancel the circuit creation
                }

                int[] location;
                Path thisPin;
                for(int i = 0; i < Convert.ToInt32(labNumInputs.Content); i++) // For the number of inputs
                {
                    location = new int[2] { 40, i * 80 + 20}; // Set the pin's location based on how many there are
                    thisPin = drawPin(location, true); // Draw the new pin

                    mainCircuit.addPin(new Pin(location));
                    pinPaths.Add(thisPin); // Add the pin to the list of paths
                    cnvMain.Children.Add(thisPin); // Add the pin to the grid
                    Canvas.SetLeft(thisPin, location[0]);
                    Canvas.SetTop(thisPin, location[1] + 10);
                }

                location = new int[2] { rootNode.getDepth() * 120 + numNOTs * 20, (Convert.ToInt32(labNumInputs.Content) - 1) * 40 + 20}; // Set the output pin's location based on the depth of the tree and the number of inputs
                thisPin = drawPin(location, false); // Draw the output pin

                mainCircuit.addPin(new Pin(location));
                mainCircuit.getPin(Convert.ToInt32(labNumInputs.Content)).flipOutput();
                pinPaths.Add(thisPin); // Add the pin to the list of paths
                cnvMain.Children.Add(thisPin); // Add the pin to the grid
                Canvas.SetLeft(thisPin, location[0]);
                Canvas.SetTop(thisPin, location[1] + 10);

                listOutputPinsTruth.Items.Add(mainCircuit.getPin(Convert.ToInt32(labNumInputs.Content)).getChar()); // Add the output pin to the lists of output pins
                listOutputPinsBoolean.Items.Add(mainCircuit.getPin(Convert.ToInt32(labNumInputs.Content)).getChar());

                Wire newWire = new Wire(new int[2] { location[0], location[1] + 20 }); // Start the wire between the output pin and the final gate
                if(mainExpression.getNumGates() == 0) // If there are no gates, connect the wire directly to a pin
                {
                    newWire.addTurnPoint(new int[2] { location[0] - 60, location[1] + 20 });
                }
                else // Otherwise connect it to where the first gate output should be
                {
                    newWire.addTurnPoint(new int[2] { location[0] - 40, location[1] + 20 });
                }
                
                PathFigureCollection thisStep = drawWireLine(newWire.getLocation(), newWire.getTurnPoints()[0]); // Draw a new line for this new segment

                for (int k = 0; k < 2; k++)
                {
                    entireWire.Figures.Add(thisStep[k]); // Add the segments
                }

                Path wirePath = new Path(); // Create the final path for the wire
                wirePath.Data = entireWire; // Add the wire data to the path

                wirePath.Stroke = Brushes.Blue;
                cnvMain.Children.Add(wirePath); // Add the whole wire to the canvas

                newWire.finishWire(); // Set the endpoint of the wire
                mainCircuit.addWire(newWire); // Add the wire to the circuit
                wirePaths.Add(wirePath); // Add the new wire path to the list

                entireWire = new PathGeometry();

                drawWires(rootNode, new int[2] { location[0] - 40, location[1] + 20}); // Draw the rest of the wires for each node in the tree

                location = new int[2] { (rootNode.getDepth() - 1) * 120 + numNOTs * 20, (Convert.ToInt32(labNumInputs.Content) - 1) * 40}; // Set location to top left corner of final gate

                if(rootNode.getID() == "¬") // If the final gate is a NOT gate
                {
                    drawGates(rootNode, new int[2] { location[0] - 20, location[1] }, true); // Draw the gates with wasNOT as true
                }
                else // If the final gate is any other kind of gate
                {
                    drawGates(rootNode, location, false); // Draw the gates with wasNOT as false
                }
                
                labNumWiresNew.Content = "Number of Wires: " + mainCircuit.getNum(1); // Set the number of wires label
                parseCircuit(); // Parse the circuit
            }
            else
            {
                MessageBox.Show("Invalid Expression", "Error", MessageBoxButton.OK);
            }
        }

        private void drawWires(TreeNode thisNode, int[] location)
        {
            Wire newWire = new Wire(new int[2] { location[0] - 80, location[1] - 20 }); // Set the start of the wire at the top connector of the node
            if(thisNode.getID() == "¬" && thisNode.getLeft() != null && !"ABCD".Contains(thisNode.getLeft().getID())) // If this node is a NOT gate and the next node is a gate
            {
                newWire = new Wire(new int[2] { location[0] - 100, location[1]}); // Set the start of the wire to the input of the NOT gate
                int[] turnPoint = new int[2] { location[0] - 140, location[1] }; // Add a turnpoint at the start of the next gate
                PathFigureCollection thisStep = drawWireLine(newWire.getLocation(), turnPoint); // Draw a new line for this new segment

                for (int k = 0; k < 2; k++)
                {
                    entireWire.Figures.Add(thisStep[k]); // Add the segments
                }
                newWire.addTurnPoint(turnPoint);

                Path wirePath = new Path(); // Create the final path for the wire
                wirePath.Data = entireWire; // Add the wire data to the path

                wirePath.Stroke = Brushes.Blue;
                cnvMain.Children.Add(wirePath); // Add the whole wire to the canvas

                newWire.finishWire(); // Set the endpoint of the wire
                mainCircuit.addWire(newWire); // Add the wire to the circuit
                wirePaths.Add(wirePath); // Add the new wire path to the list

                entireWire = new PathGeometry();

                drawWires(thisNode.getLeft(), new int[2] { location[0] - 140, location[1]}); // Repeat for the left branch
            }
            else if (thisNode.getID() == "¬" && thisNode.getLeft() != null && "ABCD".Contains(thisNode.getLeft().getID())) // If this node is a NOT gate and the next node is a pin
            {
                newWire = new Wire(new int[2] { location[0] - 100, location[1] }); // Set the start of the wire to the input of the NOT gate
                int[] turnPoint;
                PathFigureCollection thisStep;
                Path wirePath;

                switch (thisNode.getLeft().getID()) // Check which pin the wire needs to connect to
                {
                    case "A":
                        turnPoint = new int[2] { mainCircuit.getPin(0).getLocation()[0] + (((newWire.getLocation()[0] - mainCircuit.getPin(0).getLocation()[0]) / 2) / 20) * 20, mainCircuit.getPin(0).getLocation()[1] + 20 }; // Turnpoint at the midpoint of the start and end of the wire in the x direction
                        thisStep = drawWireLine(newWire.getLocation(), turnPoint); // Draw a line between the end and the turnpoint of the wire

                        for (int k = 0; k < 2; k++)
                        {
                            entireWire.Figures.Add(thisStep[k]); // Add the segments
                        }
                        newWire.addTurnPoint(turnPoint);

                        turnPoint = new int[2] { 20 + mainCircuit.getPin(0).getLocation()[0], 20 + mainCircuit.getPin(0).getLocation()[1] }; // Turnpoint at the pin
                        thisStep = drawWireLine(newWire.getTurnPoints()[0], turnPoint); // Draw a line between the end and the turnpoint of the wire

                        for (int k = 0; k < 2; k++)
                        {
                            entireWire.Figures.Add(thisStep[k]); // Add the segments
                        }
                        newWire.addTurnPoint(turnPoint);

                        break;
                    case "B":
                        turnPoint = new int[2] { mainCircuit.getPin(1).getLocation()[0] + (((newWire.getLocation()[0] - mainCircuit.getPin(0).getLocation()[0]) / 2) / 20) * 20, mainCircuit.getPin(1).getLocation()[1] + 20 }; // Turnpoint at the midpoint of the start and end of the wire in the x direction
                        thisStep = drawWireLine(newWire.getLocation(), turnPoint); // Draw a line between the end and the turnpoint of the wire

                        for (int k = 0; k < 2; k++)
                        {
                            entireWire.Figures.Add(thisStep[k]); // Add the segments
                        }
                        newWire.addTurnPoint(turnPoint);

                        turnPoint = new int[2] { 20 + mainCircuit.getPin(1).getLocation()[0], 20 + mainCircuit.getPin(1).getLocation()[1] }; // Turnpoint at the pin
                        thisStep = drawWireLine(newWire.getTurnPoints()[0], turnPoint); // Draw a line between the end and the turnpoint of the wire

                        for (int k = 0; k < 2; k++)
                        {
                            entireWire.Figures.Add(thisStep[k]); // Add the segments
                        }
                        newWire.addTurnPoint(turnPoint);

                        break;
                    case "C":
                        turnPoint = new int[2] { mainCircuit.getPin(2).getLocation()[0] + (((newWire.getLocation()[0] - mainCircuit.getPin(0).getLocation()[0]) / 2) / 20) * 20, mainCircuit.getPin(2).getLocation()[1] + 20 }; // Turnpoint at the midpoint of the start and end of the wire in the x direction
                        thisStep = drawWireLine(newWire.getLocation(), turnPoint); // Draw a line between the end and the turnpoint of the wire

                        for (int k = 0; k < 2; k++)
                        {
                            entireWire.Figures.Add(thisStep[k]); // Add the segments
                        }
                        newWire.addTurnPoint(turnPoint);

                        turnPoint = new int[2] { 20 + mainCircuit.getPin(2).getLocation()[0], 20 + mainCircuit.getPin(2).getLocation()[1] }; // Turnpoint at the pin
                        thisStep = drawWireLine(newWire.getTurnPoints()[0], turnPoint); // Draw a line between the end and the turnpoint of the wire

                        for (int k = 0; k < 2; k++)
                        {
                            entireWire.Figures.Add(thisStep[k]); // Add the segments
                        }
                        newWire.addTurnPoint(turnPoint);

                        break;
                    case "D":
                        turnPoint = new int[2] { mainCircuit.getPin(3).getLocation()[0] + (((newWire.getLocation()[0] - mainCircuit.getPin(0).getLocation()[0]) / 2) / 20) * 20, mainCircuit.getPin(3).getLocation()[1] + 20 }; // Turnpoint at the midpoint of the start and end of the wire in the x direction
                        thisStep = drawWireLine(newWire.getLocation(), turnPoint); // Draw a line between the end and the turnpoint of the wire

                        for (int k = 0; k < 2; k++)
                        {
                            entireWire.Figures.Add(thisStep[k]); // Add the segments
                        }
                        newWire.addTurnPoint(turnPoint);

                        turnPoint = new int[2] { 20 + mainCircuit.getPin(3).getLocation()[0], 20 + mainCircuit.getPin(3).getLocation()[1] }; // Turnpoint at the pin
                        thisStep = drawWireLine(newWire.getTurnPoints()[0], turnPoint); // Draw a line between the end and the turnpoint of the wire

                        for (int k = 0; k < 2; k++)
                        {
                            entireWire.Figures.Add(thisStep[k]); // Add the segments
                        }
                        newWire.addTurnPoint(turnPoint);

                        break;
                }

                wirePath = new Path(); // Create the final path for the wire
                wirePath.Data = entireWire; // Add the wire data to the path

                wirePath.Stroke = Brushes.Blue;
                cnvMain.Children.Add(wirePath); // Add the whole wire to the canvas

                newWire.finishWire(); // Set the endpoint of the wire
                mainCircuit.addWire(newWire); // Add the wire to the circuit
                wirePaths.Add(wirePath); // Add the new wire path to the list

                entireWire = new PathGeometry();
            }
            else if(thisNode.getLeft() != null && !"ABCD".Contains(thisNode.getLeft().getID())) // If the left node is a gate
            {
                int[] turnPoint = new int[2] { location[0] - 100, location[1] - 80 * Convert.ToInt32(labNumInputs.Content) / 4 }; // Midpoint of the wire in the x direction
                PathFigureCollection thisStep = drawWireLine(newWire.getLocation(), turnPoint); // Draw a new line for this new segment

                for (int k = 0; k < 2; k++)
                {
                    entireWire.Figures.Add(thisStep[k]); // Add the segments
                }
                newWire.addTurnPoint(turnPoint);

                turnPoint = new int[2] { location[0] - 120, location[1] - 80 * Convert.ToInt32(labNumInputs.Content) / 4 }; // Enpoint of the wire in the x direction
                thisStep = drawWireLine(newWire.getTurnPoints()[0], turnPoint); // Draw a new line for this new segment

                for (int k = 0; k < 2; k++)
                {
                    entireWire.Figures.Add(thisStep[k]); // Add the segments
                }
                newWire.addTurnPoint(turnPoint);


                Path wirePath = new Path(); // Create the final path for the wire
                wirePath.Data = entireWire; // Add the wire data to the path

                wirePath.Stroke = Brushes.Blue;
                cnvMain.Children.Add(wirePath); // Add the whole wire to the canvas

                newWire.finishWire(); // Set the endpoint of the wire
                mainCircuit.addWire(newWire); // Add the wire to the circuit
                wirePaths.Add(wirePath); // Add the new wire path to the list

                entireWire = new PathGeometry();

                drawWires(thisNode.getLeft(), new int[2] { location[0] - 120, location[1] - 80 * Convert.ToInt32(labNumInputs.Content) / 4 }); // Repeat for the left branch
            }
            else if(thisNode.getLeft() != null && "ABCD".Contains(thisNode.getLeft().getID())) // If the left node is a pin
            {
                int[] turnPoint;
                PathFigureCollection thisStep;
                Path wirePath;

                switch (thisNode.getLeft().getID()) // Check which pin the wire needs to connect to
                {
                    case "A":
                        turnPoint = new int[2] { mainCircuit.getPin(0).getLocation()[0] + (((newWire.getLocation()[0] - mainCircuit.getPin(0).getLocation()[0]) / 2) / 20) * 20, mainCircuit.getPin(0).getLocation()[1] + 20}; // Turnpoint at the midpoint of the start and end of the wire in the x direction
                        thisStep = drawWireLine(newWire.getLocation(), turnPoint); // Draw a line between the end and the turnpoint of the wire

                        for (int k = 0; k < 2; k++)
                        {
                            entireWire.Figures.Add(thisStep[k]); // Add the segments
                        }
                        newWire.addTurnPoint(turnPoint);

                        turnPoint = new int[2] { 20 + mainCircuit.getPin(0).getLocation()[0], 20 + mainCircuit.getPin(0).getLocation()[1] }; // Turnpoint at the pin
                        thisStep = drawWireLine(newWire.getTurnPoints()[0], turnPoint); // Draw a line between the end and the turnpoint of the wire

                        for (int k = 0; k < 2; k++)
                        {
                            entireWire.Figures.Add(thisStep[k]); // Add the segments
                        }
                        newWire.addTurnPoint(turnPoint);

                        break;
                    case "B":
                        turnPoint = new int[2] { mainCircuit.getPin(1).getLocation()[0] + (((newWire.getLocation()[0] - mainCircuit.getPin(0).getLocation()[0]) / 2) / 20) * 20, mainCircuit.getPin(1).getLocation()[1] + 20 }; // Turnpoint at the midpoint of the start and end of the wire in the x direction
                        thisStep = drawWireLine(newWire.getLocation(), turnPoint); // Draw a line between the end and the turnpoint of the wire

                        for (int k = 0; k < 2; k++)
                        {
                            entireWire.Figures.Add(thisStep[k]); // Add the segments
                        }
                        newWire.addTurnPoint(turnPoint);

                        turnPoint = new int[2] { 20 + mainCircuit.getPin(1).getLocation()[0], 20 + mainCircuit.getPin(1).getLocation()[1] }; // Turnpoint at the pin
                        thisStep = drawWireLine(newWire.getTurnPoints()[0], turnPoint); // Draw a line between the end and the turnpoint of the wire

                        for (int k = 0; k < 2; k++)
                        {
                            entireWire.Figures.Add(thisStep[k]); // Add the segments
                        }
                        newWire.addTurnPoint(turnPoint);

                        break;
                    case "C":
                        turnPoint = new int[2] { mainCircuit.getPin(2).getLocation()[0] + (((newWire.getLocation()[0] - mainCircuit.getPin(0).getLocation()[0]) / 2) / 20) * 20, mainCircuit.getPin(2).getLocation()[1] + 20 }; // Turnpoint at the midpoint of the start and end of the wire in the x direction
                        thisStep = drawWireLine(newWire.getLocation(), turnPoint); // Draw a line between the end and the turnpoint of the wire

                        for (int k = 0; k < 2; k++)
                        {
                            entireWire.Figures.Add(thisStep[k]); // Add the segments
                        }
                        newWire.addTurnPoint(turnPoint);

                        turnPoint = new int[2] { 20 + mainCircuit.getPin(2).getLocation()[0], 20 + mainCircuit.getPin(2).getLocation()[1] }; // Turnpoint at the pin
                        thisStep = drawWireLine(newWire.getTurnPoints()[0], turnPoint); // Draw a line between the end and the turnpoint of the wire

                        for (int k = 0; k < 2; k++)
                        {
                            entireWire.Figures.Add(thisStep[k]); // Add the segments
                        }
                        newWire.addTurnPoint(turnPoint);

                        break;
                    case "D":
                        turnPoint = new int[2] { mainCircuit.getPin(3).getLocation()[0] + (((newWire.getLocation()[0] - mainCircuit.getPin(0).getLocation()[0]) / 2) / 20) * 20, mainCircuit.getPin(3).getLocation()[1] + 20 }; // Turnpoint at the midpoint of the start and end of the wire in the x direction
                        thisStep = drawWireLine(newWire.getLocation(), turnPoint); // Draw a line between the end and the turnpoint of the wire

                        for (int k = 0; k < 2; k++)
                        {
                            entireWire.Figures.Add(thisStep[k]); // Add the segments
                        }
                        newWire.addTurnPoint(turnPoint);

                        turnPoint = new int[2] { 20 + mainCircuit.getPin(3).getLocation()[0], 20 + mainCircuit.getPin(3).getLocation()[1] }; // Turnpoint at the pin
                        thisStep = drawWireLine(newWire.getTurnPoints()[0], turnPoint); // Draw a line between the end and the turnpoint of the wire

                        for (int k = 0; k < 2; k++)
                        {
                            entireWire.Figures.Add(thisStep[k]); // Add the segments
                        }
                        newWire.addTurnPoint(turnPoint);

                        break;
                }

                wirePath = new Path(); // Create the final path for the wire
                wirePath.Data = entireWire; // Add the wire data to the path

                wirePath.Stroke = Brushes.Blue;
                cnvMain.Children.Add(wirePath); // Add the whole wire to the canvas

                newWire.finishWire(); // Set the endpoint of the wire
                mainCircuit.addWire(newWire); // Add the wire to the circuit
                wirePaths.Add(wirePath); // Add the new wire path to the list

                entireWire = new PathGeometry();
            }

            newWire = new Wire(new int[2] { location[0] - 80, location[1] + 20 });
            if (thisNode.getRight() != null && !"ABCD".Contains(thisNode.getRight().getID())) // If the right node is a gate
            {
                int[] turnPoint = new int[2] { location[0] - 100, location[1] + 80 * Convert.ToInt32(labNumInputs.Content) / 4 }; // Midpoint of the wire in the x direction
                PathFigureCollection thisStep = drawWireLine(newWire.getLocation(), turnPoint); // Draw a new line for this new segment

                for (int k = 0; k < 2; k++)
                {
                    entireWire.Figures.Add(thisStep[k]); // Add the segments
                }
                newWire.addTurnPoint(turnPoint);

                turnPoint = new int[2] { location[0] - 120, location[1] + 80 * Convert.ToInt32(labNumInputs.Content) / 4 }; // Enpoint of the wire in the x direction
                thisStep = drawWireLine(newWire.getTurnPoints()[0], turnPoint); // Draw a new line for this new segment

                for (int k = 0; k < 2; k++)
                {
                    entireWire.Figures.Add(thisStep[k]); // Add the segments
                }
                newWire.addTurnPoint(turnPoint);


                Path wirePath = new Path(); // Create the final path for the wire
                wirePath.Data = entireWire; // Add the wire data to the path

                wirePath.Stroke = Brushes.Blue;
                cnvMain.Children.Add(wirePath); // Add the whole wire to the canvas

                newWire.finishWire(); // Set the endpoint of the wire
                mainCircuit.addWire(newWire); // Add the wire to the circuit
                wirePaths.Add(wirePath); // Add the new wire path to the list

                entireWire = new PathGeometry();

                drawWires(thisNode.getRight(), new int[2] { location[0] - 120, location[1] + 80 * Convert.ToInt32(labNumInputs.Content) / 4 }); // Repeat for the right branch
            }
            else if (thisNode.getRight() != null && "ABCD".Contains(thisNode.getRight().getID())) // If the right node is a pin
            {
                int[] turnPoint;
                PathFigureCollection thisStep;
                Path wirePath;

                switch (thisNode.getRight().getID()) // Check which pin the wire needs to connect to
                {
                    case "A":
                        turnPoint = new int[2] { mainCircuit.getPin(0).getLocation()[0] + (((newWire.getLocation()[0] - mainCircuit.getPin(0).getLocation()[0]) / 2) / 20) * 20, mainCircuit.getPin(0).getLocation()[1] + 20 }; // Turnpoint at the midpoint of the start and end of the wire in the x direction
                        thisStep = drawWireLine(newWire.getLocation(), turnPoint); // Draw a line between the end and the turnpoint of the wire

                        for (int k = 0; k < 2; k++)
                        {
                            entireWire.Figures.Add(thisStep[k]); // Add the segments
                        }
                        newWire.addTurnPoint(turnPoint);

                        turnPoint = new int[2] { 20 + mainCircuit.getPin(0).getLocation()[0], 20 + mainCircuit.getPin(0).getLocation()[1] }; // Turnpoint at the pin
                        thisStep = drawWireLine(newWire.getTurnPoints()[0], turnPoint); // Draw a line between the end and the turnpoint of the wire

                        for (int k = 0; k < 2; k++)
                        {
                            entireWire.Figures.Add(thisStep[k]); // Add the segments
                        }
                        newWire.addTurnPoint(turnPoint);

                        break;
                    case "B":
                        turnPoint = new int[2] { mainCircuit.getPin(1).getLocation()[0] + (((newWire.getLocation()[0] - mainCircuit.getPin(0).getLocation()[0]) / 2) / 20) * 20, mainCircuit.getPin(1).getLocation()[1] + 20 }; // Turnpoint at the midpoint of the start and end of the wire in the x direction
                        thisStep = drawWireLine(newWire.getLocation(), turnPoint); // Draw a line between the end and the turnpoint of the wire

                        for (int k = 0; k < 2; k++)
                        {
                            entireWire.Figures.Add(thisStep[k]); // Add the segments
                        }
                        newWire.addTurnPoint(turnPoint);

                        turnPoint = new int[2] { 20 + mainCircuit.getPin(1).getLocation()[0], 20 + mainCircuit.getPin(1).getLocation()[1] }; // Turnpoint at the pin
                        thisStep = drawWireLine(newWire.getTurnPoints()[0], turnPoint); // Draw a line between the end and the turnpoint of the wire

                        for (int k = 0; k < 2; k++)
                        {
                            entireWire.Figures.Add(thisStep[k]); // Add the segments
                        }
                        newWire.addTurnPoint(turnPoint);

                        break;
                    case "C":
                        turnPoint = new int[2] { mainCircuit.getPin(2).getLocation()[0] + (((newWire.getLocation()[0] - mainCircuit.getPin(0).getLocation()[0]) / 2) / 20) * 20, mainCircuit.getPin(2).getLocation()[1] + 20 }; // Turnpoint at the midpoint of the start and end of the wire in the x direction
                        thisStep = drawWireLine(newWire.getLocation(), turnPoint); // Draw a line between the end and the turnpoint of the wire

                        for (int k = 0; k < 2; k++)
                        {
                            entireWire.Figures.Add(thisStep[k]); // Add the segments
                        }
                        newWire.addTurnPoint(turnPoint);

                        turnPoint = new int[2] { 20 + mainCircuit.getPin(2).getLocation()[0], 20 + mainCircuit.getPin(2).getLocation()[1] }; // Turnpoint at the pin
                        thisStep = drawWireLine(newWire.getTurnPoints()[0], turnPoint); // Draw a line between the end and the turnpoint of the wire

                        for (int k = 0; k < 2; k++)
                        {
                            entireWire.Figures.Add(thisStep[k]); // Add the segments
                        }
                        newWire.addTurnPoint(turnPoint);

                        break;
                    case "D":
                        turnPoint = new int[2] { mainCircuit.getPin(3).getLocation()[0] + (((newWire.getLocation()[0] - mainCircuit.getPin(0).getLocation()[0]) / 2) / 20) * 20, mainCircuit.getPin(3).getLocation()[1] + 20 }; // Turnpoint at the midpoint of the start and end of the wire in the x direction
                        thisStep = drawWireLine(newWire.getLocation(), turnPoint); // Draw a line between the end and the turnpoint of the wire

                        for (int k = 0; k < 2; k++)
                        {
                            entireWire.Figures.Add(thisStep[k]); // Add the segments
                        }
                        newWire.addTurnPoint(turnPoint);

                        turnPoint = new int[2] { 20 + mainCircuit.getPin(3).getLocation()[0], 20 + mainCircuit.getPin(3).getLocation()[1] }; // Turnpoint at the pin
                        thisStep = drawWireLine(newWire.getTurnPoints()[0], turnPoint); // Draw a line between the end and the turnpoint of the wire

                        for (int k = 0; k < 2; k++)
                        {
                            entireWire.Figures.Add(thisStep[k]); // Add the segments
                        }
                        newWire.addTurnPoint(turnPoint);

                        break;
                }

                wirePath = new Path(); // Create the final path for the wire
                wirePath.Data = entireWire; // Add the wire data to the path

                wirePath.Stroke = Brushes.Blue;
                cnvMain.Children.Add(wirePath); // Add the whole wire to the canvas

                newWire.finishWire(); // Set the endpoint of the wire
                mainCircuit.addWire(newWire); // Add the wire to the circuit
                wirePaths.Add(wirePath); // Add the new wire path to the list

                entireWire = new PathGeometry();
            }
        }

        private void drawGates(TreeNode thisNode, int[] location, bool wasNOT)
        {
            int[] thisPlace;
            switch (thisNode.getID()) // Check the type of gate in this node and create a new one at the location
            {
                case ".":
                    thisPlace = new int[2];
                    location.CopyTo(thisPlace, 0);
                    createNewGate(thisPlace, 0, false);
                    break;
                case "+":
                    thisPlace = new int[2];
                    location.CopyTo(thisPlace, 0);
                    createNewGate(thisPlace, 1, false);
                    break;
                case "^":
                    thisPlace = new int[2];
                    location.CopyTo(thisPlace, 0);
                    createNewGate(thisPlace, 2, false);
                    break;
                case "¬":
                    thisPlace = new int[2];
                    location.CopyTo(thisPlace, 0);
                    createNewGate(thisPlace, 3, true);
                    break;
            }

            if (!wasNOT) // If the previous gate was not a NOT gate
            {
                location[1] -= 80 * Convert.ToInt32(labNumInputs.Content) / 4; // Offset the location upwards in the y direction
            }

            if (thisNode.getLeft() != null && thisNode.getLeft().getID() == "¬") // If the left node is a NOT gate
            {
                location[0] -= 140; // Offset the location by 140 in the x direction
                thisPlace = new int[2]; // Create the gate
                location.CopyTo(thisPlace, 0);
                drawGates(thisNode.getLeft(), thisPlace, true); // Repeat the process for the left node
                location[0] += 140; // Undo the location offset so the right branch can be drawn
            }
            else if (thisNode.getLeft() != null && !"ABCD".Contains(thisNode.getLeft().getID())) // If the left node is not a NOT gate
            {
                location[0] -= 120; // Offset the location by 120 in the x direction
                thisPlace = new int[2]; // Create the gate
                location.CopyTo(thisPlace, 0);
                drawGates(thisNode.getLeft(), thisPlace, false); // Repeat the process for the left node
                location[0] += 120;  // Undo the location offset so the right branch can be drawn
            }

            location[1] += 80 * Convert.ToInt32(labNumInputs.Content) / 4; // Offset the direction downwards in the y direction

            if(thisNode.getRight() != null && thisNode.getRight().getID() == "¬") // If the right node is a NOT gate
            {
                location[0] -= 140; // Offset the location by 140 in the x direction
                location[1] += 80 * Convert.ToInt32(labNumInputs.Content) / 4; // Offset the location downwards in the y direction
                thisPlace = new int[2]; // Create the gate
                location.CopyTo(thisPlace, 0);
                drawGates(thisNode.getRight(), thisPlace, true); // Repeat the process for the right node
            }
            else if (thisNode.getRight() != null && !"ABCD".Contains(thisNode.getRight().getID())) // If the right node is not a NOT gate
            {
                location[0] -= 120; // Offset the location by 120 in the x direction
                location[1] += 80 * Convert.ToInt32(labNumInputs.Content) / 4; // Offset the location downwards in the y direction
                thisPlace = new int[2];
                location.CopyTo(thisPlace, 0);
                drawGates(thisNode.getRight(), thisPlace, false); // Repeat the process for the right node
            }
        }

        private void Input_Button_Click(object sender, RoutedEventArgs e)
        {
            for(int i = 0; i < 4; i++)
            {
                if (sender.Equals(inputButtons[i])) // Find the button that was pressed
                {
                    mainExpression.addChar(i); // Add the char of the button
                    inputButtons[i].IsEnabled = false; // Disable the button
                }
            }

            drawExpression(mainExpression.getExpression(), cnvBoolInput); // Redraw the expression
        }

        private void addNewInputButton(int buttonNum)
        {
            Button newButton = new Button(); // Create a new button

            inputButtons[buttonNum-1] = newButton; // Add the button to the array

            newButton.Width = 46;
            newButton.Height = 37;
            grdBoolean.Children.Add(newButton);
            newButton.BorderBrush = Brushes.Black;
            newButton.Margin = new Thickness(-1013 + 102*buttonNum, -150, 0, 0); // Set the location in the row of the button
            newButton.Click += Input_Button_Click;
            newButton.Content = Convert.ToString((char)(64 + buttonNum));
        }

        private void removeInputButton(int buttonNum)
        {
            grdBoolean.Children.Remove(inputButtons[buttonNum - 1]); // Remove the specified button
            inputButtons[buttonNum - 1] = new Button(); // Reset the item in the array
        }

        private void drawExpression(string literal, Canvas thisCanvas)
        {
            thisCanvas.Children.Clear(); // Ckear the canvas
            List<string> outputText = new List<string>();
            List<TextBlock> textInputs = new List<TextBlock>();
            bool drawingNOT = false;

            if (!GCSESelection) // For A-Level Notation
            {
                int otherBrackets = 0; // The number of intermediate brackets
                outputText.Add(""); // Start the list
                for (int i = 0; i < literal.Length; i++)
                {
                    if(i != 0 && literal[i] == '(' && literal[i-1] != '¬')
                    {
                        otherBrackets++; // Increase the number of intermediate brackets
                        outputText[outputText.Count - 1] += literal[i] + "";
                    }
                    else if (drawingNOT && literal[i] == ')' && otherBrackets == 0) // If closing a bracket
                    {
                        outputText[outputText.Count - 1] += literal[i] + ""; // Add the bracket and a space
                        outputText.Add(""); // Add a new item to the list
                        drawingNOT = false; // Finish drawing a NOT hat if one was being drawn
                    }
                    else if(literal[i] == ')') // If the character is a close bracket
                    {
                        otherBrackets--; // Reduce the number of intermediate brackets
                        outputText[outputText.Count - 1] += literal[i] + "";
                        outputText.Add("");
                    }
                    else if (literal[i] == '¬') // If the character is a NOT operator
                    {
                        outputText.Add("¬"); // Add the NOT operator
                        drawingNOT = true; // Begin drawing a NOT hat
                    }
                    else
                    {
                        outputText[outputText.Count - 1] += literal[i] + ""; // If nothing NOT related is happening, add the character to the end of the list
                    }
                }

                int notCount = 1;

                if(literal.Length > 0 && literal[0] == '¬') // If the expression starts with a NOT
                {
                    notCount = 0;
                }
                Pen overLinePen = new Pen(Brushes.Black, 1); // Create a new pen for the overlines

                for (int i = 0; i < outputText.Count; i++)
                {
                    textInputs.Add(new TextBlock()); // Start the list of textblocks
                    textInputs[textInputs.Count - 1].FontSize = 20; // Set the font size

                    if (outputText[i] != "" && outputText[i][0] == '¬') // If this item is under a NOT hat
                    {
                        notCount++; // Increase the count of NOT gates
                    }
                    else if(outputText[i] != "") // If the item is not under a NOT hat
                    {
                        notCount--; // Reduce the number of NOT gates
                    }

                    if(notCount == 4) // Prevent more than 3 levels of NOT
                    {
                        MessageBox.Show("Cannot display more than 3 levels of NOT", "Error", MessageBoxButton.OK);
                        mainExpression.clear();
                        return;
                    }

                    for (int j = 0; j < notCount; j++) // For the number of NOT hats over a section of the expression
                    {
                        textInputs[i].TextDecorations.Add(new TextDecoration(TextDecorationLocation.OverLine, overLinePen, j * -0.1, TextDecorationUnit.FontRecommended, TextDecorationUnit.FontRecommended)); // Add an overline
                    }


                    if (mainExpression.checkBracketValidity()) // If the circuit is valid
                    {
                        if (outputText[i] != "" && outputText[i][0] == '¬')
                        {
                            outputText[i] = outputText[i].Remove(0, 1); // Remove the NOT operator
                        }
                    }

                    textInputs[textInputs.Count - 1].Text = outputText[i]; // Set the text
                }
            }
            else // For GCSE Notation
            {
                outputText.Add("");
                for (int i = 0; i < literal.Length; i++)
                {
                    if (literal[i] == '¬') // These if statements swap the A Level notation used in the literal expression for the GCSE notation
                    {
                        outputText[0] += '¬';
                        outputText[0] += '(';
                        i++;
                    }
                    else if (i != literal.Length - 1 && literal[i + 1] == ')')
                    {
                        outputText[0] += literal[i];
                    }
                    else if (literal[i] == '(')
                    {
                        outputText[0] += literal[i];
                    }
                    else if(literal[i] == '+')
                    {
                        outputText[0] += 'ᐱ';
                        outputText[0] += " ";
                    }
                    else if(literal[i] == '.')
                    {
                        outputText[0] += 'ᐯ';
                        outputText[0] += " ";
                    }
                    else
                    {
                        outputText[0] += literal[i];
                        outputText[0] += " ";
                    }
                }

                textInputs.Add(new TextBlock()); // Add a new textblock
                textInputs[0].Text = outputText[0]; // Set the text to this character
                textInputs[0].FontSize = 20;
            }

            thisCanvas.Children.Add(textInputs[0]); // Add the text to the canvas

            Canvas.SetLeft(textInputs[0], 5);
            Canvas.SetTop(textInputs[0], 4);

            for (int i = 1; i < textInputs.Count; i++)
            {
                thisCanvas.Measure(new Size(Width, Height)); // Measure the canvas to get the ActualWidth
                thisCanvas.Arrange(new Rect(0, 0, thisCanvas.DesiredSize.Width, thisCanvas.DesiredSize.Height));

                thisCanvas.Children.Add(textInputs[i]);

                Canvas.SetLeft(textInputs[i], Canvas.GetLeft(textInputs[i - 1]) + textInputs[i - 1].ActualWidth); // Arrange the text on the canvas
                Canvas.SetTop(textInputs[i], 4);
            }
        }

        private void ButtonALEVEL_Click(object sender, RoutedEventArgs e)
        {
            butGCSE.Background = Brushes.DarkGray; // Set the selection of the buttons
            butALEVEL.Background = Brushes.LightGray;
            GCSESelection = false;
            cnvBoolInput.Children.Clear(); // Clear all the canvases and the main expression
            cnvOutput.Children.Clear();
            cnvSimplified.Children.Clear();
            mainExpression.clear();

            for(int i = 0; i < Convert.ToInt32(labNumInputs.Content); i++) // Re-enable all the input buttons
            {
                inputButtons[i].IsEnabled = true;
            }
        }

        private void ButGCSE_Click(object sender, RoutedEventArgs e)
        {
            butGCSE.Background = Brushes.LightGray; // Set the selection of the buttons
            butALEVEL.Background = Brushes.DarkGray;
            GCSESelection = true;
            cnvBoolInput.Children.Clear(); // Clear all the canvases and the main expression
            cnvOutput.Children.Clear();
            cnvSimplified.Children.Clear();
            mainExpression.clear();

            for (int i = 0; i < Convert.ToInt32(labNumInputs.Content); i++) // Re-enable all the input buttons
            {
                inputButtons[i].IsEnabled = true;
            }
        }

        private void ButtonClear_Click(object sender, RoutedEventArgs e)
        {
            mainExpression.clear(); // Clear the main expression
            drawExpression(mainExpression.getExpression(), cnvBoolInput); // Redraw the canvas with the empty expression

            for (int i = 0; i < Convert.ToInt32(labNumInputs.Content); i++) // Re-enable all the input buttons
            {
                inputButtons[i].IsEnabled = true;
            }
        }

        // These events handle adding the relevant tokens to the expression based on different buttons being pressed

        private void butNOT_Click(object sender, RoutedEventArgs e)
        {
            mainExpression.startNOT();
            drawExpression(mainExpression.getExpression(), cnvBoolInput);
        }

        private void ButAND_Click(object sender, RoutedEventArgs e)
        {
            mainExpression.addAND();
            drawExpression(mainExpression.getExpression(), cnvBoolInput);
        }

        private void ButOR_Click(object sender, RoutedEventArgs e)
        {
            mainExpression.addOR();
            drawExpression(mainExpression.getExpression(), cnvBoolInput);
        }

        private void ButXOR_Click(object sender, RoutedEventArgs e)
        {
            mainExpression.addXOR();
            drawExpression(mainExpression.getExpression(), cnvBoolInput);
        }

        private void ButOBracket_Click(object sender, RoutedEventArgs e)
        {
            mainExpression.openBracket();
            drawExpression(mainExpression.getExpression(), cnvBoolInput);
        }

        private void ButCBracket_Click(object sender, RoutedEventArgs e)
        {
            mainExpression.closeBracket();
            drawExpression(mainExpression.getExpression(),cnvBoolInput);
        }

        #endregion

        #region Canvas Movement
        // These events handle the movement buttons being pressed and moving the canvas around on the canvas

        private void butDown_Click(object sender, RoutedEventArgs e)
        {
            moveComponents(-20, 0);
        }

        private void butUp_Click(object sender, RoutedEventArgs e)
        {
            moveComponents(20, 0);
        }

        private void butRight_Click(object sender, RoutedEventArgs e)
        {
            moveComponents(0, -20);
        }

        private void butLeft_Click(object sender, RoutedEventArgs e)
        {
            moveComponents(0, 20);
        }

        private void moveComponents(int verticalShift, int horizontalShift)
        {
            deselectComponent();
            if(totalOffset[0] - horizontalShift < 0 || totalOffset[1] - verticalShift < 0) // If the offset is 0 in either direction and the user tries to keep going
            {
                MessageBox.Show("Edge of canvas reached.", "Cannot move", MessageBoxButton.OK);
                return; // Cancel the movement
            }

            totalOffset[0] -= horizontalShift; // Update the total offset
            totalOffset[1] -= verticalShift;

            for (int i = 0; i < labels.Count; i++) // For moving labels
            {
                Canvas.SetLeft(labels[i], Canvas.GetLeft(labels[i]) + horizontalShift); // Offset the labels
                Canvas.SetTop(labels[i], Canvas.GetTop(labels[i]) + verticalShift);
            }

            for (int i = 0; i < mainCircuit.getNum(0); i++) // For moving gates
            {
                int[] newLocation = new int[2] { mainCircuit.getGate(0).getLocation()[0] + horizontalShift, mainCircuit.getGate(0).getLocation()[1] + verticalShift}; // Get the new location with the offset
                Path objectToDraw = drawGate(newLocation, mainCircuit.getGate(0).getGateType(), mainCircuit.getGate(0).getNot()); // Get the gate to draw

                cnvMain.Children.Add(objectToDraw); // Draw the gate
                cnvMain.Children.Remove(gatePaths[0]); // Remove the previous path from the canvas
                gatePaths.Add(objectToDraw); // The gate's index is determined by the order it was drawn in so it will have the same index in this list
                gatePaths.Remove(gatePaths[0]); // Remove the previous path from the list

                mainCircuit.addGate(new Gate(newLocation, mainCircuit.getGate(0).getGateType(), mainCircuit.getGate(0).getNot())); // Add the new gate to the circuit
                mainCircuit.removeComponent(0, 0); // Remove the previous gate
            }

            for (int i = 0; i < mainCircuit.getNum(2); i++) // For moving pins
            {
                int[] newLocation = new int[2] { mainCircuit.getPin(0).getLocation()[0] + horizontalShift, mainCircuit.getPin(0).getLocation()[1] + verticalShift}; // Set the new location
                Path thisPin;

                mainCircuit.addPin(new Pin(newLocation, mainCircuit.getPin(0).getChar())); // Add the new pin at the new location to the circuit
                mainCircuit.getPin(mainCircuit.getNum(2) - 1).setState(mainCircuit.getPin(0).getState()); // Set the new pin's state to the previous pin's state

                if (mainCircuit.getPin(0).getIfOutput()) // If the previous pin was an output pin, set the new pin to be an output pin
                {
                    thisPin = drawPin(newLocation, false); // Create the path for the new pin at the new location
                    mainCircuit.getPin(mainCircuit.getNum(2) - 1).flipOutput(); // Set the new pin to be an output pin
                }
                else
                {
                    thisPin = drawPin(newLocation, true); // Create the path for the new pin at the new location
                }

                pinPaths.Add(thisPin); // Add the pin to the list of paths
                cnvMain.Children.Add(thisPin); // Add the pin to the grid
                Canvas.SetLeft(thisPin, newLocation[0]);
                Canvas.SetTop(thisPin, newLocation[1] + 10);

                cnvMain.Children.Remove(pinPaths[0]); // Remove the old pin
                pinPaths.Remove(pinPaths[0]);
                mainCircuit.removeComponent(2, 0);
            }

            for (int i = 0; i < mainCircuit.getNum(1); i++) // For moving wires
            {
                int[] newCoords = mainCircuit.getWire(0).getLocation();
                newCoords[0] += horizontalShift; // Add the offset to the location
                newCoords[1] += verticalShift;
                Wire newWire = new Wire(newCoords); // Create a new wire
                entireWire = new PathGeometry();

                if(mainCircuit.getWire(0).getTurnPoints().Count == 0 && mainCircuit.getWire(0).getEnd()[0] == 0 && mainCircuit.getWire(0).getEnd()[1] == 0) // If a wire is just a point (and probably a mistake by the user) only remove it as it cannot have any effect on the circuit
                {
                    cnvMain.Children.Remove(wirePaths[0]);
                    wirePaths.Remove(wirePaths[0]);
                    mainCircuit.removeComponent(1, 0);
                    continue; // Skip the rest of this iteration
                }
                else if (mainCircuit.getWire(0).getTurnPoints().Count == 0) // If a wire has no turnpoints
                {
                    int[] endCoords = mainCircuit.getWire(0).getEnd();
                    endCoords[0] += horizontalShift; // Shift the end of the wire as required
                    endCoords[1] += verticalShift;
                    PathFigureCollection thisStep = drawWireLine(newCoords, endCoords); // Draw the new line between the new start and new end
                    for (int k = 0; k < 2; k++)
                    {
                        entireWire.Figures.Add(thisStep[k]); // Add the segments
                    }
                    newWire.addTurnPoint(endCoords); // Add the endpoint as a turnpoint so the wire can be finished later
                }
                else // If a wire has turnpoints
                {
                    int[] lastEnd = mainCircuit.getWire(0).getTurnPoints()[0];
                    lastEnd[0] += horizontalShift; // Add the shift to the end of the previous segment
                    lastEnd[1] += verticalShift;
                    PathFigureCollection thisStep = drawWireLine(newCoords, lastEnd); // Draw a new line for this new segment
                    for (int k = 0; k < 2; k++)
                    {
                        entireWire.Figures.Add(thisStep[k]); // Add the segments
                    }
                    newWire.addTurnPoint(lastEnd); // Add the turnpoint to the wire

                    for (int j = 1; j < mainCircuit.getWire(0).getTurnPoints().Count; j++) // Repeat the process for every section of the wire
                    {
                        newCoords = mainCircuit.getWire(0).getTurnPoints()[j];
                        newCoords[0] += horizontalShift; // Add the shift to the location
                        newCoords[1] += verticalShift;
                        thisStep = drawWireLine(lastEnd, newCoords); // Draw the new line for the segment
                        for (int k = 0; k < 2; k++)
                        {
                            entireWire.Figures.Add(thisStep[k]); // Add the segments
                        }
                        newWire.addTurnPoint(newCoords); // Add the turnpoint to the wire
                        lastEnd = newCoords;
                    }

                    newCoords = mainCircuit.getWire(0).getEnd(); // Repeat the process for the final segment of the wire
                    newCoords[0] += horizontalShift; // Add the shift to the location
                    newCoords[1] += verticalShift;
                    thisStep = drawWireLine(lastEnd, newCoords);
                    for (int k = 0; k < 2; k++)
                    {
                        entireWire.Figures.Add(thisStep[k]); // Add the segments
                    }
                    newWire.addTurnPoint(newCoords); // Add the turnpoint to the wire
                }

                Path wirePath = new Path(); // Create the final path for the wire
                wirePath.Data = entireWire; // Add the wire data to the path
                wirePath.Stroke = Brushes.Blue;
                cnvMain.Children.Add(wirePath); // Add the whole wire to the canvas
                cnvMain.Children.Remove(wirePaths[0]); // Remove the old wire path from the canvas

                newWire.finishWire(); // Set the endpoint of the wire
                mainCircuit.addWire(newWire); // Add the wire to the circuit
                wirePaths.Add(wirePath); // Add the new wire path to the list

                wirePaths.Remove(wirePaths[0]); // Remove the old wire path from the list
                mainCircuit.removeComponent(1, 0); // Remove the old wire from the circuit
                entireWire = new PathGeometry();
            }

            parseCircuit(); // Parse the circuit to set the correct colours for every component
        }

        #endregion

        #region File Interaction
        private void ButNew_Click(object sender, RoutedEventArgs e) // reset the main circuit
        {
            if (MessageBox.Show("Unsaved work may be lost, are you sure?", "New File", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                resetCircuit(); // Reset the circuit
                mainExpression = new Expression(); // Reset the mainExpression
                cnvOutput.Children.Clear(); // Clear the boolean canvases
                cnvSimplified.Children.Clear();
                cnvBoolInput.Children.Clear();
            }
        }

        private void resetCircuit()
        {
            listOutputPinsTruth.Items.Clear(); // Clear the output pin lists
            listOutputPinsBoolean.Items.Clear();
            totalOffset = new int[2] { 0, 0 }; // Reset to total offset to 0 in both directions

            deselectComponent(); // Deselect whatever component is selected

            if (mainCircuit.getNum(0) > 0) // Reset the next indexes for the pins and gates
            {
                mainCircuit.getGate(0).resetNextIndex();
            }
            if (mainCircuit.getNum(2) > 0)
            {
                mainCircuit.getPin(0).resetNextIndex();
            }

            for (int i = 0; i < pinPaths.Count; i++) // If the children of the main canvas were cleared, the grid lines would be removed. Therefore, each component has to be manually removed.
            {
                cnvMain.Children.Remove(pinPaths[i]);
            }
            pinPaths.Clear();
            for (int i = 0; i < gatePaths.Count; i++)
            {
                cnvMain.Children.Remove(gatePaths[i]);
            }
            gatePaths.Clear();
            for (int i = 0; i < wirePaths.Count; i++)
            {
                cnvMain.Children.Remove(wirePaths[i]);
            }
            wirePaths.Clear();
            for (int i = 0; i < labels.Count; i++)
            {
                cnvMain.Children.Remove(labels[i]);
            }
            labels.Clear();

            cnvMain.Children.Remove(currentComponent[0]); // Remove any remaining ghost
            mainCircuit = new Circuit(); // Reset the main circuit
            selectedIndex = -1; // Reset the selected index
            wireHead = new Path[2]; // Reset the wireHead
            entireWire = new PathGeometry(); // Reset the entireWire geometry

            compList.SelectedIndex = -1; // Reset the selection of each list
            gateList.SelectedIndex = -1;
            selection = -1; // Reset selection
        }

        private void butSave_Click(object sender, RoutedEventArgs e)
        {
            saveFile(); // Save the file...     duh
        }

        private bool saveFile()
        {
            SaveFileDialog sf = new SaveFileDialog(); // Create a new dialog to get the file location
            sf.Title = "Save Circuit";
            sf.Filter = "Circuit File | *.cf";
            string fileText = mainCircuit.getNum(0) + "," + mainCircuit.getNum(1) + "," + mainCircuit.getNum(2) + "," + labels.Count + "," + totalOffset[0] + "," + totalOffset[1] + "\n"; // Metadata for the division of the components listed in the file and the offset of the circuit view

            for (int i = 0; i < mainCircuit.getNum(0); i++) // Writing gates
            {
                Gate currentGate = mainCircuit.getGate(i); // Get the gate to save
                fileText += currentGate.getLocation()[0] + "," + currentGate.getLocation()[1] + "," + currentGate.getGateType() + ","; // Add the gate's data to the file text
                if (currentGate.getNot()) // Add whether the gate is a NOT version to the file text
                {
                    fileText += "1\n";
                }
                else
                {
                    fileText += "0\n";
                }
            }

            for (int i = 0; i < mainCircuit.getNum(1); i++) // Writing wires
            {
                Wire currentWire = mainCircuit.getWire(i); // Get the wire to save
                fileText += currentWire.getLocation()[0] + ";" + currentWire.getLocation()[1]; // Add the start of the wire to the file text
                for (int j = 0; j < currentWire.getTurnPoints().Count; j++) // For each turnpoint
                {
                    fileText += "," + currentWire.getTurnPoints()[j][0] + ";" + currentWire.getTurnPoints()[j][1]; // Add the turnpoint to the file text
                }
                fileText += "," + currentWire.getEnd()[0] + ";" + currentWire.getEnd()[1] + "\n"; // Add the end of the wire to the file text
            }

            for (int i = 0; i < mainCircuit.getNum(2); i++) // Writing pins
            {
                Pin currentPin = mainCircuit.getPin(i); // Get the pin to save
                fileText += currentPin.getLocation()[0] + "," + currentPin.getLocation()[1] + ","; // Add the pin location to the file text
                if (currentPin.getIfOutput()) // Add whether the pin is an output pin to the file text
                {
                    fileText += "1\n";
                }
                else
                {
                    fileText += "0\n";
                }
            }

            for (int i = 0; i < labels.Count; i++) // Writing labels
            {
                fileText += Canvas.GetLeft(labels[i]) + "," + Canvas.GetTop(labels[i]) + "," + labels[i].Text + "\n"; // Add the label's data to the file text
            }

            if (sf.ShowDialog() == true) // If the user wishes to save
            {
                File.WriteAllText(sf.FileName, fileText); // Add all the text to a file
                return true;
            }
            else // If the user does not wish to save
            {
                return false;
            }
        }

        private void butLoad_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog lf = new OpenFileDialog(); // Create a new dialog to get the file location
            lf.Title = "Load Circuit";
            lf.Filter = "Circuit File | *.cf";
            string[] fileText;
            int[] metaData = new int[6];

            if (lf.ShowDialog() == true) // If the user wishes to load
            {
                try
                {
                    fileText = File.ReadAllLines(lf.FileName); // Get the file text
                    for (int i = 0; i < 6; i++) // Get the metadata from the first line
                    {
                        metaData[i] = Convert.ToInt32(fileText[0].Split(',')[i]); // Split the first line by commas
                    }

                    resetCircuit(); // Reset the circuit

                    totalOffset[0] = metaData[4]; // Get the total offset from the metadata
                    totalOffset[1] = metaData[5];

                    // Loading Gates

                    int[,] gateInfo = new int[metaData[0], 4];

                    for (int i = 0; i < metaData[0]; i++) // For the number of gates specified in the metadata
                    {
                        for (int j = 0; j < 4; j++)
                        {
                            gateInfo[i, j] = Convert.ToInt32(fileText[i + 1].Split(',')[j]); // Get the information about ther gate
                        }
                        int[] location = new int[2];
                        location[0] = gateInfo[i, 0]; // Get the location from the gate info
                        location[1] = gateInfo[i, 1];
                        bool isNot;

                        if (gateInfo[i, 3] == 1) // Get whether the gate is a NOT version from the gate infor
                        {
                            isNot = true;
                        }
                        else
                        {
                            isNot = false;
                        }
                        createNewGate(location, gateInfo[i, 2], isNot); // Create a new gate with the gate info
                    }

                    // Loading Wires

                    List<int[]> wireInfo = new List<int[]>();

                    for (int i = 0; i < metaData[1]; i++) // For the number of wires specified in the metadata
                    {
                        for (int j = 0; j < fileText[i + metaData[0] + 1].Split(',').Length; j++) // Getting the wire info
                        {
                            wireInfo.Add(new int[2]);

                            wireInfo.Last()[0] = Convert.ToInt32(fileText[i + metaData[0] + 1].Split(',')[j].Split(';')[0]); // Add the x coord
                            wireInfo.Last()[1] = Convert.ToInt32(fileText[i + metaData[0] + 1].Split(',')[j].Split(';')[1]); // Add the y coord
                        }

                        Wire currentWire = new Wire(wireInfo[0]); // Create a new wire
                        entireWire = new PathGeometry();

                        if (wireInfo.Count == 2) // If the wire has no turnpoints
                        {
                            PathFigureCollection thisStep = drawWireLine(wireInfo[0], wireInfo.Last()); // Draw the wire line
                            for (int j = 0; j < 2; j++)
                            {
                                entireWire.Figures.Add(thisStep[j]); // Add the segments
                            }
                            currentWire.addTurnPoint(wireInfo[1]); // Add the end as a turnpoint
                        }
                        else // If the wire has turnpoints
                        {
                            for (int j = 0; j < wireInfo.Count - 1; j++) // For the number of turnpoints
                            {
                                PathFigureCollection thisStep = drawWireLine(wireInfo[j], wireInfo[j + 1]); // Draw a new segment
                                for (int k = 0; k < 2; k++)
                                {
                                    entireWire.Figures.Add(thisStep[k]); // Add the segments
                                }
                                currentWire.addTurnPoint(wireInfo[j + 1]); // Add a new turnpoint
                            }
                        }

                        wireInfo.Clear(); // Clear the wire info

                        Path wirePath = new Path(); // Finish the wire
                        wirePath.Data = entireWire;
                        wirePath.Stroke = Brushes.Blue;
                        cnvMain.Children.Add(wirePath); // Add the whole wire to the canvas

                        currentWire.finishWire(); // Set the endpoint of the wire
                        mainCircuit.addWire(currentWire); // Add the wire to the circuit
                        wirePaths.Add(wirePath); // Add the wire path to the list

                        entireWire = new PathGeometry();
                    }

                    // Loading Pins

                    int[,] pinInfo = new int[metaData[2], 3];

                    for (int i = 0; i < metaData[2]; i++) // For the number of pins specified in the metadata
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            pinInfo[i, j] = Convert.ToInt32(fileText[i + metaData[0] + metaData[1] + 1].Split(',')[j]); // Get the pin info
                        }
                        int[] location = new int[2];
                        location[0] = pinInfo[i, 0]; // Get the location
                        location[1] = pinInfo[i, 1];
                        bool isOutput;

                        Pin currentPin = new Pin(location); // Create a new pin

                        if (pinInfo[i, 2] == 1) // If the pin is an output pin
                        {
                            isOutput = true;
                            currentPin.flipOutput(); // Flip the pin output
                            listOutputPinsTruth.Items.Add(currentPin.getChar()); // Add the pin to the lists of output pins
                            listOutputPinsBoolean.Items.Add(currentPin.getChar());
                        }
                        else
                        {
                            isOutput = false;
                        }

                        Path thisPin = drawPin(location, !isOutput); // Draw the new pin

                        mainCircuit.addPin(currentPin); // Add the pin to the circuit
                        pinPaths.Add(thisPin); // Add the pin to the list of paths
                        cnvMain.Children.Add(thisPin); // Add the pin to the grid
                        Canvas.SetLeft(thisPin, location[0]);
                        Canvas.SetTop(thisPin, location[1] + 10);
                    }

                    // Loading Labels

                    for (int i = 0; i < metaData[3]; i++) // For the number of labels specified in the metadata
                    {
                        string[] labelInfo = fileText[i + metaData[0] + metaData[1] + metaData[2] + 1].Split(','); // Get the label info
                        TextBox thisLabel = new TextBox(); // Create a new label
                        thisLabel.FontWeight = FontWeights.ExtraBold;
                        thisLabel.FontSize = 20;
                        thisLabel.Background = Brushes.Transparent;
                        thisLabel.BorderBrush = Brushes.Transparent;

                        thisLabel.Text = labelInfo[2]; // Add the label text

                        labels.Add(thisLabel); // Add the label to the list
                        cnvMain.Children.Add(thisLabel); // Add the label to the canvas
                        Canvas.SetLeft(thisLabel, Convert.ToInt32(labelInfo[0])); // Set the label location
                        Canvas.SetTop(thisLabel, Convert.ToInt32(labelInfo[1]));

                    }

                    parseCircuit(); // Parse the new circuit
                }
                catch // If the file failed to load
                {
                    MessageBox.Show("Failed to load file", "Error", MessageBoxButton.OK);
                }
            }
            else
            {
                return;
            }
        }

        private void butExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                moveComponents(totalOffset[1], totalOffset[0]); // Reset the positions of components relative to the origin
                totalOffset = new int[2] { 0, 0 }; // Reset the total offset as components are now in original positions

                PngBitmapEncoder encoder = new PngBitmapEncoder(); // Create an encoder to generate the png file
                SaveFileDialog sf = new SaveFileDialog(); // Create a dialog to get where to save the image
                sf.Title = "Export Circuit";
                sf.Filter = "Portable Network Graphic | *.png";

                if (sf.ShowDialog() == true)
                {
                    Canvas imageToSave = new Canvas(); // Create a new canvas to add paths to

                    int[] canvasWidths = new int[gatePaths.Count + pinPaths.Count + wirePaths.Count + labels.Count]; // These arrays will store x and y coordinates of every component on the grid
                    int[] canvasHeights = new int[gatePaths.Count + pinPaths.Count + wirePaths.Count + labels.Count];

                    for (int i = 0; i < gatePaths.Count; i++)
                    {
                        cnvMain.Children.Remove(gatePaths[i]); // Transfer each gate path from the main canvas to the new one so it can be exported
                        imageToSave.Children.Add(gatePaths[i]);

                        canvasWidths[i] = mainCircuit.getGate(i).getLocation()[0]; // Add the x and y coordinates of the gate to the respective arrays
                        canvasHeights[i] = mainCircuit.getGate(i).getLocation()[1];
                    }
                    for (int i = 0; i < pinPaths.Count; i++)
                    {
                        cnvMain.Children.Remove(pinPaths[i]); // Transfer each pin path from the main canvas to the new one so it can be exported
                        imageToSave.Children.Add(pinPaths[i]);

                        canvasWidths[i + gatePaths.Count] = mainCircuit.getPin(i).getLocation()[0]; // Add the x and y coordinates of the pin to the respective arrays
                        canvasHeights[i + gatePaths.Count] = mainCircuit.getPin(i).getLocation()[1];
                    }
                    for (int i = 0; i < wirePaths.Count; i++)
                    {
                        cnvMain.Children.Remove(wirePaths[i]); // Transfer each wire path from the main canvas to the new one so it can be exported
                        imageToSave.Children.Add(wirePaths[i]);

                        canvasWidths[i + gatePaths.Count + pinPaths.Count] = Math.Max(mainCircuit.getWire(i).getLocation()[0], mainCircuit.getWire(i).getEnd()[0]); // Add the x and y coordinates of the furthest vertex from the origin to the respective arrays
                        canvasHeights[i + gatePaths.Count + pinPaths.Count] = Math.Max(mainCircuit.getWire(i).getLocation()[1], mainCircuit.getWire(i).getEnd()[1]);

                        for (int j = 0; j < mainCircuit.getWire(i).getTurnPoints().Count; j++) // Check if any turnpoints on the wire are further from the origin and if so, add them to the array instead
                        {
                            if (mainCircuit.getWire(i).getTurnPoints()[j][0] > canvasWidths[i])
                            {
                                canvasWidths[i + gatePaths.Count + pinPaths.Count] = mainCircuit.getWire(i).getTurnPoints()[j][0];
                            }
                            if (mainCircuit.getWire(i).getTurnPoints()[j][1] > canvasHeights[i])
                            {
                                canvasHeights[i + gatePaths.Count + pinPaths.Count] = mainCircuit.getWire(i).getTurnPoints()[j][1];
                            }
                        }
                    }
                    for (int i = 0; i < labels.Count; i++)
                    {
                        int[] labelPosition = new int[2] { (int)Canvas.GetLeft(labels[i]), (int)Canvas.GetTop(labels[i]) }; // Get the position of each label on the canvas

                        cnvMain.Children.Remove(labels[i]); // Transfer each label from the main canvas to the new one so it can be exported
                        imageToSave.Children.Add(labels[i]);

                        Canvas.SetLeft(labels[i], labelPosition[0]); // Set the position of the label on the new canvas
                        Canvas.SetTop(labels[i], labelPosition[1]);

                        canvasWidths[i + gatePaths.Count + pinPaths.Count + wirePaths.Count] = labelPosition[0]; // Add the x and y coordinates of the label to the respective arrays
                        canvasHeights[i + gatePaths.Count + pinPaths.Count + wirePaths.Count] = labelPosition[1];
                    }

                    Array.Sort(canvasWidths); // Sort and reverse the coordinates in each array
                    Array.Sort(canvasHeights);
                    canvasWidths.Reverse();
                    canvasHeights.Reverse();

                    imageToSave.Width = canvasWidths.Last() + 120; // The exported image will be as wide and tall as the furthest point from the origin of any component
                    imageToSave.Height = canvasHeights.Last() + 120;

                    RenderTargetBitmap bitmap = new RenderTargetBitmap((int)imageToSave.Width, (int)imageToSave.Height, 96, 96, PixelFormats.Pbgra32); // Creat a bitmap to render the image onto
                    bitmap.Render(imageToSave);
                    BitmapFrame frame = BitmapFrame.Create(bitmap);
                    encoder.Frames.Add(frame);

                    using (FileStream stream = File.Create(sf.FileName))
                    {
                        encoder.Save(stream); // Use the encoder to save the file
                    }


                    for (int i = 0; i < gatePaths.Count; i++) // Add the component paths back to the main canvas
                    {
                        imageToSave.Children.Remove(gatePaths[i]);
                        cnvMain.Children.Add(gatePaths[i]);
                    }
                    for (int i = 0; i < pinPaths.Count; i++)
                    {
                        imageToSave.Children.Remove(pinPaths[i]);
                        cnvMain.Children.Add(pinPaths[i]);
                    }
                    for (int i = 0; i < wirePaths.Count; i++)
                    {
                        imageToSave.Children.Remove(wirePaths[i]);
                        cnvMain.Children.Add(wirePaths[i]);
                    }
                    for (int i = 0; i < labels.Count; i++) // Labels require a bit more work as they are more integrated into the canvas than the other components
                    {
                        int[] labelPosition = new int[2] { (int)Canvas.GetLeft(labels[i]), (int)Canvas.GetTop(labels[i]) };

                        imageToSave.Children.Remove(labels[i]);
                        cnvMain.Children.Add(labels[i]);

                        Canvas.SetLeft(labels[i], labelPosition[0]);
                        Canvas.SetTop(labels[i], labelPosition[1]);
                    }
                }
            }
            catch
            {
                MessageBox.Show("Failed to export", "Error", MessageBoxButton.OK);
            }
            
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MessageBoxResult instruction = MessageBox.Show("Save circuit?", "Close Window", MessageBoxButton.YesNoCancel); // Ask the user if they want to save

            if (instruction == MessageBoxResult.Yes) // If they want to save
            {
                if (!saveFile()) // If they do not save
                {
                    e.Cancel = true; // Cancel the window closing
                }
            }
            else if (instruction == MessageBoxResult.Cancel) // If they want to cancel the window closing
            {
                e.Cancel = true; // Cancel the window closing
            }
        }

        private void butTableExport_Click(object sender, RoutedEventArgs e)
        {
            PngBitmapEncoder encoder = new PngBitmapEncoder(); // Create an encoder to generate the png file
            SaveFileDialog sf = new SaveFileDialog(); // Create a dialog to get where to save the image
            sf.Title = "Export Table";
            sf.Filter = "Portable Network Graphic | *.png";

            if(sf.ShowDialog() == true)
            {
                Canvas imageToSave = new Canvas(); // Create a canvas to draw to
                
                drawTable(ref imageToSave); // Draw the table onto the canvas

                imageToSave.Measure(new Size(Width, Height)); // Initialise the canvas to get an accurate width and height
                imageToSave.Arrange(new Rect(0, 0, imageToSave.DesiredSize.Width, imageToSave.DesiredSize.Height));

                imageToSave.Width = 120 + (120 * mainCircuit.getNum(false)); // Set the required height and width
                imageToSave.Height = 40 + 40 * (Math.Pow(2, mainCircuit.getNum(false)));

                RenderTargetBitmap bitmap = new RenderTargetBitmap((int)imageToSave.Width, (int)imageToSave.Height, 96, 96, PixelFormats.Pbgra32); // Creat a bitmap to render the image onto
                bitmap.Render(imageToSave); // Render the image on the bitmap
                BitmapFrame frame = BitmapFrame.Create(bitmap);
                encoder.Frames.Add(frame);

                using (FileStream stream = File.Create(sf.FileName))
                {
                    encoder.Save(stream); // Use the encoder to save the file
                }
            }
        }

        #endregion
    }
}
