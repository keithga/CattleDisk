<#
 .SYNOPSIS
Installation Check for Component
.DESCRIPTION
Returns True if service need to be installed.
.NOTES

#>

[cmdletbinding()]
param ( )

$ErrorActionPreference = 'stop'
$rebootrequested = $false

#region Password
#######################################

    if ( Get-WmiObject -Class Win32_UserAccount -filter "SID LIKE '%500' and Disabled='false'" ) {
        # local Administrator account is present *and* active
        write-host "`n`nchange the administrator password:"
        net user administrator *
    }

#endregion

#region Enable Terminal Services
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

    Enable-PSRemoting -force
    Set-Item wsman:localhost\client\trustedhosts -Value * -force
    winrm  quickconfig -transport:HTTP -force

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
            net.exe user $USerAccount *
        }
    }

#endregion

#region ComptuerName
#######################################

    $ComputerName = read-Host "Computer Name:"
    if ($ComputerName)
    {
        rename-computer -newname $COmputerName
        $rebootrequested = $true
    }


#endregion

#region Client vs Server
#######################################

if ( gwmi win32_operatingsystem | Where-Object ProductType -eq 1 ) {
    # Client

}
else {

    # Server
    reg.exe add "hklm\SOFTWARE\Policies\Microsoft\Windows NT\Reliability" /v ShutdownReasonOn /t REG_DWORD /d 0x00000000 /f

    netsh.exe advfirewall firewall set rule name="File and Printer Sharing (SMB-In)" dir=in profile=any new enable=yes
    Get-Disk | Where-Object operationalstatus -ne Online | set-Disk -IsOffline $False

}


#endregion

#region Cleanup
#######################################

if ( $rebootrequested ) {

        write-host "reboot requried!`n press enter to reboot!"
        read-host
        shutdown -r -f -t 0 

}

#endregion
