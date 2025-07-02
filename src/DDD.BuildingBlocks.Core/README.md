# DDD.BuildingBlocks

Table of Content

*   [Introduction](#_introduction)
*   [Overview](#_overview)
    *   [Library Core](#_library_core)
    *   [Third Party Provider Packages](#_third_party_provider_packages)


Oliver Grau <[oliver.grau@grausoft.net](mailto:oliver.grau@grausoft.net)>

Documentation for developers to use the DDD.BuildingBlocks Library.

## Introduction

The DDD.BuildingBlocks Solution provides a collection of components to quickly and efficiently use selected tactical patterns of the DDD philosophy for your own projects.


This documentation assumes that the reader knows and understands the concepts behind the Tactical Patterns of the DDD philosophy.

## Overview

The library is basically divided into a core package and various third-party specific implementations, divided into so-called packages.

The following table gives a short overview:

<table><caption>Table 1\. Basic breakdown of the library</caption> <colgroup><col> <col></colgroup>
<thead>
<tr>
<th>Component</th>
<th>Description</th>
</tr>
</thead>
<tbody>
<tr>
<td>
DDD.BuildingBlocks.Core
</td>
<td>

Provides interfaces and base classes for implementing the following tactical patterns:

*   Aggregate Roots
*   Event Sourcing & Snapshotting
*   Event driven architecture
*   Entities & Value Objects
*   Repositories
*   Command Pattern
</td>
</tr>
<tr>

<td>
DDD.BuildingBlocks.DevelopmentPackage
</td>

<td>
Provides implementations of certain components as an in-memory version. Is only required in the development phase.
</td>
</tr>
</tbody>
</table>

### Library Core

The **DDD.BuildingBlocks.Core** component provides all required interfaces and basic implementations. The component is divided into the following areas:

<table><caption>Table 2\. The Library Core</caption> <colgroup><col><col ></colgroup>
<thead>
<tr>

<th>Section</th>
<th>Description</th>

</tr>
</thead>
<tbody>
<tr>

<td>
Commanding
</td>
<td>
Classes and interfaces for the use of the _Command Pattern_.
</td>
</tr>
<tr>
<td>
Domain
</td>
<td>
Building Blocks for the core model (the domain)
</td>
</tr>
<tr>
<td>
Event
</td>
<td>
Classes and interfaces for the realisation of an event driven architecture.
</td>
</tr>
<tr>
<td>
Persistence
</td>
<td>
Interfaces for connecting an external persistence system. The persistence-based part of the library is based on the repository pattern with the use of abstract storage providers. Contains a base implementation for an event sourcing based repository, which is persistence agnostic.
</td>
</tr>
<tr>
<td>
Utilities
</td>
<td>
Auxiliary classes for various cross-sectional aspects.
</td>
</tr>
</tbody>
</table>

### Third Party Provider Packages
The following table provides an overview of the currently available provider packages.


<table><caption>Table 3\. Third Party Packages</caption> <colgroup><col><col ></colgroup>
<thead>
<tr>
<th>Section</th>
<th>Description</th>
</tr>
</thead>
<tbody>

<tr>
<td>
DDD.BuildingBlocks.MSSQLPackage
</td>
<td>
Provides implementations for an MSSQL based event store database. Thus, an ordinary MSSQLServer database can be used as storage for the events.
</td>
</tr>
</tbody>
</table>
