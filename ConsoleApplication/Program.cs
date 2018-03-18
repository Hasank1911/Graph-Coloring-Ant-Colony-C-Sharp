using System;
using System.Collections.Generic;
using System.IO;

namespace ConsoleApplication {
    class Program {
        public static String filename = "SampleData.txt"; 

        // Parameters;
        public static double alpha = 2; // for heuristic
        public static double beta  = 5; // for pheremone
        public static int NUMBER_OF_ANTS = 100;
        public static int NUMBER_OF_ITERATIONS = 100;
        public static double DecayConstant = 0.8;
        public static int randomSeed = 1;
        public static bool binarySelection = false;

        // It will remain same for each instance;
        public static int numberOfNodes;
        public static int numberOfEdges;
        public static bool[,] edges;
        public static int[] degrees;
        public static List<int> listOfAllNodes;

        public static double[,] pheremone; // Pij = ith node with jth color  // or ith and jth node same color

        public static void Main(String[] args) {
            if (args.Length > 0)
                ReadData(args[0]);
            else
                ReadData("");

            adjustParameters();
            findDegrees(); // initialize degrees[]
            prepareListOfEveryNode(); // initialize List object listOfAllNodes
            initializePheremone();
            mainAntAlgorithm();

            Console.ReadLine();
        }
        private static void adjustParameters() {
            if (numberOfNodes <= 50) {
                NUMBER_OF_ANTS = 1000;
                NUMBER_OF_ITERATIONS = 10;
            } else if (numberOfNodes <= 100) {
                NUMBER_OF_ANTS = 12000;
                NUMBER_OF_ITERATIONS = 12;
            } else {
                NUMBER_OF_ANTS = 100; // 1000
                NUMBER_OF_ITERATIONS = 10; //12
            }
        }
        private static void mainAntAlgorithm() {
            int minColor = numberOfNodes;
            int[] bestColorAssignments = new int[numberOfNodes];
            Random random = new Random(randomSeed);
            for (int iterationIndex = 0; iterationIndex < NUMBER_OF_ITERATIONS; iterationIndex++) {
                int[,] assignedColorsForEachNode = new int[NUMBER_OF_ANTS, numberOfNodes]; // Ant index, selections of this ant
                int[] totalNumberOfColorsUsedForEachAnt = new int[NUMBER_OF_ANTS]; // Will determine the ant quality 
                for (int antIndex = 0; antIndex < NUMBER_OF_ANTS; antIndex++) {

                    int currentColor = 0;
                    List<int> nodesCanBeColoredWithCurrentColor = new List<int>(listOfAllNodes); // currently all nodes
                    List<int> colorAssignedNodes = new List<int>(); // currently empty
                    List<int> nodesCurrentlyColoredWithCurrentColor = new List<int>(); // currently empty
                    int[] currentDegrees = (int[])degrees.Clone();

                    while (colorAssignedNodes.Count != numberOfNodes) {
                        while (nodesCanBeColoredWithCurrentColor.Count != 0) {
                            int selectedNode = selectNode(nodesCanBeColoredWithCurrentColor, currentDegrees, currentColor, nodesCurrentlyColoredWithCurrentColor, random); // it will return node id. it will use pheromone and degrees(heuristic)
                            assignedColorsForEachNode[antIndex, selectedNode] = currentColor;
                            nodesCurrentlyColoredWithCurrentColor.Add(selectedNode);
                            colorAssignedNodes.Add(selectedNode);
                            nodesCanBeColoredWithCurrentColor.Remove(selectedNode); // remove ITEM selectedNode(not index) from the list
                            for (int i = 0; i < numberOfNodes; i++) { // removing any node that is directly connected to 
                                if (edges[i, selectedNode]) {
                                    nodesCanBeColoredWithCurrentColor.Remove(i);// remove item i directly (i is not index)
                                }
                            }
                            currentDegrees = findNewDegrees(nodesCanBeColoredWithCurrentColor); // find the degrees in SUBGRAPH!
                        }
                        currentColor++;
                        nodesCurrentlyColoredWithCurrentColor = new List<int>(); // currently empty
                        nodesCanBeColoredWithCurrentColor = new List<int>(listOfAllNodes);
                        foreach (int node in colorAssignedNodes) {  // remove colored nodes
                            nodesCanBeColoredWithCurrentColor.Remove(node);
                        }
                        currentDegrees = findNewDegrees(nodesCanBeColoredWithCurrentColor); // find the degrees in SUBGRAPH!
                    }
                    totalNumberOfColorsUsedForEachAnt[antIndex] = currentColor;

                    if (minColor > currentColor) {
                        minColor = currentColor;
                        for (int i = 0; i < numberOfNodes; i++)
                            bestColorAssignments[i] = assignedColorsForEachNode[antIndex, i];
                    }     
                }
                pheremoneUpdate(assignedColorsForEachNode, totalNumberOfColorsUsedForEachAnt);
            }
            Console.WriteLine(minColor); //Console.WriteLine("minColor: " + minColor+ " and its assignments; ");
            for (int i = 0; i < numberOfNodes; i++)
                Console.Write(bestColorAssignments[i] + " ");
            //Console.WriteLine();

        }

        private static void pheremoneUpdate(int[,] assignedColorsForEachNode, int[] totalNumberOfColorsUsedForEachAnt) {
            for (int i = 0; i < numberOfNodes; i++) {
                for (int j = 0; j < numberOfNodes; j++) {
                    pheremone[i, j] = pheremone[i, j] * DecayConstant;
                }
            }
            for (int antIndex = 0; antIndex < NUMBER_OF_ANTS; antIndex++) {
                for (int nodeID = 0; nodeID < numberOfNodes; nodeID++) {
                    for (int secondNodeID = 0; secondNodeID < numberOfNodes; secondNodeID++) {
                        if (assignedColorsForEachNode[antIndex, nodeID] == assignedColorsForEachNode[antIndex, secondNodeID] && secondNodeID != nodeID) {
                            pheremone[nodeID, secondNodeID] += ((double)numberOfNodes / Math.Pow(totalNumberOfColorsUsedForEachAnt[antIndex], 1));
                            pheremone[secondNodeID, nodeID] += ((double)numberOfNodes / Math.Pow(totalNumberOfColorsUsedForEachAnt[antIndex], 1));
                        }
                    }
                }
            }
        }

        private static int selectNode(List<int> nodesCanBeColoredWithCurrentColor, int[] currentDegrees, int currentColor, List<int> nodesCurrentlyColoredWithCurrentColor, Random random) {
            int selectedNode = -1; // Shouldn't be -1, if there is a bug, it will throw an exception this way.
            int numberOfUnColoredNodes = nodesCanBeColoredWithCurrentColor.Count;
            double[] heuristicCumulative = new double[numberOfUnColoredNodes];
            double[] pheromoneCumulative = new double[numberOfUnColoredNodes];
            double[] totalCumulative = new double[numberOfUnColoredNodes];

            double totalSum = 0;
            
            for (int i = 0; i < numberOfUnColoredNodes; i++) {
                heuristicCumulative[i] = Math.Pow(numberOfUnColoredNodes - currentDegrees[nodesCanBeColoredWithCurrentColor[i]], alpha); // (double)2.0*currentDegrees[nodesCanBeColoredWithCurrentColor[i]] / numberOfNodes, alpha
                if (heuristicCumulative[i] > -0.0000001 && heuristicCumulative[i] < 0.0000001) // == 0
                    heuristicCumulative[i] = 1.0 / numberOfNodes;
                pheromoneCumulative[i] = Math.Pow(findTotalPheremone(nodesCanBeColoredWithCurrentColor[i], nodesCurrentlyColoredWithCurrentColor), beta);
                //Console.WriteLine("P: " + pheromoneCumulative[i] + " h: "+ heuristicCumulative[i]);
                totalCumulative[i] = heuristicCumulative[i] * pheromoneCumulative[i];
                totalSum = totalSum + totalCumulative[i];
            }
            for (int i = 0; i < numberOfUnColoredNodes; i++) {
                totalCumulative[i] = totalCumulative[i] / totalSum;
            }
            if (binarySelection) {
                if (numberOfUnColoredNodes == 1) {
                    return nodesCanBeColoredWithCurrentColor[0];
                } else if (numberOfUnColoredNodes == 2) {
                    if (totalCumulative[0] > totalCumulative[1]) {
                        return nodesCanBeColoredWithCurrentColor[0];
                    } else {
                        return nodesCanBeColoredWithCurrentColor[1];
                    } 
                } else {
                    int index1 = random.Next(numberOfUnColoredNodes);
                    int index2 = random.Next(numberOfUnColoredNodes);
                    if (index1 == index2) {
                        if (index2 == numberOfUnColoredNodes - 1) {
                            index2 = 0;
                        } else {
                            index2++;
                        }
                    }
                    if (totalCumulative[index1] > totalCumulative[index2]) {
                        return nodesCanBeColoredWithCurrentColor[index1];
                    } else {
                        return nodesCanBeColoredWithCurrentColor[index2];
                    }
                }
            } else {
                double accumulate = 0;
                double randomNumber = random.NextDouble();

                for (int i = 0; i < numberOfUnColoredNodes; i++) {
                    accumulate = accumulate + totalCumulative[i];

                    if (accumulate >= randomNumber) {
                        selectedNode = nodesCanBeColoredWithCurrentColor[i];
                        break;
                    }
                }
            }
            return selectedNode;
        }
        private static double findTotalPheremone(int node, List<int> nodesCurrentlyColoredWithCurrentColor) {
            int numberOfNodesColoredWithSameColor = nodesCurrentlyColoredWithCurrentColor.Count;
            if (numberOfNodesColoredWithSameColor == 0)
                return 1.0 / numberOfNodes;
            double totalPh = 0;
            for (int i = 0; i < numberOfNodesColoredWithSameColor; i++) {
                totalPh += pheremone[nodesCurrentlyColoredWithCurrentColor[i], node];
            }
            return totalPh;
        }
        private static int[] findNewDegrees(List<int> nodesCanBeColoredWithCurrentColor) { // find the degrees in SUBGRAPH!
            int[] currentDegrees = new int[numberOfNodes];
            int numberOfUnColoredNodes = nodesCanBeColoredWithCurrentColor.Count;
            for (int i = 0; i < numberOfUnColoredNodes; i++) {
                for (int j = i + 1; j < numberOfUnColoredNodes; j++) {
                    if (edges[nodesCanBeColoredWithCurrentColor[i], nodesCanBeColoredWithCurrentColor[j]]) {
                        currentDegrees[nodesCanBeColoredWithCurrentColor[i]]++;
                        currentDegrees[nodesCanBeColoredWithCurrentColor[j]]++;
                    }
                }
            }
            return currentDegrees;
        }
        private static void prepareListOfEveryNode() {
            listOfAllNodes = new List<int>();
            for (int i = 0; i < numberOfNodes; i++)
                listOfAllNodes.Add(i);
        }
        private static void findDegrees() {
            degrees = new int[numberOfNodes];
            for (int i = 0; i < numberOfNodes; i++) {
                for (int j = i+1; j < numberOfNodes; j++) {
                    if (edges[i, j]) {
                        degrees[i]++;
                        degrees[j]++;
                    }
                }
            }
        }
        private static void initializePheremone() {
            for (int i = 0; i < numberOfNodes; i++) {
                for (int j = 0; j < numberOfNodes; j++) {
                    pheremone[i, j] = 1.0 / numberOfNodes; // Minimum possible
                }
            }
        }
        private static void ReadData(string filenamex) {
            try {
                String file_name = "";
                if (filenamex.Equals(""))
                    file_name = filename;
                else
                    file_name = filenamex;

                String[] texts;
                using (StreamReader sr = new StreamReader(file_name)) {
                    String fullText = sr.ReadToEnd();
                    texts = fullText.Split('\n');
                }
                String[] firstLineData = System.Text.RegularExpressions.Regex.Split(texts[0], @"\s+");
                numberOfNodes = Int32.Parse(firstLineData[0]);
                numberOfEdges = Int32.Parse(firstLineData[1]);
                edges = new bool[numberOfNodes, numberOfNodes]; // 
                pheremone = new double[numberOfNodes, numberOfNodes];

                for (int i = 1; i < numberOfEdges + 1; i++) {
                    String[] LineData = System.Text.RegularExpressions.Regex.Split(texts[i], @"\s+");
                    int from = Int32.Parse(LineData[0]);
                    int to = Int32.Parse(LineData[1]);

                    edges[from, to] = true;
                    edges[to, from] = true;
                }
            } catch (Exception e) {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }
        }
    }
}
