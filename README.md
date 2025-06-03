# Real-Time-Adaptive-Pathfinding-using-Real-Time-Obstruction
User Guide: Pathfinding Simulation in Unity
This simulation demonstrates the behavior and performance of A*, D* Lite, and Adaptive pathfinding algorithms in both static and dynamic environments using a 3D grid-based setup in Unity. Users can interact with the simulation to trigger algorithm execution and observe real-time reactions to environmental changes.
System Requirements
•	Unity Editor (version 2022.3.21f1)
•	Compatible Windows/macOS system
•	Mouse and keyboard

Simulation Controls
•	Spacebar - Start or stop the pathfinding process. The agent will find a path from the start node to the goal node using the selected algorithm.
•	G - Activate or toggle random obstructions. Random tiles will become blocked to simulate dynamic changes in the environment.
The simulation supports toggling of obstruction mid-execution to observe how adaptive algorithms respond to real-time changes.
 

Simulation Features
•	Grid-based Environment: The simulation uses a 10x10, 25x25 and 50x50 tile grid where each tile can be free or blocked.
•	Algorithms Supported: A*, D* Lite, and an Adaptive algorithm combining both.
•	Visual Feedback:
o	Red: Agent
o	Blue: Destination
o	Yellow: Available Path
o	Green: Obstructed Tiles
•	Performance Monitoring:
o	Time elapsed and memory usage are logged for each run.

How to Use
1.	Launch the Simulation in Unity’s Play Mode.
2.	Press Spacebar to start pathfinding from the start to the goal position.
3.	(Optional) Press G to generate random obstacles dynamically.
4.	Observe how the algorithm adapts to new obstructions (Adaptive and D* Lite handles this in real-time).
5.	Stop the simulation to check results or restart for new trials.

