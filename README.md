# OPC UA - User modelled interface - V1.1.1

<div style="border: 1px solid gray; padding: 10px">
  <strong>What's new with V1.1.1</strong>
  <ul>
    <li><strong>Software Units</strong>: A new <a href="#create-server-interface-for-sw-units">action</a> allows the creation of user-modeled interfaces for Software Units.</li>
    <li><strong>Multi-dimensional arrays</strong>: Support for arrays with multiple dimensions.</li>
    <li><strong>Strings of length defined by Constant</strong>: Strings where the length is defined by a constant are now supported.</li>
  </ul>
</div>

## Description
This Add-In has been developed to streamline the creation of user-modeled interfaces for OPC UA servers. By browsing through the TIA Portal project, it incorporates all variables that are accessible via OPC UA and arranges them in a structured manner, eliminating the need for manual configuration and significantly reducing the time and effort required.

The main goals of the Add-In are:

- **Enhanced performance:** Replace the default SIMATIC server interface with modelled interfaces that offer improved performance. With modelled interfaces, OPC UA clients can create monitored items and access data (using read or write services) up to 6 times faster.
- **Automatic interface generation for S7-1200 and S7-1500 R/H PLCs:** Create server interfaces for S7-1200 and S7-1500 R/H PLCs, which do not support the default SIMATIC server interface.
- **Customization to fit project needs:** Provide flexibility by allowing customization of the server interface to meet specific project requirements. With the "Extend create" option, users can modify certain parameters of the server interface and adapt it to their needs.

## Table of contents

1. [Installation](#installation)
2. [Usage](#usage)
3. [Features](#features)
4. [How it works](#how-it-works)
5. [Tia Portal version](#tia-portal-version)

<a name="installation"></a>
## Installation

Follow these steps to carry out the installation process:

1. Download the repository to the local machine.
2. Open Visual Studio, selecting the option: "Run as Administrator".
3. Ensure that all necessary references are included in the project **AddinOpcUaInterface**. If any references are missing, select the Openness DLL files from TIA Portal version V19 as your reference source.
4. Build the solution. The post-build event command will automatically transfer the .addin file to the **C:\Program Files\Siemens\Automation\Portal V19\AddIn** directory.

**NOTE:** If TIA Portal version V17 or V18 is being used, please refer to the instructions described in [Chapter 4](#tia-portal-version).

<a name="usage"></a>
## Usage
To use the Add-In in TIA Portal, make sure that the **OpcUa-UserModelledInterface.addin** file is located in the following directory: **C:\Program Files\Siemens\Automation\Portal V19\AddIns**. 

Then, proceed with the following steps:

1. Open a TIA Portal project and navigate to the Add-Ins tab on the right of the screen.
2. Activate the Add-In with the dropdown menu. 

    <img src="media/activate-add.png" alt="Activate Add-In" width="400"/>

3. Once activated, right-click on a device.
4. Place the cursor over the Add-In. The two possible actions, "Create" and "Extend create," will appear.

    <img src="media/run-add.png" alt="Run Add-In" width="400"/>


### Create
The "Create" option enables users to recreate the default SIMATIC server interface. This means that all configuration parameters are pre-set, and the user only needs to specify the name of the server interface, namespace URI, and a file path to store the XML file.

<div style="text-align:center">
    <img src="media/create.png" alt="Create GUI" width="450" />
</div>

### Extend create
The "Extend create" option allows users to customize the interface according to their specific requirements. Some of the options offered by this action include:
- Setting filters to define the access levels of nodes within the server's AddressSpace.
- Optimizing the server interface: Exclude children nodes from UDTs, structs, arrays, etc. In large projects, the number of nodes  can be significantly reduced to fit the server's maximum node limit. - There are currently some [limitations](#features) regarding this feature.
- Choosing between strings or numeric identifiers for node identification: While the default SIMATIC server interface uses strings, numeric identifiers offer improved performance in terms of server storage and communication. - There are currently some [limitations](#features) regarding this feature.
- Removing empty data blocks: This option enables the removal of data blocks that do not contain any accessible variables via OPC UA, thus reducing the number of nodes in the server's AddressSpace.
- Preserving the folder structure of the project: Folders present in the project can be included in the server interface. Maintaining the folder structure allows for easier browsing of the AddressSpace, although it increases the number of nodes.

<div style="text-align:center">
    <img src="media/extend_create.png" alt="Extend create GUI" width="450" />
</div>

### Create server interface for SW Units
The "Create server interface for SW Units" action allows the creation of user-modeled interfaces for Software Units. To launch this action, right-click on the desired Software Unit and select "Create server interface for SW Unit".

<div style="text-align:center">
    <img src="media/create_sw_units.png" alt="Create server interface for SW Unit" width="450" />
</div>

**NOTE:** This option can only be executed with the .dll from TIA Portal V19.

<a name="features"></a>
## Features

### Features included in this version of the Add-In
- "Create" and "Extend create" actions have been implemented.
- Access level filters: Safety parameters are set to read-only by default.
- Optimization of the server interface: The Add-In currently operates in "optimize" mode by default, excluding children nodes from UDTs, structs, arrays, etc.
- Removal of empty data blocks: To reduce the number of nodes.
- Folder structure management: Users can choose to keep or exclude the folder structure of the project.
- Log file: Maintains a record of nodes that have not been added to the server interface.

<a name="limitations"></a>
### Limitations
Some of the Add-In's current limitations are listed below:
- It is not possible to use the Add-In without the "optimize" mode.
- NodeIDs cannot be configured with numeric identifiers.
- Function Blocks instantiated within other Function Blocks are modelled as "UAObjects" instead of "UAVariables". Therefore, the variables contained inside nested FBs are not accessible via the generated interface. 

**DISCLAIMER:** This Add-In is designed to serve as a starting point for users to build upon. Users are encouraged to expand upon this basic version by implementing additional functionalities according to their specific requirements.

<a name="how-it-works"></a>
## How it works
The Add-In utilizes the Openness API to access information stored in the TIA Portal project. To create a user-modelled interface, the Add-In follows these steps:
1. Imports an XML base template of the server interface as an XDocument.
2. Scans through all system and user datatypes in the project and includes them in the XDocument as UADataTypes and UAObjects.
3. Browses through the project's tag tables and adds all Tags to the server interface.
4. Repeats the previous step with the Global and Instance data blocks that are accessible via OPC UA.
5. Finally, the program exports the XDocument as an XML file, which is imported into TIA Portal to create the server interface.

<div style="text-align:center">
    <img src="media/flowchart-add-in.png" alt="Flowchart of the Add-In" width="1000" />
</div>

<a name="tia-portal-versions"></a>
## TIA Portal version

The Add-In is designed to run on TIA Portal V19, but it can be adapted to run on TIA Portal version V17 and V18. To do so:
- Update the references of the project **AddinOpcUaInterface**. Use the Openness DLL file "Siemens.Engineering.Addin" from TIA Portal versions V17 or V18 as your reference source. 

    **NOTE**: Openness has backward compatibility. This means that an Add-In built with the DLL files from TIA Portal V17 can also run on TIA Portal V18, V19 and future versions.
- Change the post-build event command to: 
    ```
    copy "$(ProjectDir)AddInPublisherConfig.xml" "$(TargetDir)AddInPublisherConfig.xml" /Y 
    "C:\Program Files\Siemens\Automation\Portal V1X\PublicAPI\V1X.AddIn\Siemens.Engineering.AddIn.Publisher.exe" -f "$(TargetDir)AddInPublisherConfig.xml" -l "$(TargetDir)PostBuildLog.txt" -v -c -o "C:\Program Files\Siemens\Automation\Portal V1X\AddIns\OpcUa-UserModelledInterface.addin"
    ```
    **NOTE**: Replace the placeholder "Portal V1X" with the specific version of TIA Portal installed on your machine. For example, if TIA Portal V18 is installed, replace "Portal V1X" with "Portal V18". Similarly, update "V1X.AddIn" to match the version of the Openness DLL files selected in the previous step. If the selected DLL files are those from TIA Portal V17, change "V1X.AddIn" to "V17.AddIn".

- Replace the content in the **AddInPublisherConfig.xml** file.

    For TIA V19:

        <PackageConfiguration xmlns="http://www.siemens.com/automation/Openness/AddIn/Publisher/V19">
            <Author>&lt;SIEMENS AG - DI FA S SUP SPH COM&gt;</Author>
            <AddInVersion>V19</AddInVersion>
            <Description>Automatically create user modelled interfaces for OPC UA servers.</Description>
            <DisplayInMultiuser />
            
            <Product>
                <Name>OPC UA - User modelled interface</Name>
                <Id>tia_add_in_opc_ua_interface</Id>
                <Version>1.1.0</Version>
            </Product>

            <FeatureAssembly>
                <AssemblyInfo>
                <Assembly>AddInOpcUaInterface.dll</Assembly>
                </AssemblyInfo>
            </FeatureAssembly>

            <AdditionalAssemblies>
                <AssemblyInfo>
                    <Assembly>AddInOPCUAInterface.UI.dll</Assembly>
                </AssemblyInfo>
            </AdditionalAssemblies>

            <!--Please check required permissions-->
            <RequiredPermissions>
                <TIAPermissions>
                <TIA.ReadWrite/>
                </TIAPermissions>
                <UnrestrictedPermissions>
                    <System.UnrestrictedAccess>
                        <JustificationComment>Needed for the execution of the "OPC UA - User Modelled Interface" Add-In</JustificationComment>
                    </System.UnrestrictedAccess>
                </UnrestrictedPermissions>
            </RequiredPermissions>
        </PackageConfiguration>
    
    For TIA V18:

        <PackageConfiguration xmlns="http://www.siemens.com/automation/Openness/AddIn/Publisher/V18">
            <Author>&lt;SIEMENS AG - DI FA S SUP SPH COM&gt;</Author>
            <AddInVersion>V18</AddInVersion>
            <Description>Automatically create user modelled interfaces for OPC UA servers.</Description>
            <DisplayInMultiuser />
            
            <Product>
                <Name>OPC UA - User modelled interface</Name>
                <Id>tia_add_in_opc_ua_interface</Id>
                <Version>1.1.0</Version>
            </Product>

            <FeatureAssembly>
                <AssemblyInfo>
                <Assembly>AddInOpcUaInterface.dll</Assembly>
                </AssemblyInfo>
            </FeatureAssembly>

            <AdditionalAssemblies>
                <AssemblyInfo>
                    <Assembly>AddInOPCUAInterface.UI.dll</Assembly>
                </AssemblyInfo>
            </AdditionalAssemblies>

            <!--Please check required permissions-->
            <RequiredPermissions>
            <TIAPermissions>
                <TIA.ReadWrite/>
            </TIAPermissions>
            <SecurityPermissions>
                <System.Security.Permissions.SecurityPermission.UnmanagedCode/>
                <System.Security.Permissions.FileDialogPermission/>
                <System.Security.Permissions.UIPermission/>
                <System.Security.Permissions.MediaPermission/>
                <System.Security.Permissions.FileIOPermission/>
            </SecurityPermissions>
        </RequiredPermissions>
        </PackageConfiguration>

    For TIA V17:

        <PackageConfiguration xmlns="http://www.siemens.com/automation/Openness/AddIn/Publisher/V17">
            <Author>&lt;SIEMENS AG - DI FA S SUP SPH COM&gt;</Author>
            <AddInVersion>V17</AddInVersion>
            <Description>Automatically create user modelled interfaces for OPC UA servers.</Description>
            <DisplayInMultiuser />
            
            <Product>
                <Name>OPC UA - User modelled interface</Name>
                <Id>tia_add_in_opc_ua_interface</Id>
                <Version>1.1.0</Version>
            </Product>

            <FeatureAssembly>
                <AssemblyInfo>
                <Assembly>AddInOpcUaInterface.dll</Assembly>
                </AssemblyInfo>
            </FeatureAssembly>

            <AdditionalAssemblies>
                <AssemblyInfo>
                    <Assembly>AddInOPCUAInterface.UI.dll</Assembly>
                </AssemblyInfo>
            </AdditionalAssemblies>

            <!--Please check required permissions-->
            <RequiredPermissions>
            <TIAPermissions>
                <TIA.ReadWrite/>
            </TIAPermissions>
            <SecurityPermissions>
                <System.Security.Permissions.SecurityPermission.UnmanagedCode/>
                <System.Security.Permissions.FileDialogPermission/>
                <System.Security.Permissions.UIPermission/>
                <System.Security.Permissions.MediaPermission/>
                <System.Security.Permissions.FileIOPermission/>
            </SecurityPermissions>
        </RequiredPermissions>
        </PackageConfiguration>

# Reference links

OPC UA communication:
- [SIMATIC S7-1500, ET 200MP, ET 200SP, ET 200AL, ET 200pro, ET 200eco PN Communication](https://support.industry.siemens.com/cs/document/59192925/simatic-s7-1500-et-200mp-et-200sp-et-200al-et-200pro-et-200eco-pn-communication?dti=0&lc=en-WW)

Openness API:
- [TIA Portal Openness: Automation of engineering workflows](https://support.industry.siemens.com/cs/document/109792902/tia-portal-openness-automation-of-engineering-workflows?dti=0&lc=en-WW)
- [TIA Add-Ins](https://support.industry.siemens.com/cs/document/109773999/tia-add-ins?dti=0&lc=en-WW)
