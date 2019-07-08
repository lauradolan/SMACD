# System Mapping and Architectural Concept Diagram
The concept behind this is to create a threat modeling approach that is scalable as well as being designed with attacker behavior in mind.

## Data Model
![SMACD Data Model image](https://github.com/anthturner/SMACD/blob/master/DocAssets/ObjectModel.png?raw=true "SMACD Data Model image")

#### Features
Features are groups of Use Cases that come together to implement a high-level Feature in an application, such as "Search Inventory".

#### Use Cases
Use Cases are workflows that a user follows in order to accomplish some task, usually implemented as a Feature.

#### Abuse Cases
Abuse Cases are workflows that an attacker can use to manipulate a Use Case into doing something unintended, frequently as a way to get a foothold to further attack an application.

#### Plugin Pointers
Data structures containing a Plugin Identifier, parameters for the plugin, and a Resource Pointer to the Resource being used by the Plugin

#### Resource Pointers
Data structures containing a Resource Identifier, the type of Resource being described, and parameters for the Resource

## Workflow
![SMACD Tool Workflow image](https://github.com/anthturner/SMACD/blob/master/DocAssets/Workflow.png?raw=true "SMACD Tool Workflow image")

**Describing a Scan Workflow:**
1. A Service Map is loaded
2. Features are loaded, then Use Cases, then Abuse Cases, then Plugin Pointers
3. For each Plugin Pointer, look up the actual loaded Plugin instance
4. Using the instance, validate the Plugin Pointer to ensure required options are provided, and that the Resource Pointer provided can be consumed by the Plugin
5. Queue an execution of the Plugin with the TaskManager's Task Queue
6. When ready, TaskManager executes the queued item. When doing this, if there is a consumer of an event indicating that a task is being started, that consumer is notified
7. Plugin executes, placing artifacts in a given working directory
8. When complete, TaskManager notifies any consumers subscribed to the event indicating a task has completed
9. When all Plugins have completed, iterate over each result object, running operations to summarize the content of all reports and generate a risk score
10. Summary Report is generated, containing all result objects, a generalized list of vulnerabilities, scores for each result, and any Resources that were generated during a spider operation (not provided on the Resource Map)
