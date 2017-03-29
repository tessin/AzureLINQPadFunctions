# Run LINQPad scripts as Azure functions

This is a set of PowerShell scripts and conventions to deploy and run LINQPad scripts as Azure functions.

## Requirements

You have to install the Azure Resource Manager PowerShell cmdlets.

# Getting started

## Initial deployment

If this is the first time you use this guide you need to:

1. Create a new resource group (login to the Azure Portal create a new Resource Group) if you don’t want to use an existing...
2. Run the PowerShell script `.\Init-AzureFnDeployment.ps1`

**Note** that you have to be logged in to the Azure Resource Manager for this to work, if you are not it won’t work so run run these two commands first:

~~~
Login-AzureRmAccount; `
Set-AzureRmContext -SubscriptionId <Subscription ID goes here>
~~~

## Continuous deployment

The previous step has prepared your working directory with a file `.\AzureFn.PublishSettings` which will be used to supply the credentials when needed, keep it safe.

To publish a LINQPad script as a Azure function, do this:

> `.\lp2azfn .\HelloWorld.linq`

`lp2azfn` is a binary that was _installed_ as part of running the `.\Init-AzureFnDeployment.ps1` it also added a shortcut in the `shell:sendto` folder. Which means you can right-click a LINQPad script and now send it to Azure.

The `lp2azfn` is available pre-built from the releases tab, source code is included in this repository.

# Good to know

By default we provision using the dynamic SKU which has a maximum timeout of 5 minutes.

If you need to run LINQPad scripts for a longer than 5 minutes you need to switch SKU to an always on SKU, you can do this from within the Azure Portal or by modifying the ARM template (`azuredeploy.json`).

## References

- https://github.com/Azure/azure-webjobs-sdk-script/issues/18#issuecomment-245636277
- https://github.com/Azure/azure-webjobs-sdk-script/issues/18#issuecomment-246416239
