
<#
.SYNOPSIS
Cattle Disk Prep

.EXAMPLE
c:> cattle.exe

.LINK
http://ps2wiz.codeplex.com

.NOTES
Copyright Keith Garner (KeithGa@KeithGa.com) all rights reserved.
Microsoft Reciprocal License (Ms-RL) 
http://www.microsoft.com/en-us/openness/licenses.aspx

#>

[cmdletbinding(DefaultParameterSetName="None")]
param(
    [parameter(Mandatory=$true)]
    [System.IO.FileInfo] $ImagePath,
    [string] $Updates,
    [string] $Name,
    [int]    $Index,
    [PSCustomObject[]] $MyFields,
    [switch] $Force,

    [parameter(Mandatory=$true, ParameterSetName="USB")] [string] $USBPath,
    [parameter(Mandatory=$true, ParameterSetName="VHD")] [string] $VHDPath,
    [parameter(Mandatory=$true, ParameterSetName="ISO")] [string] $ISOPath,
    [parameter(Mandatory=$true, ParameterSetName="HV2")] [string] $HyperVName,

    [parameter(Mandatory=$true, ParameterSetName="HV2")] [int] $Generation = 2
)

#region script Prep
###########################################################

$ErrorActionPreference = 'Stop'

########
Import-module DeployShared -Force 
########

function get-MyFilePath ( [parameter(Mandatory=$true)] [System.IO.FileInfo] $FileName ) { 
    if ( Test-Path $FileName ) {
        if ( [System.Windows.Forms.MessageBox]::Show((Get-WindowOwner),"File Exists`r`nOK to overwrite file?","Overwrite",1, 64) -eq 2 ) {
            get-MyFilePath | Write-Output
        }
    }
    $FileName | Write-Output
}

function get-MyDirPath ( [parameter(Mandatory=$false)] [System.IO.DirectoryInfo] $Directory ) { $Directory | Write-Output }

if ( -not ( Test-Path $imagepath )) { throw "bad imagepath" }
if ( $ImagePath.Extension -notin ".iso",".img" ) { throw "Not ISO or WIM image" }

Write-verbose "found image: $ImagePath"

#endregion

#region Mount ISO and get WIM Data
###########################################################

$Host.UI.RawUI.WindowTitle = "Select Image"

if ( $ImagePath.Extension -in ".iso",".img" )
{
    Write-Verbose "DVD ISO processing..."
    if ((get-diskimage $ImagePath.FullName -erroraction silentlycontinue | Get-Volume) -isnot [object])
    {
        write-verbose "Mount DVD:  $($ImagePath.FullName)"
        mount-diskimage $ImagePath.FullName
    }

    $DVDDrive = Get-DiskImage $ImagePath.FullName | get-Volume
    $DVDDrive | out-string | write-verbose
    if ( $DVDDrive -isnot [Object] ) { throw "Get-DiskImage Failed for $($SourceImageFile.FullName)" }

    $WimImage = "$($DVDDrive.DriveLetter)`:\sources\install.wim"
}
elseif ( $ImagePath.Extension -eq ".WIM" )
{
    $WimImage = $ImagePath.FullName
}

if ( -not ( Test-Path $wimimage ) ) { throw "missing WIM file: $WimImage" }

#endregion

#region get-wimdata
###########################################################

$Host.UI.RawUI.WindowTitle = "WIM Data"

$ImageData = Get-WindowsImage -ImagePath $WimImage

if ( $ImageData.Count -eq 0 ) {
    throw "missing ImageData $WimImage"
}
elseif ( $ImageData.Count -eq 1 ) {
    $ImageName = $ImageData | Select-Object -First 1 -ExpandProperty ImageName
}
elseif ( $index -and ( $ImageData | Where-Object ImageIndex -eq $Index ) ) {
    Write-Host "Match Index $Name"
    $ImageName = $ImageData | Where-Object ImageIndex -Match $Index | Select-Object -ExpandProperty ImageName
}
elseif ( $Name -and ( $ImageData | Where-Object ImageName -match $Name ) ) {
    Write-Host "Match Name $Name"
    $ImageName = $ImageData | Where-Object ImageName -match $Name | Select-Object -ExpandProperty ImageName
}
else {

    $Host.UI.RawUI.WindowTitle = "Image-Index"
    Clear-Host
    Write-Host @"

    Select Image Name:

"@

    $ImageName = get-windowsimage -ImagePath $WimImage | 
        Select-Object -ExpandProperty ImageName | 
        out-GridView -OutputMode Single

    if ( -not $ImageName ) { throw "Missing selected image" }

}

write-host "ImagePath: $ImagePath `r`nImageName: $ImageName"

#endregion

#region Variables
###########################################################

$Host.UI.RawUI.WindowTitle = "Variables"

$MyDefaults = @(
    [PSCustomObject] @{ Tag="ComputerName"; Name="Computer Name"; Value = '*'; ToolTip = "ComputerName ( * = Random )"; },
    [PSCustomObject] @{ Tag="AdministratorPassword"; Name="Administrator Password"; Value = ( 'P@ssw0rd' ); ToolTip = "Administrator Account Password"; },

    [PSCustomObject] @{ Tag="TimeZone"; Name="Time Zone"; Value = ( [System.Timezone]::CurrentTimezone.StandardName ); ToolTip = "TimeZone"; },
    [PSCustomObject] @{ Tag="SystemLocale"; Name="SystemLocale"; Value = ( (Get-WinUserLanguageList).LanguageTag ); ToolTip = "System Locale"; },
    [PSCustomObject] @{ Tag="IEHarden"; Name="Harden IE"; Value = ( 'false' ); ToolTip = "Harden IE for all users"; },

    [PSCustomObject] @{ Tag="EnableTS"; Name="Enable Terminal Services"; Value = ( 'false' ); ToolTip = "Enable Terminal Services and open Firewall Port"; },
    [PSCustomObject] @{ Tag="PSRemoting"; Name="Enable Powershell Remoting"; Value = ( 'true' ); ToolTip = "Enable PowerShell Remoting"; },
    [PSCustomObject] @{ Tag="ExecutionPolicy"; Name="PowerShell Execution Policy"; Value = ( 'RemoteSigned' ); ToolTip = "Set-ExecutionPolicy for 64bit and 32bit"; },

    [PSCustomObject] @{ Tag=""; Name=""; value = ''; tooltip = '' },
    # [PSCustomObject] @{ Tag="Updates"; Name="Updates Directory"; Value = ''; ToolTip = "Directory containing Updates for Image"; },

    [PSCustomObject] @{ Tag="AdditionalPS1"; Name="Additional PS1 Script to run"; Value = ( 'https://raw.githubusercontent.com/KuduApps/CustomPowershell/master/hello.ps1' ); ToolTip = "Additional Configuration Script to launch"; }
)

if ( $MyFields ) {
    Write-Verbose "MyFields already present"
    $MyFields | out-string -Width 200 | write-verbose
}
elseif ( get-command edit-keyvaluepair -ErrorAction SilentlyContinue ) {
    cls
    Write-Host "Common Variables"
    $MyFields = $MyDefaults | edit-KeyValuePair -HeaderWidths 0,-170,10000,0
    $MyFields | Select-Object -Property Tag,Value | Out-String | Write-verbose
}
else {
    $MyFields = $MyDefaults
}

function Get-PropValue ( $name ) { $MyFields | where-object Tag -eq $Name | Where-object Value -ne '' | Select-object -first 1 -ExpandProperty value } 

#endregion

#region Create Unattend.xml
###########################################################

new-item -ItemType directory -force -path $env:temp\newISO | out-string | write-verbose

write-verbose "UNattend"
UNATTEND  {

    write-verbose "Specialize"
    SETTINGS specialize  {
        COMPONENT Microsoft-Windows-Shell-Setup {
            ELEMENT TimeZone (Get-PropValue 'TimeZone')
            ELEMENT ComputerName (Get-PropValue 'ComputerName')
        }

        COMPONENT Microsoft-Windows-IE-ESC { 
            ELEMENT IEHardenAdmin (Get-PropValue 'IEHarden').ToString().TOLower()
            ELEMENT IEHardenUser  (Get-PropValue 'IEHarden').ToString().TOLower()
        } 

        COMPONENT Microsoft-Windows-IE-InternetExplorer {
            ELEMENT Home_Page "about:tab"
        }
        
        COMPONENT Microsoft-Windows-TerminalServices-LocalSessionManager {
            ELEMENT fDenyTSConnections (-not (get-PropValue 'EnableTS')).ToString().ToLower()
        }

        COMPONENT Microsoft-Windows-TerminalServices-RDP-WinStationExtensions {
            ELEMENT UserAuthentication '0'
        }

        COMPONENT Networking-MPSSVC-Svc {
            ELEMENT FirewallGroups {
                ELEMENT FirewallGroup -TypeAdd -Namespace 'RemoteDesktop' {
                    ELEMENT Active $True.ToString().ToLower()
                    ELEMENT Profile 'all'
                    ELEMENT Group '@FirewallAPI.dll,-28752'
                }
            }
        }

    }

    write-verbose "OOBESystem"
    SETTINGS oobeSystem {
        COMPONENT Microsoft-Windows-Shell-Setup {
            ELEMENT OOBE {
                ELEMENT NetworkLocation "Other"
                ELEMENT ProtectYourPC "1"
                ELEMENT HideEULAPage $True.ToString().ToLower()
                ELEMENT HideOnlineAccountScreens $True.ToString().ToLower()

                ELEMENT HideWirelessSetupInOOBE $True.ToString().ToLower()
                ELEMENT HideLocalAccountScreen $True.ToString().ToLower()
                ELEMENT HideOnlineAccountScreens $True.ToString().ToLower()
            }

            ELEMENT AutoLogon {
                ELEMENT LogonCount "1"
                ELEMENT UserName "Administrator"
                ELEMENT Enabled "true"
                PASSWORD "Password" (Get-PropValue 'AdministratorPassword')
            }

            ELEMENT UserAccounts {
                PASSWORD "AdministratorPassword" (Get-PropValue 'AdministratorPassword')
            }

            ELEMENT FirstLogonCommands {

                $NewExecutionPolicy = $MyFields | where-object Tag -eq 'ExecutionPolicy' | Select-Object -ExpandProperty Value -first 1
                if( $NewExecutionPolicy -in 'AllSigned','RemoteSigned','Unrestricted','Bypass' )
                {
                    write-verbose "Add PSRemoting"

                    ELEMENT SynchronousCommand -TypeAdd -ForceNew {
                        ELEMENT Description "Set powershell execution policy to remote signed (32-bit)"
                        ELEMENT Order "3"
                        ELEMENT CommandLine "reg.exe add HKLM\SOFTWARE\Microsoft\PowerShell\1\ShellIds\Microsoft.PowerShell /v ExecutionPolicy /d $NewExecutionPolicy /f /reg:32"
                        ELEMENT RequiresUserInput $true.Tostring().ToLower() 
                    }

                    ELEMENT SynchronousCommand -TypeAdd -ForceNew {
                        ELEMENT Description "Set powershell execution policy to remote signed (64-bit)"
                        ELEMENT Order "4"
                        ELEMENT CommandLine "reg.exe add HKLM\SOFTWARE\Microsoft\PowerShell\1\ShellIds\Microsoft.PowerShell /v ExecutionPolicy /d $NewExecutionPolicy /f /reg:64"
                        ELEMENT RequiresUserInput $true.Tostring().ToLower() 
                    }

                }

                if( $MyFields | where-object Tag -eq 'PSRemoting' | where-object value -eq 'true' )
                {

                    ELEMENT SynchronousCommand -TypeAdd -ForceNew {
                        ELEMENT Description "Enable-PSRemoting"
                        ELEMENT Order "5"
                        ELEMENT CommandLine 'powershell.exe -command "echo ''Enable-PSRemoting''; Enable-PSRemoting -force"'
                        ELEMENT RequiresUserInput $true.Tostring().ToLower() 
                    }

                    ELEMENT SynchronousCommand -TypeAdd -ForceNew {
                        ELEMENT Description "set trustedhosts to ALL (less secure)"
                        ELEMENT Order "6"
                        ELEMENT CommandLine 'powershell.exe -command "echo ''set trustedhosts''; Set-Item wsman:localhost\client\trustedhosts -Value * -force"'
                        ELEMENT RequiresUserInput $true.Tostring().ToLower() 
                    }

                    ELEMENT SynchronousCommand -TypeAdd -ForceNew {
                        ELEMENT Description "winrm quick config"
                        ELEMENT Order "7"
                        ELEMENT CommandLine "cmd.exe /c winrm.cmd quickconfig -transport:HTTP -force"
                        ELEMENT RequiresUserInput $true.Tostring().ToLower() 
                    }

                }

                $AdditionalCmd = Get-PropValue 'AdditionalPS1'
                if( $AdditionalCmd )
                {
                    ELEMENT SynchronousCommand -TypeAdd -ForceNew {
                        ELEMENT Description "Run Additional Commands"
                        ELEMENT Order "8"
                        ELEMENT CommandLine "powershell -executionpolicy RemoteSigned ""Start-Transcript; echo 'remote script'; iwr '$AdditionalCmd' -UseBasicParsing | iex"""
                        ELEMENT RequiresUserInput $true.Tostring().ToLower() 
                    }
                }

                ELEMENT SynchronousCommand -TypeAdd -ForceNew {
                    ELEMENT Description "Finished"
                    ELEMENT Order "999"
                    ELEMENT CommandLine 'powershell.exe -command "echo ''reboot machine''; start-sleep 10; shutdown.exe -r -f -t 0"'
                    ELEMENT RequiresUserInput $true.Tostring().ToLower() 
                }

            }

        }
        COMPONENT Microsoft-Windows-International-Core {
            ELEMENT InputLocale   (Get-PropValue 'SystemLocale')
            ELEMENT SystemLocale  (Get-PropValue 'SystemLocale')
            ELEMENT UILanguage    (Get-PropValue 'SystemLocale')
            ELEMENT UserLocale    (Get-PropValue 'SystemLocale')
        }
    }

    write-verbose "WindowsPE"
    SETTINGS windowsPE {

        COMPONENT Microsoft-Windows-International-Core-WinPE {
            ELEMENT SetupUILanguage {
                ELEMENT UILanguage   (Get-PropValue 'SystemLocale')
            }
            ELEMENT InputLocale   (Get-PropValue 'SystemLocale')
            ELEMENT SystemLocale  (Get-PropValue 'SystemLocale')
            ELEMENT UILanguage    (Get-PropValue 'SystemLocale')
            ELEMENT UILanguageFallback    (Get-PropValue 'SystemLocale')
            ELEMENT UserLocale    (Get-PropValue 'SystemLocale')
        }

        COMPONENT Microsoft-Windows-Setup {
            ELEMENT UserData {
                ELEMENT AcceptEula $true.ToString().ToLower()
                if ( $ImageName -notmatch "(server|enterprise|education)" )
                {
                    ELEMENT ProductKey {
                        ELEMENT Key "12345-12345-12345-12345-12345"
                        ELEMENT WillShowUI "OnError" 
                    }
                }
            }
        }
        
    }


}  | Save-XMLFile -path $env:temp\NewISO\AutoUnattend.xml

#endregion

#region Select Target Type
###########################################################

$Host.UI.RawUI.WindowTitle = "Target"

if ( -not ( $UsbPath -or $VHDPath -or $ISOPath -or $HyperVName ) )
{

    clear-host
    $title = "Select Target Type"

    $ChooseUSB = New-Object System.Management.Automation.Host.ChoiceDescription "&USB Flash Drive"
    $ChooseVHD = New-Object System.Management.Automation.Host.ChoiceDescription "&VHD Image File"
    $ChooseISO = New-Object System.Management.Automation.Host.ChoiceDescription "&ISO Image File"
    $ChooseVM = New-Object System.Management.Automation.Host.ChoiceDescription "&Hyper-V Machine"

    $options = [System.Management.Automation.Host.ChoiceDescription[]]($ChooseUSB, $ChooseVHD,$ChooseISO,$ChooseVM)

    switch ($host.ui.PromptForChoice($title, $null, $options, 0) ) {
        0 { 
            write-verbose "Selected USB"

            $Disks = Get-Disk | where-object bustype -eq 'usb'
            while ( -not $Disks ) {
                clear-host
                Write-Host @"

    No USB Disk found.

    Please insert USB Disk now!

    Disk should be formatted as Fat32, with one active partition for non-UEFI

"@
                $host.ui.RawUI.ReadKey() | out-null
            }

            $TargetDisk = $Disks | Select-Object -Property DiskNumber,PartitionStyle,OperationalStatus,@{Name="Size"; Expression = { ($_.Size / 1gb).ToString("0.00")+'GB' }},FriendlyName | out-gridview -OutputMode Single
            $TargetDisk | Out-String | write-verbose
            if ( -not $TargetDisk ) { throw "Missing Target Disk!" }

            $System = $TargetDisk | Get-Partition | Get-Volume
            $System | Out-String | write-verbose
            if ( -not $System ) { throw "Missing Target Volume!" }
            if ( $System.FileSystem -ne 'FAT32') { throw "Filesystem not Fat32 $($System.FileSystem)" }
            $USBPath = $System.DriveLetter + ':\'

            # TODO - test to see if $USBPath\Sources\install.wim alrady exists

        }
        1 { write-host "What is the path to the target VHD file:"; $vhdPath = get-MyFilePath;  }
        2 { write-host "What is the path to the target ISO file"; $ISOPath = get-MyFilePath }
        3 { Write-Host "What is the HyperV Host Name:"; $HyperVName = Read-Host }
    }
}

#endregion

#region Prep Working Environment
###########################################################

$Host.UI.RawUI.WindowTitle = "Processing..."
clear-host
Write-Host "Working..."

if ( $VHDPath -or $HyperVName ) {

    if ( $HyperVName ) {
        $VHDPath = "$((get-vmHost).VirtualHardDiskPath)\$($Name).vhdx"
    }

    if (Test-Path $VHDPath ) { remove-item $VHdpath }

    $ConvertWVArgs = @{
        ImagePath = $WimImage
        VHDFile = $VHDPath
        Name = $ImageName
        Generation = $Generation
    }
    $Packages = Get-PropValue 'Updates' | ForEach-Object { get-childitem -Path $_ }
    if ( $Packages ) { $ConvertWVArgs.add('Packages',$Packages) }
    Convert-WIMtoVHD @ConvertWVArgs

    if ( $HyperVName ) {
        Write-Verbose "start Hyper-V $HyperVName"
        New-HyperVirtualMachine -Name $HyperVName -Startup -VHDPath $VHDPath
    }
}
elseif ( $ISOPath -or $USBPath ) {

    if ($ISOPath) {
        $USBPath = New-TempDirectory
        write-verbose "Write ISOPath: $ISOPath USB: $USBPath"
        copy-itemwithprogress /e (Split-Path (Split-Path $WimImage))  $USBPath | out-string |write-verbose 

    }
    else {
        copy-itemwithprogress /max:4294967295 /e (Split-Path (Split-Path $WimImage)) $USBPath | out-string |write-verbose 
    }

    ########
    if ( -not ( test-path "$USBPath\sources\install.wim" ) ) 
    {
        write-verbose "Split WIMs $USBPath\sources\install.wim   $USBPath\Sources\Install.swm "
        $LogPath = Get-NewDismArgs
        push-location "$USBPath\Sources"
        dism.exe /split-image "/ImageFile:$WimImage" /SWMFile:Install.swm /FileSize:4095 /logpath:$($LogPath.LogPath) | out-string | write-verbose
        pop-location
    }
        
    ########
    copy-item $env:temp\NewISO\AutoUnattend.xml $usbpath\

    if ($ISOPath) {
        new-isoimage -SourcePath $USBPath -ISOTarget $ISOPath
        Remove-Item -Path $USBPath -Recurse -Force
    }
}
else {
    throw "unknown target"
}
#endregion

#region Cleanup
###########################################################

if ( $ImagePath.Extension -in ".iso",".img" ) {
    Write-Verbose "cleanup image"
    Dismount-DiskImage $ImagePath.FullName -ErrorAction SilentlyContinue
}

#endregion