# Dataverse Strong Type Entity Class Generator

Microsoft Dynamics CRM / Dynamics 365 / Dataverse Strong Type Entity Class Generator.

# Download

[https://marketplace.visualstudio.com/items?itemName=JohanKustner.DataverseClassGeneratorVS2022]()

# Overview

This tool provides a standalone option to generate strongly typed entity classes for the Dataverse, Dynamics 365 and Dynamics CRM.

This tool essentially wraps up extensions to CrmSvcUtil.exe.

The great thing about this tool is that it is installed as an extension to run from the Tools menu of Visual Studio. It provides a UI where the user can select which Entities/Tables to generate Classes for. The user can select which Project the Classes should be created in. The user can also choose to create a separate class file for each class.

This tool uses the Microsoft XRM Tooling capability for authentication with Dynamics CRM, Dynamics 365 and the Dataverse. The tool works for on premise and online implementations.

This tool doesn't generate duplicate Enum definitions if the user chooses to create separate class files, like some other tools do. The tool also names the Classes and Enum definitions using the Display Name of the Entity or Attribute instead of the Schema Name or Logical Name, thus removing those annoying Publisher Prefixes and the underscores. This allows compliance with Microsoft Naming Conventions for Code Analysis. XML comments are also added wherever required and corrections are made to the generation of OptionSetValue Properties, as there are errors with the CrmSvcUtil.exe doing this out-of-the-box.

An OrganizationServiceContext instance is also created.

# Visual Studio Versions

**Please note that this tool only supports and targets Visual Studio 2022. For older version, please use this tool: [https://marketplace.visualstudio.com/items?itemName=JohanKustner.Dynamics365StrongTypeEntityClassGenerator]()**

# Version History

## Version 1.0

Initial release.
