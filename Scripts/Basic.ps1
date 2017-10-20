<#
 .SYNOPSIS
Basic Machine Setup

.DESCRIPTION
Will perform the basic setup of a machine.

Will open up ports and services for remote administration, but not before securing accounts.

.NOTES

    TODO:
        Rip out server cfg below

#>

[cmdletbinding()]
param (
    [switch] $SkipFeatures,
    [switch] $SkipUpdate,
    [string[]] $OSFeatures = @("netfx3","Microsoft-Hyper-V")
    )

#region Initial Setup

if ( Test-Path 'c:\Pause.txt' ) { exit }

$Accounts = $null
$ErrorActionPreference = 'stop'
[bool] $RestartsRequested = $false
$isServer = !( gwmi win32_operatingsystem | Where-Object ProductType -eq 1 )

#endregion 

#region Account Support Functions 

#######################################

function Add-MicrosoftAccountToUser {
    [cmdletbinding()]
    param( $MicrosoftAccount, $User )

    if ( -not ( Test-Path $env:temp\PSExec.exe ) ) {
        Invoke-WebRequest -Uri 'http://live.sysinternals.com/psexec.exe' -OutFile $env:temp\PSExec.exe
    }

    [scriptBlock]$CommandRun = { 

        [cmdletbinding()]
        param( $MicrosoftAccount, $User )

        Start-Transcript -Path "c:\windows\log\MicrosoftAccount_$($User).log"
        Write-Host "Hello World: $MicrosoftAccount $User"

        $objUser = New-Object System.Security.Principal.NTAccount($User)
        $strSID = $objUser.Translate([System.Security.Principal.SecurityIdentifier])
        $c = New-Object 'byte[]' $strsid.BinaryLength
        $strSID.GEtBinaryForm($c,0)

        $FoundUser = $NULL
        foreach ($user in get-childitem "HKLM:\Sam\Sam\Domains\Account\Users") { 
            if ( $User.GetValue("V").length -gt 0 ) {
                $v = $User.GetValue("V")
                foreach ( $i in ($v.length-$c.Length)..0)  {
                    if ((compare-object $c $v[$i..($i-1+$c.length)] -sync 0).length -eq 0) {
                        $FoundUSer = $User
                        break
                    }
                }
            }
        }

        if ($FoundUser -is [object]) {
            Write-Verbose "Found USer: $($FoundUSer.PSPAth) now write $MicrosoftAccount"

            if ( $FoundUSer.GetValue("InternetUserName") -isnot [byte[]] ) {
                Set-ItemProperty $FoundUser.PSPath "ForcePasswordReset"   ([byte[]](0,0,0,0))
                Set-ItemProperty $FoundUser.PSPath "InternetUserName"     ([System.Text.Encoding]::UniCode.GetBytes($MicrosoftAccount))
                Set-ItemProperty $FoundUser.PSPath "InternetProviderGUID" ([GUID]("d7f9888f-e3fc-49b0-9ea6-a85b5f392a4f")).TOByteArray()
            }
        }

        Stop-Transcript
    }

    $tempfile = [System.IO.Path]::GetTempFileName() + '.ps1'
    $Prefix + $CommandRun.ToString() | Out-File -Encoding ascii -FilePath $tempFile

    write-verbose " Call: $tempfile -verbose -MicrosoftAccount '$Microsoftaccount' -User '$User'"
    $local:ErrorActionPreference = 'continue'
    & $env:temp\PSExec.exe /AcceptEula -e -i -s Powershell.exe -noprofile -executionpolicy bypass -File $tempfile -verbose -MicrosoftAccount "$Microsoftaccount" -User "$User" 2> out-null

    remove-item $tempFile -force   

}

function Request-UserNames { 
    [cmdletbinding(SupportsShouldProcess=$True,ConfirmImpact='high')]
    param ( [parameter(Mandatory=$true,HelpMessage="Username,email@MicrosoftAccount.com")] [string[]] $UserNames ) 

    $OutList = $UserNames | 
        ForEach-Object {
            $userObj = $_ -split ','
            [pscustomobject] @{ User = $UserObj[0]; MicrosoftAccount = $UserObj[1] }
        }

    if( $pscmdlet.ShouldProcess($env:computername,"Create Accounts `r`n`r`n"+ ($OutList | out-string) ) ) 
    {
        $OutList | Write-Output
        return
    }
    Request-USerNames | write-output
}


#######################################

#endregion 

#region Add Windows Features
#######################################

if ( -not $SkipFeatures ) {

    Write-Verbose "Gather the local path"

    $SxsPath = $null
    $SxsPath = get-volume | foreach-object { "$($_.DriveLetter)`:\Sources\SXS" } | where-object { Test-Path $_ }

    $FeatureArgs = @{}
    if ( $SxsPath ) { 
        $featureargs.Add( 'Source', $SxsPath )
        $FeatureArgs.Add( 'LimitAccess', $true )
    }

    Write-Verbose 'remove Hyper-V if running within a HyperVisor'
    if ( gwmi Win32_ComputerSystem | ? HyperVisorPresent -EQ 'True' ) {
        $OSFeatures = $OSFeatures | ? { $_ -ne 'Microsoft-Hyper-V' }
    }

    Write-Verbose "Adding Features..."
    $RestartsRequested += $OSFeatures | 
        Where-Object { get-WindowsOptionalFeature -Online -FeatureName $_ | Where-Object State -NE Enabled } |
        ForEach-Object { 
            Write-Host "Add feature $_"
            Enable-WindowsOptionalFeature -Online -FeatureName $_ -All -NoRestart @FeatureArgs
        } | 
        Where-Object RestartNeeded -eq $True

}

#endregion

#region Windows Update
#######################################

if ( -not $SkipUpdate ) {

    Write-Verbose "prototype, just a single pass..."

    if ( -not ( get-packageprovider | ? Name -eq NuGet ) ) {
        Install-PackageProvider -Name NuGet -Force
    }
    Install-Module PSWIndowsUpdate -Force
    Import-Module PSWIndowsUpdate 

    $RestartsRequested += !!( Get-WUInstall -MicrosoftUpdate -AcceptAll -IgnoreReboot | ? { $_ -match 'Reboot' } )

}

#endregion

#region Create Local User Accounts
#######################################

$Users = @{}
if ( test-path $env:temp\accounts.txt ) { 
    $Users = @{ UserNames = (type $env:temp\accounts.txt) -split ';' }
    #XXX TBD del $env:temp\accounts.txt -Force 
}

write-host ( '@' * 80 )
write-host ( '@' * 80 )

write-host @"
please enter a colection of users names:
    UserID,EmailAddress

Example:
    BillG,BillG@hotmail.com
    SteveB,SteveB@Hotmail.com
"@

foreach ( $NewAccount in Request-UserNames @Users ) {

    Write-Verbose "Create the account: $($NewAccounts.User)"
    net.exe user /add $($NewAccounts.User) /FullName:"$($NewAccounts.User)" /Expires:Never P@ssw0rd
    get-wmiobject -Class Win32_UserAccount -Filter "name='$($NewAccounts.User)'"  | swmi -Argument @{PasswordExpires = 0}
    write-host "net.exe localgroup administrators /add $($NewAccounts.User)"
    net.exe localgroup administrators /add $($NewAccounts.User)

    if ( $isServer -or [string]::IsNullOrEmpty($NewAccounts.MicrosoftAccount) ) {
        Write-Host "Enter Password:"
        net.exe user$($NewAccounts.User) *
    }
    else {
        Add-MicrosoftAccountToUser @NewAccount
    }
}

#endregion

#region Secure Local Administrator Account
#######################################

if ( ! $ISServer ) {
    Write-Verbose "Remove the local Administrator account if on Workstation..."
    if (  get-localuser |? SID -notmatch '(500|501|503)$' |? Enabled -EQ $True ) {
        Write-Verbose "There is at least one active account. So..."
        net user administrator /active:no
    }
}

if ( Get-WmiObject -Class Win32_UserAccount -filter "SID LIKE '%500' and Disabled='false'" ) {
    # local Administrator account is present *and* active
    write-host "`n`nchange the administrator password:"
    net user administrator *
}

Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon\' -Name AutoAdminLogon -Value '0'
Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon\' -Name DefaultPassword -Value ''

#endregion

#######################################
####   SECURED!
#######################################

#region Enable Remote Services
#######################################

    Write-Verbose "Warning: Some of these services may not be avaiable on WKS until the 1st logon."

    set-ItemProperty -Path 'HKLM:\System\CurrentControlSet\Control\Terminal Server'-name "fDenyTSConnections" -Value 0
    set-ItemProperty -Path 'HKLM:\System\CurrentControlSet\Control\Terminal Server'-name "fSingleSessionPerUser" -Value 1
    netsh.exe advfirewall firewall set rule group="remote desktop" new enable=Yes

    Get-ExecutionPolicy -list | out-string | Write-Verbose
    Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Force -ErrorAction SilentlyContinue
    c:\windows\sysWOW64\WindowsPowerShell\v1.0\powershell.exe -Command "Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Force -ErrorAction SilentlyContinue"

    get-netconnectionprofile | 
        Where-Object NetworkCategory -eq public | 
        get-netadapter | 
        Where-Object  NDisphysicalMedium -in 0,14 | 
        Set-NetConnectionProfile -NetworkCategory Private

    Enable-PSRemoting -force
    Set-Item wsman:localhost\client\trustedhosts -Value * -force
    winrm  quickconfig -transport:HTTP -force

#endregion

#region Client vs Server
#######################################

<#

# Punt this crap to the actual logged in user.

$ComputerName = read-Host "Computer Name:"
if ($ComputerName)
{
    rename-computer -newname $ComputerName
    $RestartsRequested += $true
}




if ( gwmi win32_operatingsystem | Where-Object ProductType -eq 1 ) {
    # Client

}
else {

    # Server
    reg.exe add "hklm\SOFTWARE\Policies\Microsoft\Windows NT\Reliability" /v ShutdownReasonOn /t REG_DWORD /d 0x00000000 /f

    netsh.exe advfirewall firewall set rule name="File and Printer Sharing (SMB-In)" dir=in profile=any new enable=yes
    Get-Disk | Where-Object operationalstatus -ne Online | set-Disk -IsOffline $False

    # Disable IE HArdening
    Set-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Active Setup\Installed Components\{A509B1A7-37EF-4b3f-8CFC-4F3A74704073}" -Name "IsInstalled" -Value 0
    Set-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Active Setup\Installed Components\{A509B1A8-37EF-4b3f-8CFC-4F3A74704073}" -Name "IsInstalled" -Value 0

    get-volume | Where-Object FileSystemLabel -EQ HDDScratch | foreach-object { net.exe share "scratch$=$($_.DriveLetter)`:\" /Grant:Administrators,Full /Unlimited }
    get-volume | Where-Object FileSystemLabel -EQ HDDArchive | foreach-object { net.exe share "Archive$=$($_.DriveLetter)`:\" /Grant:Administrators,Full /Unlimited }

    get-disk | where-object BusType -eq 'NVMe' | get-partition | get-volume | foreach-object { "$($_.DriveLetter)`:\" } |
        ForEach-Object {
            net share "Fast$=$_"  /Grant:Administrators,Full /Unlimited

            $VHDPath = Join-Path $_ "Hyper-V\Virtual Hard Disks"
            $VMPath = Join-Path $_ "Hyper-V"
            new-item -ItemType Directory -Path $VMPath,$VHDPath -ErrorAction SilentlyContinue | Out-Null
            Set-VMHost -VirtualHardDiskPath $VHDPath -VirtualMachinePath $VMPath

        }

}

#>
#endregion

#region Cleanup
#######################################

if ( $RestartsRequested ) {
    if( (new-object -com 'wscript.shell').Popup("The machine has installed several components that require a reboot.`r`nPress OK to reboot.`r`n`r`n...auto reboot in 20 seconds.",20,'Reboot required',1) -ne 2 ) {
        shutdown -r -f -t 0
    }
}

#endregion
