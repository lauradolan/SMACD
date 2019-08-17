# SMACD data model

The SMACD data model integrates data about your system or application's [**features**](#features), [**use cases**](#use-cases), possible [**abuse cases**](#abuse-cases), [**plugin pointers**](#plugin-pointers) and [**resource pointers**](#resource-pointers).

![Nested model of different concepts in SMACD program architecture including features, use cases, abuse cases, testing and business objects.](Assets/ObjectModel.png "SMACD data model")

The following data modelling terms are used in the SMACD CLI tool:

## Features

Features are when groups of use cases result in a functionality or capability within an application. 

For example, a "search inventory" feature.

## Use cases

Use cases are the basis of workflows that a user follows in order to accomplish some task. These are usually implemented as features.

For example, to implement a "search inventory" feature, you might need to:

1. Register a user
2. Log in a user
3. Gather the current inventory
4. Correlate a search term with a product ID
5. Search the current inventory for that product ID's availability

## Abuse cases

Abuse cases are workflows that an attacker can use to manipulate a use case into performing an unintended task. Frequently, these unintended tasks are malicious ways to get a foothold to further attack the application.

For example, to abuse a "search inventory" feature, a malicious actor might perform a SQL injection in the search form.

## Plugin pointers

Plugin pointers are data structures that contain a plugin identifier, parameters for the plugin, and a [resource pointer](#resource-pointer) to the resource being used by the plugin.

## Resource pointers

Resource pointers are data structures that contain a resource identifier, the type of resource being identified, and parameters for the resource.
