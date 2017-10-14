<#
 .SYNOPSIS
Basic Machine Setup

.DESCRIPTION
Will perform the basic setup of a machine.

Will open up ports and services for remote administration, but not before securing accounts.

.NOTES

#>

[cmdletbinding()]
param (
    [string[]] $OSFeatures = @("netfx3","Microsoft-Hyper-V")
    )

$ErrorActionPreference = 'stop'
$RestartsRequested = $null


#region Add Microsoft Account

function Add-MicrosoftAccountToUser {
    [cmdletbinding()]
    param( $MicrosoftAccount, $User )

    if ( -not ( Test-Path $env:SystemRoot\System32\PSExec.exe ) ) {
        Invoke-WebRequest -Uri 'http://live.sysinternals.com/psexec.exe' $env:SystemRoot\System32\PSExec.exe
    }

    [scriptBlock]$CommandRun = { 

        Write-Host "Hello World: $MicrosoftAccount $User"

        Read-Host "Done"
    }

    $CommandRun.ToString()

}

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

        Stop-Transcript

        start-sleep 1

        exit

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
    & $env:temp\PSExec.exe /AcceptEula -e -i -s Powershell.exe -noprofile -executionpolicy bypass -File $tempfile -verbose -MicrosoftAccount "$Microsoftaccount" -User "$User" 2> out-null

    remove-item $tempFile -force   

}

#######################################

#endregion 

#region Add Windows Features
#######################################

    Write-Verbose "Gather the local path"

    $SxsPath = $null
    $SxsPath = get-volume | foreach-object { "$($_.DriveLetter)`:\Sources\SXS" } | where-object { Test-Path $_}

    Write-Verbose "Adding Features..."
    $RestartsRequested = $OSFeatures | 
        Where-Object { get-WindowsOptionalFeature -Online -FeatureName $_ | Where-Object State -NE Enabled } |
        ForEach-Object { 
            Write-Host "Add feature $_"
            Enable-WindowsOptionalFeature -Online -FeatureName $_ -All -LimitAccess -Source $SxsPath -NoRestart 
        } | 
        Where-Object RestartNeeded -eq $True

#endregion

#region Windows Update
#######################################

    Write-Verbose "prototype, just a single pass..."

    if ( -not ( get-packageprovider | ? Name -eq NuGet ) ) {
        Install-PackageProvider -Name NuGet -Force
    }
    Install-Module PSWIndowsUpdate -Force
    Import-Module PSWIndowsUpdate 

    $results = Get-WUInstall -MicrosoftUpdate -AcceptAll 

    $RestartsRequested = $results -match 'Reboot'

#endregion

#region Password
#######################################
#######################################
#######################################

    if ( Get-WmiObject -Class Win32_UserAccount -filter "SID LIKE '%500' and Disabled='false'" ) {
        # local Administrator account is present *and* active
        write-host "`n`nchange the administrator password:"
        net user administrator *

        Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon\' -Name AutoAdminLogon -Value '0'
        Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon\' -Name DefaultPassword -Value ''

    }

    #########################################################
    #########################################################
    ######
    ###### Warning!!! Local Administrator password may not be
    ###### secure. Do not open any ports, or enable remote 
    ###### services BEFORE this point
    #########################################################
    #########################################################

#endregion

#region ComptuerName
#######################################

$ComputerName = read-Host "Computer Name:"
if ($ComputerName)
{
    rename-computer -newname $COmputerName
    $RestartsRequested = $true
}

#endregion

#region Create Local Administrator Accounts
#######################################

write-host "`n`nComma Delimited List of User Accounts: (example: JohnDoe)"
foreach ( $UserAccount in (read-Host "User Accounts:") -split ',' )
{
    if ( -not ( [string]::IsNullOrEmpty( $UserAccount ) ) ) 
    {
        net.exe user /add $UserAccount /FullName:"$UserAccount" /Expires:Never P@ssw0rd
        get-wmiobject -Class Win32_UserAccount -Filter "name='$UserAccount'"  | swmi -Argument @{PasswordExpires = 0}
        write-host "net.exe localgroup administrators /add $UserAccount"
        net.exe localgroup administrators /add $UserAccount

        if ( gwmi win32_operatingsystem | Where-Object ProductType -eq 1 ) {

            write-host "`n`nMicrosoft Account: John.Doe@Hotmail.com"
            $MicrosoftAccount = read-Host "MicrosoftAccount:"
            if ( -not ( [string]::IsNullOrEmpty( $MicrosoftAccount ) ) ) 
            {
                Add-MicrosoftAccountToUser -MicrosoftAccount $MicrosoftAccount -User $UserAccount
            }
            else {
                Write-Host "Enter Password:"
                net.exe user $UserAccount *
            }
        }
        else {
            Write-Host "Enter Password:"
            net.exe user $UserAccount *
        }
    }
}

if ( gwmi win32_operatingsystem | Where-Object ProductType -eq 1 ) {

    Write-Verbose "Remove the local Administrator account if on Workstation..."
    if (  get-localuser |? SID -notmatch '(500|501|503)$' |? Enabled -EQ $True ) {
        Write-Verbose "There is at least one active account"
        net user administrator /active:no
    }
}

#endregion


#region Enable Terminal Services
#######################################
#######################################
#######################################

    set-ItemProperty -Path 'HKLM:\System\CurrentControlSet\Control\Terminal Server'-name "fDenyTSConnections" -Value 0
    set-ItemProperty -Path 'HKLM:\System\CurrentControlSet\Control\Terminal Server'-name "fSingleSessionPerUser" -Value 1
    netsh.exe advfirewall firewall set rule group="remote desktop" new enable=Yes

#endregion

#region Set Powershell Execution Policy
#######################################

    Get-ExecutionPolicy -list | out-string | Write-Verbose
    Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Force -ErrorAction SilentlyContinue
    c:\windows\sysWOW64\WindowsPowerShell\v1.0\powershell.exe -Command "Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Force -ErrorAction SilentlyContinue"

#endregion

#region Enable PS remoting
#######################################

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

    write-host "reboot requried!`n press enter to reboot!"
    read-host
    shutdown -r -f -t 0 

}

#endregion
