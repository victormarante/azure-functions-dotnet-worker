#
# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

<#
    This script requires PowerShell Core 6 or higher. To install the latest PowerShell Core, run the following:
    iex "& { $(irm 'https://aka.ms/install-powershell.ps1')} -UseMSI"
#>

#Requires -Version 6.0


function WaitForStorageEmulatorToStartRunning
{
    param ( 
        $WaitTimeInSeconds = 10,
        $MaxNumberOfTries = 360
    )

    Write-Host "Waiting for Storage emulator to start..."

    $tries = 1

    while ($true)
    {
        Start-Sleep $WaitTimeInSeconds

        if (IsStorageEmulatorRunning)
        {
            Write-Host "StorageEmulator is running"
            break
        }

        $currentWaitTimeInSeconds = [Math]::Round($tries*$WaitTimeInSeconds)        
        if (($currentWaitTimeInSeconds) % 10 -eq 0)
        {
            Write-Host "Wait time in seconds: $($currentWaitTimeInSeconds)"
        }

        if ($tries -ge $MaxNumberOfTries)
        {
            throw "Emulator did not completed initializing in $currentWaitTimeInSeconds seconds."
        }

        $tries++
    }
}

function IsStorageEmulatorRunning
{
    try
    {
        $response = Invoke-WebRequest -Uri "http://127.0.0.1:10000/"
        $StatusCode = $Response.StatusCode
    }
    catch
    {
        $StatusCode = $_.Exception.Response.StatusCode.value__
    }

    if ($StatusCode -eq 400)
    {
        return $true
    }

    return $false
}

function StartEmulator
{
    param(
        [Switch]
        $SkipStorageEmulator,
        [Switch]
        $SkipCosmosDBEmulator,
        [Switch]
        $NoWait,
        [Switch]
        $AsJob
    )

    if ($AsJob.IsPresent)
    {
        $parametersHashTable = $MyInvocation.BoundParameters
        $parametersHashTable.Remove("AsJob")

        $modulePath = Join-Path $PSScriptRoot "emulatorsHelper.psm1"

        Start-Job -ScriptBlock {
            param($Arguments, $ModulePath)

            Import-Module $ModulePath -Force
            StartEmulator @Arguments
        } -ArgumentList $parametersHashTable, $modulePath
    }

    else
    {
        Write-Host "Skip CosmosDB Emulator: $SkipCosmosDBEmulator"
        Write-Host "Skip Storage Emulator: $SkipStorageEmulator"

        if (!$SkipCosmosDBEmulator)
        {
            Import-Module "$env:ProgramFiles\Azure Cosmos DB Emulator\PSModules\Microsoft.Azure.CosmosDB.Emulator"
        }

        $startedCosmos = $false
        $startedStorage = $false
        
        if (!$SkipCosmosDBEmulator)
        {
            Write-Host ""
            Write-Host "---Starting CosmosDB emulator---"
            $cosmosStatus = Get-CosmosDbEmulatorStatus

            if ($cosmosStatus -ne "Running")
            {
                Write-Host "CosmosDB emulator is not running. Starting emulator."
                Start-CosmosDbEmulator -NoWait
                $startedCosmos = $true
            }
            else
            {
                Write-Host "CosmosDB emulator is already running."
            }
        }

        if (!$SkipStorageEmulator)
        {
            Write-Host "------"
            Write-Host ""
            Write-Host "---Starting Storage emulator---"
            $storageEmulatorRunning = IsStorageEmulatorRunning

            if ($storageEmulatorRunning -eq $false)
            {
                if ($IsWindows)
                {
                    npm install -g azurite
                    Start-Process azurite.cmd -ArgumentList "--silent"
                }
                else
                {
                    sudo npm install -g azurite
                    sudo mkdir azurite
                    sudo azurite --silent --location azurite --debug azurite\debug.log &
                }

                $startedStorage = $true
            }
            else
            {
                Write-Host "Storage emulator is already running."
            }

            Write-Host "------"
            Write-Host
        }

        if ($NoWait -eq $true)
        {
            Write-Host "'NoWait' specified. Exiting."
            Write-Host
            exit 0
        }

        if (!$SkipCosmosDBEmulator -and $startedCosmos -eq $true)
        {
            Write-Host "---Waiting for CosmosDB emulator to be running---"
            while ($cosmosStatus -ne "Running")
            {
                Write-Host "Cosmos emulator not ready. Status: $cosmosStatus"
                $cosmosStatus = Get-CosmosDbEmulatorStatus
                Start-Sleep -Seconds 5
            }
            Write-Host "Cosmos status: $cosmosStatus"
            Write-Host "------"
            Write-Host
        }

        if (!$SkipStorageEmulator -and $startedStorage -eq $true)
        {
            Write-Host "---Waiting for Storage emulator to be running---"
            $storageEmulatorRunning = IsStorageEmulatorRunning
            while ($storageEmulatorRunning -eq $false)
            {
                Write-Host "Storage emulator not ready."
                Start-Sleep -Seconds 5
                $storageEmulatorRunning = IsStorageEmulatorRunning
            }
            Write-Host "Storage emulator ready."
            Write-Host "------"
            Write-Host
        }
    }
}
